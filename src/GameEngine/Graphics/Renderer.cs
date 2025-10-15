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
public unsafe class Renderer(
    IGraphicsContext context,
    ISwapChain swapChain,
    ISyncManager syncManager,
    ICommandPoolManager commandPoolManager,
    ILoggerFactory loggerFactory,
    IContentManager contentManager,
    VulkanSettings vulkanSettings) : IRenderer
{    
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(Renderer));
    private int _currentFrameIndex = 0;
    private ICommandPool? _graphicsCommandPool;
    private readonly Dictionary<RenderPass, IBatchStrategy> _batchStrategies = BuildBatchStrategyMapping(swapChain, vulkanSettings);

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

        // Collect all draw commands from scene graph (unsorted)
        var allDrawCommands = GetDrawCommandsFromComponents(contentManager.Viewport.Content).ToList();

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
                var batchStrategy = _batchStrategies[renderPass];

                // Filter and sort draw commands for this pass
                var passDrawCommands = allDrawCommands
                    .Where(cmd => (cmd.RenderMask & passMask) != 0)
                    .OrderBy(cmd => cmd, batchStrategy)
                    .ToList();

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

    private void Draw(CommandBuffer commandBuffer, DrawCommand drawCommand)
    {
        context.VulkanApi.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, drawCommand.Pipeline);
        
        var vertexBuffers = stackalloc Silk.NET.Vulkan.Buffer[] { drawCommand.VertexBuffer };
        var offsets = stackalloc ulong[] { 0 };
        context.VulkanApi.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
        
        context.VulkanApi.CmdDraw(commandBuffer, drawCommand.VertexCount, drawCommand.InstanceCount, drawCommand.FirstVertex, 0);
    }

    /// <summary>
    /// Builds the mapping from RenderPass handles to their configured batch strategies.
    /// Uses array indices to pair swapChain.RenderPasses with vulkanSettings.RenderPasses.
    /// </summary>
    private static Dictionary<RenderPass, IBatchStrategy> BuildBatchStrategyMapping(
        ISwapChain swapChain,
        VulkanSettings vulkanSettings)
    {
        var mapping = new Dictionary<RenderPass, IBatchStrategy>();
        
        for (int i = 0; i < swapChain.RenderPasses.Length; i++)
        {
            var renderPass = swapChain.RenderPasses[i];
            var config = vulkanSettings.RenderPasses[i];
            mapping[renderPass] = config.BatchStrategy;
        }
        
        return mapping;
    }
}
