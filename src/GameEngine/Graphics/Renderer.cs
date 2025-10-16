using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Graphics.Synchronization;
using Nexus.GameEngine.Runtime;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Vulkan renderer implementation that orchestrates frame rendering.
/// Manages image acquisition, command recording, and presentation.
/// </summary>
/// <remarks>
/// <para><strong>Current Status:</strong> Partial implementation</para>
/// <para>✅ Swap chain integration (acquire/present)</para>
/// <para>✅ Synchronization (ISyncManager)</para>
/// <para>✅ Command buffer recording (ICommandPoolManager)</para>
/// <para>✅ Render pass execution (empty pass for layout transitions)</para>
/// <para>❌ Pipeline binding (pending IPipelineManager)</para>
/// </remarks>
public unsafe class Renderer(
    IGraphicsContext context,
    ISwapChain swapChain,
    ISyncManager syncManager,
    ICommandPoolManager commandPoolManager,
    ILoggerFactory loggerFactory,
    IContentManager contentManager)
    : IRenderer
{    
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(Renderer));
    private int _currentFrameIndex = 0;
    private ICommandPool? _graphicsCommandPool;

    public event EventHandler? BeforeRendering;
    public event EventHandler? AfterRendering;

    public void OnRender(double deltaTime)
    {
        if (contentManager.Viewport.Content == null)
        {
            throw new InvalidOperationException("ContentManager.Viewport.Content is null, nothing to render.");
        }

        BeforeRendering?.Invoke(this, EventArgs.Empty);

        // Get synchronization primitives for current frame
        var frameSync = syncManager.GetFrameSync(_currentFrameIndex);

        // Wait for this frame to finish (ensures command buffers aren't in use)
        syncManager.WaitForFence(frameSync.InFlightFence);
        syncManager.ResetFence(frameSync.InFlightFence);

        // 1. Acquire next swapchain image (signals per-frame ImageAvailable semaphore)
        var imageIndex = swapChain.AcquireNextImage(frameSync.ImageAvailable, out var acquireResult);

        if (acquireResult == Result.ErrorOutOfDateKhr)
        {
            swapChain.Recreate();
            return; // Skip this frame, will render with new swapchain next frame
        }
        else if (acquireResult != Result.Success && acquireResult != Result.SuboptimalKhr)
        {
            throw new Exception($"Failed to acquire swap chain image: {acquireResult}");
        }

        // Get per-image render semaphore
        var imageSync = syncManager.GetImageSync(imageIndex);

        // 2. Record command buffer with render pass (for layout transitions)
        // Lazy-initialize command pool
        _graphicsCommandPool ??= commandPoolManager.GetOrCreatePool(CommandPoolType.Graphics);

        // Allocate command buffer for this frame
        var commandBuffers = _graphicsCommandPool.AllocateCommandBuffers(1, CommandBufferLevel.Primary);
        var cmd = commandBuffers[0];

        // Create render context with camera and viewport information
        var renderContext = new RenderContext
        {
            Camera = contentManager.Viewport.Camera,
            Viewport = contentManager.Viewport,
            AvailableRenderPasses = RenderPasses.All,
            RenderPassNames = RenderPasses.Configurations.Select(c => c.Name).ToArray(),
            DeltaTime = deltaTime
        };

        // OPTIMIZATION: Pre-allocate per-pass sorted sets (11 passes = indices 0-10)
        // Commands are automatically sorted on insertion using batch strategy comparer
        // Note: RenderPasses.Configurations is static, so BatchStrategy instances are already cached
        const int PassCount = 11;
        var passCommandSets = new SortedSet<DrawCommand>[PassCount];
        for (int i = 0; i < PassCount; i++)
        {
            passCommandSets[i] = new SortedSet<DrawCommand>(RenderPasses.Configurations[i].BatchStrategy);
        }
        
        // OPTIMIZATION: Collect and sort draw commands into per-pass buckets in single traversal
        uint activePasses = 0;
        var componentStack = new Stack<IRuntimeComponent>();
        componentStack.Push(contentManager.Viewport.Content);
        
        while (componentStack.Count > 0)
        {
            var component = componentStack.Pop();
            
            // Collect draw commands and distribute to pass-specific lists
            if (component is IRenderable renderable)
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

        // Record commands
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

            // Iterate all 11 passes in order (pass index corresponds to bit position)
            for (uint passMask = 1u, p = 0; p < PassCount; passMask <<= 1, p++)
            {
                // Skip inactive passes
                if ((activePasses & passMask) == 0)
                    continue;
                
                var passDrawCommands = passCommandSets[p];

                // Skip empty passes (defensive check)
                if (passDrawCommands.Count == 0)
                    continue;

                // Begin render pass
                var renderPassInfo = new RenderPassBeginInfo
                {
                    SType = StructureType.RenderPassBeginInfo,
                    RenderPass = swapChain.Passes[p],
                    Framebuffer = swapChain.Framebuffers[p][imageIndex],
                    RenderArea = new Rect2D
                    {
                        Offset = new Offset2D(0, 0),
                        Extent = swapChain.SwapchainExtent
                    },
                    ClearValueCount = (uint)swapChain.ClearValues[p].Length,
                    PClearValues = (ClearValue*)System.Runtime.CompilerServices.Unsafe.AsPointer(
                        ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(swapChain.ClearValues[p]))
                };

                context.VulkanApi.CmdBeginRenderPass(cmd, &renderPassInfo, SubpassContents.Inline);

                // Draw sorted commands for this pass
                foreach (var drawCommand in passDrawCommands)
                {
                    Draw(cmd, drawCommand);
                }

                // End render pass
                context.VulkanApi.CmdEndRenderPass(cmd);
            }

            // End command buffer
            result = context.VulkanApi.EndCommandBuffer(cmd);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to end command buffer: {result}");
            }

            // Submit command buffer
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

            result = context.VulkanApi.QueueSubmit(context.GraphicsQueue, 1, &submitInfo, frameSync.InFlightFence);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to submit queue: {result}");
            }
        }

        // 3. Present the rendered image (waits for RenderFinished semaphore)
        try
        {
            swapChain.Present(imageIndex, imageSync.RenderFinished);
        }
        catch (Exception ex) when (ex.Message.Contains("out of date") || ex.Message.Contains("suboptimal"))
        {
            swapChain.Recreate();
        }

        AfterRendering?.Invoke(this, EventArgs.Empty);

        // Move to next frame
        _currentFrameIndex = (_currentFrameIndex + 1) % syncManager.MaxFramesInFlight;
    }

    private void Draw(CommandBuffer commandBuffer, DrawCommand drawCommand)
    {
        context.VulkanApi.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, drawCommand.Pipeline);
        
        var vertexBuffers = stackalloc Silk.NET.Vulkan.Buffer[] { drawCommand.VertexBuffer };
        var offsets = stackalloc ulong[] { 0 };
        context.VulkanApi.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
        
        context.VulkanApi.CmdDraw(commandBuffer, drawCommand.VertexCount, drawCommand.InstanceCount, drawCommand.FirstVertex, 0);
    }
}
