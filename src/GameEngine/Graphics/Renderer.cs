using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Graphics.Synchronization;
using Nexus.GameEngine.Runtime;
using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Vulkan renderer implementation that orchestrates frame rendering.
/// Manages image acquisition, command recording, and presentation.
/// </summary>
public unsafe class Renderer(
    ILoggerFactory loggerFactory,
    IGraphicsContext context,
    ISwapChain swapChain,
    ISyncManager syncManager,
    ICommandPoolManager commandPoolManager,
    IContentManager contentManager)
    : IRenderer
{    
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(Renderer));
    private int _currentFrameIndex = 0;
    private ICommandPool? _graphicsCommandPool;

    public event EventHandler<RenderEventArgs>? BeforeRendering;
    public event EventHandler<RenderEventArgs>? AfterRendering;

    public void OnRender(double deltaTime)
    {
        try
        {
            if (contentManager.Viewport.Content == null)
            {
                throw new InvalidOperationException("ContentManager.Viewport.Content is null, nothing to render.");
            }

            // Skip rendering if window is minimized (swapchain extent is 0x0)
            if (swapChain.SwapchainExtent.Width == 0 || swapChain.SwapchainExtent.Height == 0)
            {
                return;
            }

            // Prepare frame synchronization and acquire next image
            var (frameSync, imageIndex, imageSync) = PrepareFrame();

            BeforeRendering?.Invoke(this, new RenderEventArgs { ImageIndex = imageIndex });

            // Create render context for this frame
            var renderContext = new RenderContext
            {
                Camera = contentManager.Viewport.Camera,
                Viewport = contentManager.Viewport,
                AvailableRenderPasses = RenderPasses.All,
                RenderPassNames = RenderPasses.Configurations.Select(c => c.Name).ToArray(),
                DeltaTime = deltaTime
            };

            // Collect and sort draw commands from component tree
            var (activePasses, passCommandSets) = CollectDrawCommands(renderContext);

            // Allocate and record command buffer
            _graphicsCommandPool ??= commandPoolManager.GetOrCreatePool(CommandPoolType.Graphics);

            // Allocate command buffer for this frame
            var commandBuffers = _graphicsCommandPool.AllocateCommandBuffers(1, CommandBufferLevel.Primary);

            // Record rendering commands
            RecordCommandBuffer(commandBuffers[0], imageIndex, renderContext, activePasses, passCommandSets);

            // Submit commands to GPU
            SubmitFrame(commandBuffers[0], frameSync, imageSync);

            // Present rendered image to screen
            PresentFrame(imageIndex, imageSync);

            AfterRendering?.Invoke(this, new RenderEventArgs { ImageIndex = imageIndex });

            // Move to next frame
            _currentFrameIndex = (_currentFrameIndex + 1) % syncManager.MaxFramesInFlight;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during Render loop");
        }
    }

    /// <summary>
    /// Prepares frame synchronization and acquires the next swapchain image.
    /// </summary>
    /// <returns>Frame sync, image index, and image sync objects.</returns>
    private (FrameSync frameSync, uint imageIndex, ImageSync imageSync) PrepareFrame()
    {
        // Get synchronization primitives for current frame
        var frameSync = syncManager.GetFrameSync(_currentFrameIndex);

        // Wait for this frame to finish (ensures command buffers aren't in use)
        syncManager.WaitForFence(frameSync.InFlightFence);
        syncManager.ResetFence(frameSync.InFlightFence);

        // Acquire next swapchain image (signals per-frame ImageAvailable semaphore)
        var imageIndex = swapChain.AcquireNextImage(frameSync.ImageAvailable, out var acquireResult);

        if (acquireResult == Result.ErrorOutOfDateKhr)
        {
            swapChain.Recreate();
            throw new InvalidOperationException("Swapchain is out of date, recreating.");
        }
        else if (acquireResult != Result.Success && acquireResult != Result.SuboptimalKhr)
        {
            throw new Exception($"Failed to acquire swap chain image: {acquireResult}");
        }

        // Get per-image render semaphore
        var imageSync = syncManager.GetImageSync(imageIndex);

        return (frameSync, imageIndex, imageSync);
    }

    /// <summary>
    /// Collects and sorts draw commands from the component tree into per-pass buckets.
    /// </summary>
    /// <returns>Active passes mask and per-pass sorted command sets.</returns>
    private (uint activePasses, SortedSet<DrawCommand>[] passCommandSets) CollectDrawCommands(RenderContext renderContext)
    {
        // OPTIMIZATION: Pre-allocate per-pass sorted sets (8 passes = indices 0-7)
        // Commands are automatically sorted on insertion using batch strategy comparer
        // Note: RenderPasses.Configurations is static, so BatchStrategy instances are already cached
        const int PassCount = 8;
        var passCommandSets = new SortedSet<DrawCommand>[PassCount];
        for (int i = 0; i < PassCount; i++)
        {
            passCommandSets[i] = new SortedSet<DrawCommand>(RenderPasses.Configurations[i].BatchStrategy);
        }
        
        // OPTIMIZATION: Collect and sort draw commands into per-pass buckets in single traversal
        // Main pass always executes to handle initial image layout transitions
        // UI pass always executes to transition image to PresentSrcKhr for presentation
        uint activePasses = RenderPasses.Main | RenderPasses.UI;
        var componentStack = new Stack<IRuntimeComponent>();
        componentStack.Push(contentManager.Viewport.Content!);
        
        while (componentStack.Count > 0)
        {
            var component = componentStack.Pop();
            
            // Collect draw commands and distribute to pass-specific lists
            // PERFORMANCE: Check IsVisible before calling GetDrawCommands() to skip hidden components
            if (component is IDrawable renderable && renderable.IsVisible())
            {
                foreach (var drawCommand in renderable.GetDrawCommands(renderContext))
                {
                    activePasses |= drawCommand.RenderMask;  // HOT PATH: Bitwise OR accumulation
                    
                    // Add command to all passes it participates in (auto-sorted on insertion)
                    for (int bitPos = 0; bitPos < PassCount; bitPos++)
                    {
                        uint passMask = 1u << bitPos;
                        if ((drawCommand.RenderMask & passMask) != 0)
                        {
                            passCommandSets[bitPos].Add(drawCommand);  // O(log N) insertion, maintains sort
                        }
                    }
                }
            }
            
            // Push children onto stack for traversal
            foreach (var child in component.Children)
            {
                componentStack.Push(child);
            }
        }

        return (activePasses, passCommandSets);
    }

    /// <summary>
    /// Records all rendering commands into the command buffer.
    /// </summary>
    private void RecordCommandBuffer(
        CommandBuffer cmd, 
        uint imageIndex, 
        RenderContext renderContext, 
        uint activePasses, 
        SortedSet<DrawCommand>[] passCommandSets)
    {
        const int PassCount = 8;

        unsafe
        {
            // Begin command buffer
            var beginInfo = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit
            };

            var result = context.VulkanApi.BeginCommandBuffer(cmd, &beginInfo);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to begin command buffer: {result}");
            }

            // Pre-allocate clear values buffer outside loop to avoid stack overflow
            ClearValue* passClearValues = stackalloc ClearValue[2]; // Max 2: color + depth

            // Iterate all 8 passes in order (pass index corresponds to bit position)
            for (uint passMask = 1u, p = 0; p < PassCount; passMask <<= 1, p++)
            {
                // Skip inactive passes
                if ((activePasses & passMask) == 0)
                    continue;
                
                // Skip empty passes except Main and UI
                if (passMask != RenderPasses.Main
                    && passMask != RenderPasses.UI
                    && passCommandSets[p].Count == 0)
                    continue;

                // Record this render pass
                RecordRenderPass(cmd, imageIndex, p, passClearValues, renderContext, passCommandSets[p]);
            }

            // End command buffer
            result = context.VulkanApi.EndCommandBuffer(cmd);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to end command buffer: {result}");
            }
        }
    }

    /// <summary>
    /// Records a single render pass with its draw commands.
    /// </summary>
    private unsafe void RecordRenderPass(
        CommandBuffer cmd,
        uint imageIndex,
        uint passIndex,
        ClearValue* passClearValues,
        RenderContext renderContext,
        SortedSet<DrawCommand> drawCommands)
    {
        // Build clear values dynamically based on pass configuration
        var config = RenderPasses.Configurations[passIndex];
        var clearValueCount = 0;
        
        // Add color clear value if this pass clears color
        if (config.ColorLoadOp == AttachmentLoadOp.Clear)
        {
            // Use cached clear value from Viewport (converted in OnUpdate)
            passClearValues[clearValueCount++] = renderContext.Viewport.ClearColorValue;
        }
        else if (config.ColorFormat != Format.Undefined)
        {
            // Color attachment exists but not clearing - still need placeholder
            passClearValues[clearValueCount++] = default;
        }
        
        // Add depth clear value if this pass has depth and clears it
        if (config.DepthFormat != Format.Undefined)
        {
            if (config.DepthLoadOp == AttachmentLoadOp.Clear)
            {
                passClearValues[clearValueCount++] = new ClearValue
                {
                    DepthStencil = new ClearDepthStencilValue
                    {
                        Depth = 1.0f,
                        Stencil = 0
                    }
                };
            }
            else
            {
                // Depth attachment exists but not clearing - still need placeholder
                passClearValues[clearValueCount++] = default;
            }
        }

        // Begin render pass
        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = swapChain.Passes[passIndex],
            Framebuffer = swapChain.Framebuffers[passIndex][imageIndex],
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = swapChain.SwapchainExtent
            },
            ClearValueCount = (uint)clearValueCount,
            PClearValues = passClearValues
        };

        context.VulkanApi.CmdBeginRenderPass(cmd, &renderPassInfo, SubpassContents.Inline);

        // Set dynamic viewport and scissor state from the current viewport
        var viewport = renderContext.Viewport.VulkanViewport;
        context.VulkanApi.CmdSetViewport(cmd, 0, 1, &viewport);

        var scissor = renderContext.Viewport.VulkanScissor;
        context.VulkanApi.CmdSetScissor(cmd, 0, 1, &scissor);

        // Draw sorted commands for this pass
        foreach (var drawCommand in drawCommands)
        {
            Draw(cmd, drawCommand);
        }

        // End render pass
        context.VulkanApi.CmdEndRenderPass(cmd);
    }

    /// <summary>
    /// Submits the recorded command buffer to the GPU queue.
    /// </summary>
    private unsafe void SubmitFrame(CommandBuffer cmd, FrameSync frameSync, ImageSync imageSync)
    {
        var waitStages = PipelineStageFlags.ColorAttachmentOutputBit;
        var imageAvailableSemaphore = frameSync.ImageAvailable;  // Per-frame acquire semaphore
        var renderFinishedSemaphore = imageSync.RenderFinished;   // Per-image render semaphore
        var commandBuffer = cmd;

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &imageAvailableSemaphore,
            PWaitDstStageMask = &waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &renderFinishedSemaphore
        };

        var result = context.VulkanApi.QueueSubmit(context.GraphicsQueue, 1, &submitInfo, frameSync.InFlightFence);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to submit queue: {result}");
        }
    }

    /// <summary>
    /// Presents the rendered image to the screen.
    /// </summary>
    private void PresentFrame(uint imageIndex, ImageSync imageSync)
    {
        try
        {
            swapChain.Present(imageIndex, imageSync.RenderFinished);
        }
        catch (Exception ex) when (ex.Message.Contains("out of date") || ex.Message.Contains("suboptimal"))
        {
            swapChain.Recreate();
        }
    }

    private void Draw(CommandBuffer commandBuffer, DrawCommand drawCommand)
    {
        context.VulkanApi.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, drawCommand.Pipeline.Pipeline);
        
        // Bind descriptor sets if provided
        if (drawCommand.DescriptorSet.Handle != 0)
        {
            var descriptorSets = stackalloc DescriptorSet[] { drawCommand.DescriptorSet };
            context.VulkanApi.CmdBindDescriptorSets(
                commandBuffer,
                PipelineBindPoint.Graphics,
                drawCommand.Pipeline.Layout,
                0, // first set
                1, // descriptor set count
                descriptorSets,
                0, // dynamic offset count
                null); // dynamic offsets
        }
        
        // Push constants if provided
        if (drawCommand.PushConstants != null && drawCommand.Pipeline.Layout.Handle != 0)
        {
            PushConstantsToShader(commandBuffer, drawCommand.Pipeline.Layout, drawCommand.PushConstants);
        }
        
        var vertexBuffers = stackalloc Silk.NET.Vulkan.Buffer[] { drawCommand.VertexBuffer };
        var offsets = stackalloc ulong[] { 0 };
        context.VulkanApi.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
        
        context.VulkanApi.CmdDraw(commandBuffer, drawCommand.VertexCount, drawCommand.InstanceCount, drawCommand.FirstVertex, 0);
    }

    private unsafe void PushConstantsToShader(CommandBuffer commandBuffer, PipelineLayout pipelineLayout, object pushConstants)
    {
        switch (pushConstants)
        {
            case UniformColorPushConstants uniformColor:
            {
                var size = (uint)Marshal.SizeOf<UniformColorPushConstants>();
                var dataPtr = &uniformColor;
                context.VulkanApi.CmdPushConstants(
                    commandBuffer,
                    pipelineLayout,
                    ShaderStageFlags.VertexBit,
                    0,  // offset
                    size,
                    dataPtr);
                break;
            }
            case VertexColorsPushConstants colors:
            {
                var size = (uint)Marshal.SizeOf<VertexColorsPushConstants>();
                var dataPtr = &colors;
                context.VulkanApi.CmdPushConstants(
                    commandBuffer,
                    pipelineLayout,
                    ShaderStageFlags.VertexBit,
                    0,  // offset
                    size,
                    dataPtr);
                break;
            }
            case LinearGradientPushConstants linearGradient:
            {
                var size = (uint)Marshal.SizeOf<LinearGradientPushConstants>();
                var dataPtr = &linearGradient;
                context.VulkanApi.CmdPushConstants(
                    commandBuffer,
                    pipelineLayout,
                    ShaderStageFlags.FragmentBit,  // Linear gradient uses fragment shader
                    0,  // offset
                    size,
                    dataPtr);
                break;
            }
            case RadialGradientPushConstants radialGradient:
            {
                var size = (uint)Marshal.SizeOf<RadialGradientPushConstants>();
                var dataPtr = &radialGradient;
                context.VulkanApi.CmdPushConstants(
                    commandBuffer,
                    pipelineLayout,
                    ShaderStageFlags.FragmentBit,  // Radial gradient uses fragment shader
                    0,  // offset
                    size,
                    dataPtr);
                break;
            }
            case ImageTexturePushConstants imageTexture:
            {
                var size = (uint)Marshal.SizeOf<ImageTexturePushConstants>();
                var dataPtr = &imageTexture;
                context.VulkanApi.CmdPushConstants(
                    commandBuffer,
                    pipelineLayout,
                    ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,  // Both shaders access push constants
                    0,  // offset
                    size,
                    dataPtr);
                break;
            }
        }
    }
}
