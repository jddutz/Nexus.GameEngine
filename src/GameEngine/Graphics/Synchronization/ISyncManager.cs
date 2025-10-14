namespace Nexus.GameEngine.Graphics.Synchronization;

/// <summary>
/// Manages Vulkan synchronization primitives for frame rendering.
/// Implements the "frames in flight" pattern to prevent CPU/GPU stalls and enable parallelism.
/// </summary>
/// <remarks>
/// <para><strong>Frames in Flight Pattern:</strong></para>
/// Instead of waiting for each frame to fully complete before starting the next, we allow
/// multiple frames to be in different stages of the pipeline simultaneously:
/// 
/// <code>
/// Frame 0: [Rendering on GPU]
/// Frame 1: [Waiting for GPU] ← CPU preparing commands
/// Frame 2: [Being presented]  
/// </code>
/// 
/// This maximizes GPU utilization and prevents CPU/GPU idle time.
/// 
/// <para><strong>Synchronization Requirements:</strong></para>
/// <list type="number">
/// <item>CPU must not overwrite command buffers still in use by GPU (fence)</item>
/// <item>GPU must not render to swapchain image still being displayed (ImageAvailable semaphore)</item>
/// <item>Presentation must not occur before rendering finishes (RenderFinished semaphore)</item>
/// </list>
/// 
/// <para><strong>Typical Configuration:</strong></para>
/// <list type="bullet">
/// <item>MaxFramesInFlight = 2 (double buffering) - Good balance</item>
/// <item>MaxFramesInFlight = 3 (triple buffering) - More throughput, higher latency</item>
/// <item>MaxFramesInFlight = 1 (single buffering) - Simple but may cause stalls</item>
/// </list>
/// 
/// <para><strong>Usage Pattern:</strong></para>
/// <code>
/// // At frame start
/// var frameSync = syncManager.GetFrameSync(currentFrameIndex);
/// 
/// // Wait for this frame slot to be available
/// syncManager.WaitForFence(frameSync.InFlightFence);
/// syncManager.ResetFence(frameSync.InFlightFence);
/// 
/// // Acquire image (signals ImageAvailable when ready)
/// swapchain.AcquireNextImage(frameSync.ImageAvailable, out imageIndex);
/// 
/// // Submit rendering (waits for ImageAvailable, signals RenderFinished, signals InFlightFence)
/// SubmitCommands(frameSync);
/// 
/// // Present (waits for RenderFinished)
/// swapchain.Present(imageIndex, frameSync.RenderFinished);
/// 
/// // Move to next frame
/// currentFrameIndex = (currentFrameIndex + 1) % MaxFramesInFlight;
/// </code>
/// </remarks>
public interface ISyncManager : IDisposable
{
    /// <summary>
    /// Gets the maximum number of frames that can be in flight simultaneously.
    /// Typically 2 (double buffering) or 3 (triple buffering).
    /// </summary>
    int MaxFramesInFlight { get; }

    /// <summary>
    /// Gets synchronization primitives for the specified frame index.
    /// Frame index must be in range [0, MaxFramesInFlight).
    /// </summary>
    /// <param name="frameIndex">Zero-based frame index</param>
    /// <returns>Synchronization primitives for this frame slot</returns>
    /// <exception cref="ArgumentOutOfRangeException">If frameIndex is invalid</exception>
    /// <remarks>
    /// The same FrameSync object is returned for the same frame index across calls.
    /// This allows the calling code to cycle through frame indices and reuse sync objects.
    /// </remarks>
    FrameSync GetFrameSync(int frameIndex);

    /// <summary>
    /// Gets semaphores for a specific swapchain image.
    /// Use this to avoid semaphore reuse conflicts when frames in flight != swapchain image count.
    /// </summary>
    /// <param name="imageIndex">Swapchain image index</param>
    /// <returns>Image-specific semaphores</returns>
    /// <remarks>
    /// This method creates semaphores on-demand for each swapchain image index.
    /// Recommended pattern: Use per-image semaphores + per-frame fences.
    /// See: https://docs.vulkan.org/guide/latest/swapchain_semaphore_reuse.html
    /// </remarks>
    ImageSync GetImageSync(uint imageIndex);

    /// <summary>
    /// Waits for a fence to be signaled, with optional timeout.
    /// Blocks the CPU until the GPU signals the fence.
    /// </summary>
    /// <param name="fence">Fence to wait for</param>
    /// <param name="timeoutNanoseconds">Maximum time to wait (default: infinite)</param>
    /// <returns>True if fence was signaled, false if timeout occurred</returns>
    /// <remarks>
    /// Typical usage: Wait for previous frame to finish before reusing its command buffers.
    /// Use UINT64_MAX for infinite timeout (recommended for frame synchronization).
    /// </remarks>
    bool WaitForFence(Silk.NET.Vulkan.Fence fence, ulong timeoutNanoseconds = ulong.MaxValue);

    /// <summary>
    /// Waits for multiple fences to be signaled, with optional timeout.
    /// </summary>
    /// <param name="fences">Array of fences to wait for</param>
    /// <param name="waitAll">If true, waits for all fences; if false, waits for any fence</param>
    /// <param name="timeoutNanoseconds">Maximum time to wait (default: infinite)</param>
    /// <returns>True if condition was met, false if timeout occurred</returns>
    bool WaitForFences(Silk.NET.Vulkan.Fence[] fences, bool waitAll = true, ulong timeoutNanoseconds = ulong.MaxValue);

    /// <summary>
    /// Resets a fence to unsignaled state.
    /// Must be called after WaitForFence and before submitting new commands.
    /// </summary>
    /// <param name="fence">Fence to reset</param>
    /// <remarks>
    /// Fence must not be in pending state (associated with in-flight commands).
    /// Typical usage: Reset fence immediately after waiting for it.
    /// </remarks>
    void ResetFence(Silk.NET.Vulkan.Fence fence);

    /// <summary>
    /// Resets multiple fences to unsignaled state.
    /// </summary>
    /// <param name="fences">Array of fences to reset</param>
    void ResetFences(Silk.NET.Vulkan.Fence[] fences);

    /// <summary>
    /// Waits for the device to become idle (all GPU work to complete).
    /// Heavy operation - use sparingly (e.g., before cleanup, shader hot-reload).
    /// </summary>
    /// <remarks>
    /// This blocks until the GPU has finished processing all submitted commands.
    /// Useful before destroying resources or during major state changes.
    /// Do NOT call this every frame - defeats the purpose of async rendering!
    /// </remarks>
    void DeviceWaitIdle();

    /// <summary>
    /// Waits for a specific queue to become idle.
    /// Lighter than DeviceWaitIdle but still blocks.
    /// </summary>
    /// <param name="queue">Queue to wait for</param>
    void QueueWaitIdle(Silk.NET.Vulkan.Queue queue);

    /// <summary>
    /// Gets statistics about synchronization usage and performance.
    /// Useful for debugging frame pacing and GPU utilization.
    /// </summary>
    /// <returns>Synchronization statistics</returns>
    SyncStatistics GetStatistics();
}
