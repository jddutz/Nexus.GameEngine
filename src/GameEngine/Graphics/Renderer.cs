using Microsoft.Extensions.Logging;
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
public class Renderer(
    IGraphicsContext context,
    ISwapChain swapChain,
    ISyncManager syncManager,
    ICommandPoolManager commandPoolManager,
    ILoggerFactory loggerFactory,
    IContentManager contentManager,
    IBatchStrategy batchStrategy) : IRenderer
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

        _logger.LogTrace("Render frame - deltaTime: {DeltaTime:F4}s", deltaTime);

        // Get synchronization primitives for current frame
        var frameSync = syncManager.GetFrameSync(_currentFrameIndex);

        // Wait for this frame to finish (ensures command buffers aren't in use)
        syncManager.WaitForFence(frameSync.InFlightFence);
        syncManager.ResetFence(frameSync.InFlightFence);

        // 1. Acquire next swapchain image (signals per-frame ImageAvailable semaphore)
        var imageIndex = swapChain.AcquireNextImage(frameSync.ImageAvailable, out var acquireResult);

        if (acquireResult == Result.ErrorOutOfDateKhr)
        {
            _logger.LogInformation("Swap chain out of date during acquire, recreating");
            swapChain.Recreate();
            return; // Skip this frame, will render with new swapchain next frame
        }
        else if (acquireResult != Result.Success && acquireResult != Result.SuboptimalKhr)
        {
            throw new Exception($"Failed to acquire swap chain image: {acquireResult}");
        }

        _logger.LogTrace("Acquired image {ImageIndex}", imageIndex);

        // Get per-image render semaphore
        var imageSync = syncManager.GetImageSync(imageIndex);

        // 2. Record command buffer with render pass (for layout transitions)
        // Lazy-initialize command pool
        _graphicsCommandPool ??= commandPoolManager.GetOrCreatePool(CommandPoolType.Graphics);

        // Allocate command buffer for this frame
        var commandBuffers = _graphicsCommandPool.AllocateCommandBuffers(1, CommandBufferLevel.Primary);
        var cmd = commandBuffers[0];

        var drawCommands = GetDrawCommandsFromComponents(contentManager.Viewport.Content)
            .OrderBy(batchStrategy.GetHashCode)
            .ToList();

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

            // Render each configured pass
            for (uint passIndex = 0, passMask = 1;
                passIndex < swapChain.RenderPasses.Length;
                passIndex++, passMask <<= 1)
            {                
                // Get render pass resources
                var renderPass = swapChain.RenderPasses[passIndex];
                var framebuffer = swapChain.Framebuffers[renderPass][imageIndex];
                var clearValues = swapChain.ClearValues[renderPass];

                // Begin render pass
                var renderPassInfo = new RenderPassBeginInfo
                {
                    SType = StructureType.RenderPassBeginInfo,
                    RenderPass = renderPass,
                    Framebuffer = framebuffer,
                    RenderArea = new Rect2D
                    {
                        Offset = new Offset2D(0, 0),
                        Extent = swapChain.SwapchainExtent
                    },
                    ClearValueCount = (uint)clearValues.Length,
                    PClearValues = (ClearValue*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(clearValues))
                };

                context.VulkanApi.CmdBeginRenderPass(cmd, &renderPassInfo, SubpassContents.Inline);

                // Draw all commands for this pass (filter by bit mask)
                foreach (var drawCommand in drawCommands)
                {
                    if ((drawCommand.RenderMask & passMask) != 0)
                    {
                        Draw(cmd, drawCommand);
                    }
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
            _logger.LogInformation("Swap chain needs recreation after present");
            swapChain.Recreate();
        }

        AfterRendering?.Invoke(this, EventArgs.Empty);

        // Move to next frame
        _currentFrameIndex = (_currentFrameIndex + 1) % syncManager.MaxFramesInFlight;
    }
    
    private IEnumerable<DrawCommand> GetDrawCommandsFromComponents(IRuntimeComponent component)
    {
        if (component is IRenderable renderable)
        {
            foreach (var drawCommand in renderable.GetDrawCommands())
            {
                yield return drawCommand;
            }
        }
        
        foreach (var drawCommand in component.Children.SelectMany(GetDrawCommandsFromComponents))
        {
            yield return drawCommand;
        }
    }

    private void Draw(CommandBuffer cmd, DrawCommand element)
    {
        // TODO: Implement actual drawing using command buffers and pipelines
        // This will involve:
        // 1. Selecting appropriate pipeline from IVkPipelineManager
        // 2. Binding pipeline to command buffer
        // 3. Binding vertex/index buffers
        // 4. Binding descriptor sets (uniforms, textures)
        // 5. Recording draw command
    }
}
