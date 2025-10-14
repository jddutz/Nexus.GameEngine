using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using VkFence = Silk.NET.Vulkan.Fence;

namespace Nexus.GameEngine.Graphics.Synchronization;

/// <summary>
/// Synchronization primitives for a single frame in flight.
/// Contains fence and acquire semaphore for CPU/GPU synchronization.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// Each frame being rendered simultaneously (frames in flight) needs its own fence and
/// acquire semaphore. Use with per-image render semaphores to avoid conflicts.
/// 
/// <para><strong>Pattern (2 frames in flight, 3 swapchain images):</strong></para>
/// <code>
/// Frame 0: ImageAvailable[0] (for acquire) + Fence[0] (for CPU wait)
/// Frame 1: ImageAvailable[1] (for acquire) + Fence[1] (for CPU wait)
/// Image 0: RenderFinished[0] (for present) - from ImageSync
/// Image 1: RenderFinished[1] (for present) - from ImageSync  
/// Image 2: RenderFinished[2] (for present) - from ImageSync
/// </code>
/// 
/// <para><strong>Semaphores:</strong></para>
/// <list type="bullet">
/// <item>ImageAvailable: Per-frame acquire semaphore, signaled by vkAcquireNextImageKHR</item>
/// </list>
/// 
/// <para><strong>Fence:</strong></para>
/// <list type="bullet">
/// <item>InFlightFence: GPU-CPU sync - signals when GPU has finished processing this frame's commands</item>
/// </list>
/// 
/// <para><strong>Typical Usage Flow:</strong></para>
/// <code>
/// // 1. Wait for fence (CPU waits for GPU to finish previous use of this frame slot)
/// vkWaitForFences(device, 1, &fence, VK_TRUE, UINT64_MAX);
/// vkResetFences(device, 1, &fence);
/// 
/// // 2. Acquire swapchain image (signals ImageAvailable when ready)
/// vkAcquireNextImageKHR(device, swapchain, UINT64_MAX, imageAvailableSemaphore, VK_NULL_HANDLE, &imageIndex);
/// 
/// // 3. Submit commands (waits for ImageAvailable, signals RenderFinished)
/// submitInfo.waitSemaphoreCount = 1;
/// submitInfo.pWaitSemaphores = &imageAvailableSemaphore;
/// submitInfo.signalSemaphoreCount = 1;
/// submitInfo.pSignalSemaphores = &renderFinishedSemaphore;
/// vkQueueSubmit(graphicsQueue, 1, &submitInfo, fence);
/// 
/// // 4. Present (waits for RenderFinished)
/// presentInfo.waitSemaphoreCount = 1;
/// presentInfo.pWaitSemaphores = &renderFinishedSemaphore;
/// vkQueuePresentKHR(presentQueue, &presentInfo);
/// </code>
/// </remarks>
public record FrameSync
{
    /// <summary>
    /// Per-frame semaphore signaled by vkAcquireNextImageKHR when swapchain image is available.
    /// GPU waits for this before starting render pass.
    /// Use one per frame in flight (not per swapchain image).
    /// </summary>
    public required VkSemaphore ImageAvailable { get; init; }

    /// <summary>
    /// Fence signaled when all command buffers for this frame have finished executing.
    /// CPU waits for this before reusing the frame's command buffers.
    /// </summary>
    public required VkFence InFlightFence { get; init; }

    /// <summary>
    /// Frame index this sync set belongs to (0 to MaxFramesInFlight-1).
    /// </summary>
    public required int FrameIndex { get; init; }
}
