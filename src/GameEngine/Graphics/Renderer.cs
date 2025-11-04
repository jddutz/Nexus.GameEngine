using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Graphics.Synchronization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Vulkan renderer implementation that orchestrates frame rendering.
/// Manages image acquisition, command recording, and presentation.
/// Uses ContentManager to get active cameras for rendering.
/// </summary>
public unsafe class Renderer(
    IGraphicsContext context,
    ISwapChain swapChain,
    ISyncManager syncManager,
    ICommandPoolManager commandPoolManager,
    IContentManager contentManager,
    IWindowService windowService,
    IComponentFactory componentFactory,
    IOptions<GraphicsSettings> graphicsOptions)
    : IRenderer
{
    private readonly GraphicsSettings _graphicsSettings = graphicsOptions.Value;
    private readonly ICamera _defaultCamera = CreateAndActivateDefaultCamera(componentFactory, graphicsOptions.Value);
    private int _currentFrameIndex = 0;
    private ICommandPool? _graphicsCommandPool;

    public event EventHandler<RenderEventArgs>? BeforeRendering;
    public event EventHandler<RenderEventArgs>? AfterRendering;

    private static ICamera CreateAndActivateDefaultCamera(IComponentFactory factory, GraphicsSettings settings)
    {
        var clearColor = settings.BackgroundColor ?? new Vector4D<float>(0, 0, 0.545f, 1);
        var template = new StaticCameraTemplate
        {
            Name = "DefaultCamera",
            ClearColor = clearColor,
            ScreenRegion = new Rectangle<float>(0, 0, 1, 1),
            RenderPriority = 100,  // High priority - renders last (UI overlay)
            RenderPassMask = RenderPasses.All
        };
        
        var camera = factory.CreateInstance(template) as ICamera;
        if (camera is IRuntimeComponent runtimeComponent)
        {
            runtimeComponent.Activate();
        }
        
        return camera ?? throw new InvalidOperationException("Failed to create default camera");
    }

    public void OnRender(double deltaTime)
    {
        try
        {
            // Skip rendering if window is minimized (swapchain extent is 0x0)
            if (swapChain.SwapchainExtent.Width == 0 || swapChain.SwapchainExtent.Height == 0)
            {
                return;
            }

            // Prepare frame synchronization and acquire next image
            var (frameSync, imageIndex, imageSync) = PrepareFrame();

            BeforeRendering?.Invoke(this, new RenderEventArgs { ImageIndex = imageIndex });

            int cameraCount = 0;

            // Iterate over all cameras: content cameras + default screen-space camera
            foreach (var camera in GetAllCameras())
            {
                cameraCount++;

                // Update camera viewport size to match swapchain (handles window resize)
                if (camera is StaticCamera staticCamera)
                {
                    staticCamera.SetViewportSize(swapChain.SwapchainExtent.Width, swapChain.SwapchainExtent.Height);
                }

                // Create viewport from camera
                var viewport = camera.GetViewport();

                // Create render context for this frame
                var renderContext = new RenderContext
                {
                    Camera = camera,
                    Viewport = viewport,
                    AvailableRenderPasses = RenderPasses.All,
                    RenderPassNames = RenderPasses.Configurations.Select(c => c.Name).ToArray(),
                    DeltaTime = deltaTime,
                    ViewProjectionDescriptorSet = camera.GetViewProjectionDescriptorSet()
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
            }

            if (cameraCount > 0)
            {
                // Present rendered image to screen
                PresentFrame(imageIndex, imageSync);

                AfterRendering?.Invoke(this, new RenderEventArgs { ImageIndex = imageIndex });

                // Move to next frame
                _currentFrameIndex = (_currentFrameIndex + 1) % syncManager.MaxFramesInFlight;
            }
            else
            {
                throw new InvalidOperationException("No active viewports to render.");
            }
        }
        catch (Exception)
        {
            windowService.GetWindow().Close();
        }
    }

    /// <summary>
    /// Gets all cameras to render: content cameras from ContentManager + default screen-space camera.
    /// Cameras are sorted by RenderPriority (default camera has priority 100, renders last for UI overlay).
    /// </summary>
    private IEnumerable<ICamera> GetAllCameras()
    {
        // Yield content cameras first (sorted by priority)
        foreach (var camera in contentManager.ActiveCameras)
        {
            yield return camera;
        }
        
        // Always include default screen-space camera for UI rendering
        yield return _defaultCamera;
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
        
        // OPTIMIZATION: Collect and sort draw commands into per-pass buckets
        // Main pass always executes to handle initial image layout transitions
        // UI pass always executes to transition image to PresentSrcKhr for presentation
        uint activePasses = RenderPasses.Main | RenderPasses.UI;
        
        // Use cached visible drawables list built during Update (no tree traversal needed!)
        var visibleDrawables = contentManager.VisibleDrawables.ToList();
        
        int commandCount = 0;
        foreach (var drawable in visibleDrawables)
        {
            foreach (var drawCommand in drawable.GetDrawCommands(renderContext))
            {
                commandCount++;
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
            // Convert viewport's ClearColor to Vulkan clear value
            var clearColor = renderContext.Viewport.ClearColor;
            passClearValues[clearValueCount++] = new ClearValue
            {
                Color = new ClearColorValue(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W)
            };
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

        // Set dynamic viewport and scissor state from the viewport extent
        var extent = renderContext.Viewport.Extent;
        var vulkanViewport = new Silk.NET.Vulkan.Viewport
        {
            X = 0,
            Y = 0,
            Width = extent.Width,
            Height = extent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        context.VulkanApi.CmdSetViewport(cmd, 0, 1, &vulkanViewport);

        var scissor = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = extent
        };
        context.VulkanApi.CmdSetScissor(cmd, 0, 1, &scissor);

        // Track state to avoid redundant Vulkan calls
        ulong lastPipelineHandle = 0;
        ulong lastDescriptorSetHandle = 0;

        // Draw sorted commands for this pass
        foreach (var drawCommand in drawCommands)
        {
            Draw(cmd, drawCommand, renderContext, ref lastPipelineHandle, ref lastDescriptorSetHandle);
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

    private void Draw(CommandBuffer commandBuffer, DrawCommand drawCommand, RenderContext renderContext, ref ulong lastPipelineHandle, ref ulong lastDescriptorSetHandle)
    {
        // Only bind pipeline if it changed
        if (lastPipelineHandle != drawCommand.Pipeline.Pipeline.Handle)
        {
            context.VulkanApi.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, drawCommand.Pipeline.Pipeline);
            lastPipelineHandle = drawCommand.Pipeline.Pipeline.Handle;
        }
        
        // Bind descriptor sets based on pipeline type
        if (drawCommand.Pipeline.Name == "UI_Element")
        {
            // UIElement shader needs two descriptor sets:
            // set=0: ViewProjection UBO (from camera)
            // set=1: Texture sampler (from command)
            
            // Bind ViewProjection at set=0
            if (renderContext.ViewProjectionDescriptorSet.Handle != 0)
            {
                var vpDescriptorSets = stackalloc DescriptorSet[] { renderContext.ViewProjectionDescriptorSet };
                context.VulkanApi.CmdBindDescriptorSets(
                    commandBuffer,
                    PipelineBindPoint.Graphics,
                    drawCommand.Pipeline.Layout,
                    0, // first set
                    1, // descriptor set count
                    vpDescriptorSets,
                    0, // dynamic offset count
                    null); // dynamic offsets
            }
            
            // Bind texture at set=1
            if (drawCommand.DescriptorSet.Handle != 0)
            {
                var textureDescriptorSets = stackalloc DescriptorSet[] { drawCommand.DescriptorSet };
                context.VulkanApi.CmdBindDescriptorSets(
                    commandBuffer,
                    PipelineBindPoint.Graphics,
                    drawCommand.Pipeline.Layout,
                    1, // first set
                    1, // descriptor set count
                    textureDescriptorSets,
                    0, // dynamic offset count
                    null); // dynamic offsets
            }
        }
        else
        {
            // Standard single descriptor set binding
            if (drawCommand.DescriptorSet.Handle != 0 && drawCommand.DescriptorSet.Handle != lastDescriptorSetHandle)
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
                lastDescriptorSetHandle = drawCommand.DescriptorSet.Handle;
            }
        }
        
        // Push constants if provided (these typically change per draw, so always push)
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
            case UIElementPushConstants uiElement:
            {
                var size = (uint)Marshal.SizeOf<UIElementPushConstants>();
                var dataPtr = &uiElement;
                context.VulkanApi.CmdPushConstants(
                    commandBuffer,
                    pipelineLayout,
                    ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
                    0,  // offset
                    size,
                    dataPtr);
                break;
            }
        }
    }
}
