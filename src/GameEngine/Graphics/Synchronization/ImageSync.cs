using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace Nexus.GameEngine.Graphics.Synchronization;

/// <summary>
/// Synchronization primitives for a specific swapchain image.
/// Contains semaphores that should be reused only for the same swapchain image.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// To avoid semaphore reuse conflicts, each swapchain image should have its own set of semaphores.
/// This is separate from the "frames in flight" concept which uses fences.
/// 
/// <para><strong>Pattern:</strong></para>
/// <list type="bullet">
/// <item>Fences: One per frame in flight (tracks CPU/GPU synchronization)</item>
/// <item>Semaphores: One set per swapchain image (tracks GPU-GPU synchronization)</item>
/// </list>
/// 
/// <para><strong>Example with 2 frames in flight and 3 swapchain images:</strong></para>
/// <code>
/// Frame fences: [0, 1] (reused cyclically)
/// Image semaphores: [0, 1, 2] (indexed by acquired image index)
/// 
/// Acquire image 0 → Use ImageSync[0].ImageAvailable + ImageSync[0].RenderFinished + FrameSync[currentFrame].Fence
/// Acquire image 2 → Use ImageSync[2].ImageAvailable + ImageSync[2].RenderFinished + FrameSync[currentFrame].Fence
/// </code>
/// 
/// <para><strong>Reference:</strong></para>
/// See https://docs.vulkan.org/guide/latest/swapchain_semaphore_reuse.html
/// </remarks>
public record ImageSync
{
    /// <summary>
    /// Per-image semaphore signaled when rendering to this image is finished.
    /// Presentation waits for this before showing the image.
    /// Use one per swapchain image (not per frame).
    /// </summary>
    public required VkSemaphore RenderFinished { get; init; }

    /// <summary>
    /// Swapchain image index this sync set belongs to.
    /// </summary>
    public required uint ImageIndex { get; init; }
}
