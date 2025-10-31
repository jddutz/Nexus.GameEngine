namespace Nexus.GameEngine.Graphics.Commands;

/// <summary>
/// Manages multiple command pools for different queue families and use cases.
/// Provides centralized access to command buffer allocation across the application.
/// </summary>
/// <remarks>
/// <para><strong>Responsibilities:</strong></para>
/// <list type="bullet">
/// <item>Create and manage command pools for different queue families</item>
/// <item>Provide typed access to pools (Graphics, Transfer, Compute)</item>
/// <item>Lifecycle management for all pools</item>
/// <item>Statistics aggregation across all pools</item>
/// </list>
/// 
/// <para><strong>Thread Safety:</strong></para>
/// This manager is thread-safe for pool creation and retrieval. However, individual
/// command pools are NOT thread-safe. For multi-threaded rendering, create separate
/// pools per thread using CreatePool().
/// 
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// // Get the graphics pool for main rendering
/// var graphicsPool = commandPoolManager.GetOrCreatePool(CommandPoolType.Graphics);
/// var cmdBuffers = graphicsPool.AllocateCommandBuffers(3);
/// 
/// // Get a transient pool for short-lived commands
/// var transientPool = commandPoolManager.GetOrCreatePool(CommandPoolType.TransientGraphics);
/// var tempBuffer = transientPool.AllocateCommandBuffers(1)[0];
/// 
/// // Reset all pools at end of frame
/// commandPoolManager.ResetAll();
/// </code>
/// </remarks>
public interface ICommandPoolManager : IDisposable
{
    /// <summary>
    /// Gets or creates a command pool of the specified type.
    /// Returns a cached pool if one exists, creates a new one if not.
    /// </summary>
    /// <param name="type">Type of command pool to get or create</param>
    /// <returns>Command pool instance</returns>
    /// <remarks>
    /// This method is thread-safe. Pools are created lazily on first access.
    /// The same pool instance is returned for subsequent calls with the same type.
    /// </remarks>
    ICommandPool GetOrCreatePool(CommandPoolType type);

    /// <summary>
    /// Creates a new command pool with custom settings.
    /// Use this for specialized pools (e.g., per-thread pools for multi-threading).
    /// </summary>
    /// <param name="queueFamilyIndex">Queue family index for this pool</param>
    /// <param name="allowIndividualReset">Whether individual buffers can be reset</param>
    /// <param name="transient">Whether buffers are short-lived</param>
    /// <returns>New command pool instance</returns>
    /// <remarks>
    /// Unlike GetOrCreatePool(), this always creates a new pool instance.
    /// You are responsible for disposing the returned pool when done.
    /// </remarks>
    ICommandPool CreatePool(uint queueFamilyIndex, bool allowIndividualReset = false, bool transient = true);

    /// <summary>
    /// Gets the graphics command pool.
    /// Convenience property equivalent to GetOrCreatePool(CommandPoolType.Graphics).
    /// </summary>
    ICommandPool GraphicsPool { get; }

    /// <summary>
    /// Gets the transfer command pool (if available).
    /// Returns null if device doesn't have a dedicated transfer queue.
    /// </summary>
    ICommandPool? TransferPool { get; }

    /// <summary>
    /// Gets the compute command pool (if available).
    /// Returns null if device doesn't have a dedicated compute queue.
    /// </summary>
    ICommandPool? ComputePool { get; }

    /// <summary>
    /// Resets all managed command pools.
    /// Typically called at the end of each frame to recycle command buffers.
    /// </summary>
    /// <param name="flags">Reset flags applied to all pools</param>
    /// <remarks>
    /// Must ensure all command buffers have finished executing before calling this.
    /// Use fences or vkQueueWaitIdle() to synchronize properly.
    /// </remarks>
    void ResetAll(CommandPoolResetFlags flags = CommandPoolResetFlags.None);

    /// <summary>
    /// Trims all managed command pools to reduce memory usage.
    /// </summary>
    /// <remarks>
    /// Call this after resetting pools to reclaim unused memory.
    /// Has a performance cost, so use sparingly (e.g., after loading screens).
    /// </remarks>
    void TrimAll();

    /// <summary>
    /// Gets aggregated statistics for all managed pools.
    /// </summary>
    /// <returns>Summary statistics across all pools</returns>
    CommandPoolManagerStatistics GetStatistics();

    /// <summary>
    /// Gets statistics for all individual pools.
    /// </summary>
    /// <returns>Collection of per-pool statistics</returns>
    IEnumerable<(CommandPoolType Type, CommandPoolStatistics Stats)> GetAllPoolStatistics();
}
