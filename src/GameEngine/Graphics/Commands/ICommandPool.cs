using VkCommandPool = Silk.NET.Vulkan.CommandPool;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Commands;

/// <summary>
/// Manages Vulkan command buffer allocation and lifecycle.
/// Provides thread-safe command buffer pool management with reset capabilities.
/// </summary>
/// <remarks>
/// <para><strong>Responsibilities:</strong></para>
/// <list type="bullet">
/// <item>Command buffer allocation from queue-specific pools</item>
/// <item>Command buffer lifecycle management (allocate, free, reset)</item>
/// <item>Thread-safe pool access for multi-threaded rendering</item>
/// <item>Per-frame command buffer recycling</item>
/// </list>
/// 
/// <para><strong>Thread Safety:</strong></para>
/// Command pools are NOT thread-safe in Vulkan. For multi-threaded rendering,
/// create one ICommandPool instance per thread. This implementation is designed
/// for single-threaded use per instance.
/// 
/// <para><strong>Queue Family:</strong></para>
/// Command buffers must be allocated from a pool specific to a queue family.
/// Typically, you'll have pools for:
/// <list type="bullet">
/// <item>Graphics queue family (most common)</item>
/// <item>Transfer queue family (for async uploads)</item>
/// <item>Compute queue family (for compute workloads)</item>
/// </list>
/// 
/// <para><strong>Lifecycle:</strong></para>
/// <list type="number">
/// <item>Create pool during initialization</item>
/// <item>Allocate command buffers as needed</item>
/// <item>Reset pool after frame to reuse buffers (optional)</item>
/// <item>Free specific buffers when no longer needed (optional)</item>
/// <item>Dispose pool to destroy all resources</item>
/// </list>
/// 
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// // Create pool for graphics queue
/// var pool = commandPoolManager.GetOrCreatePool(CommandPoolType.Graphics);
/// 
/// // Allocate primary command buffers for rendering
/// var commandBuffers = pool.AllocateCommandBuffers(3, CommandBufferLevel.Primary);
/// 
/// // Use command buffers...
/// 
/// // Reset pool at end of frame to reuse buffers
/// pool.Reset(CommandPoolResetFlags.None);
/// </code>
/// </remarks>
public interface ICommandPool : IDisposable
{
    /// <summary>
    /// Gets the native Vulkan command pool handle.
    /// </summary>
    VkCommandPool Pool { get; }

    /// <summary>
    /// Gets the queue family index this pool belongs to.
    /// Command buffers from this pool can only be submitted to queues from this family.
    /// </summary>
    uint QueueFamilyIndex { get; }

    /// <summary>
    /// Gets whether this pool allows individual command buffer reset.
    /// If false, must reset entire pool via Reset() method.
    /// </summary>
    bool AllowIndividualReset { get; }

    /// <summary>
    /// Allocates command buffers from this pool.
    /// </summary>
    /// <param name="count">Number of command buffers to allocate</param>
    /// <param name="level">Buffer level (Primary or Secondary)</param>
    /// <returns>Array of allocated command buffer handles</returns>
    /// <exception cref="InvalidOperationException">If allocation fails</exception>
    /// <remarks>
    /// <para><strong>Primary vs Secondary:</strong></para>
    /// <list type="bullet">
    /// <item>Primary: Can be submitted to queue directly. Can execute secondary buffers.</item>
    /// <item>Secondary: Must be executed within primary buffer. Useful for multi-threading.</item>
    /// </list>
    /// 
    /// <para>
    /// Command buffers remain allocated until explicitly freed via FreeCommandBuffers()
    /// or until the pool is reset/destroyed. Reusing buffers by resetting them is
    /// more efficient than freeing and reallocating.
    /// </para>
    /// </remarks>
    CommandBuffer[] AllocateCommandBuffers(uint count, CommandBufferLevel level = CommandBufferLevel.Primary);

    /// <summary>
    /// Frees specific command buffers back to the pool.
    /// Buffers can be reallocated after being freed.
    /// </summary>
    /// <param name="commandBuffers">Array of command buffers to free</param>
    /// <remarks>
    /// This is optional - command buffers are automatically freed when the pool
    /// is reset or destroyed. Explicit freeing is useful for long-lived pools
    /// where individual buffers need to be recycled.
    /// </remarks>
    void FreeCommandBuffers(CommandBuffer[] commandBuffers);

    /// <summary>
    /// Resets all command buffers in this pool to initial state.
    /// More efficient than freeing and reallocating buffers.
    /// </summary>
    /// <param name="flags">Reset behavior flags</param>
    /// <remarks>
    /// <para><strong>Reset Flags:</strong></para>
    /// <list type="bullet">
    /// <item>None: Default behavior - command buffers can be reused</item>
    /// <item>ReleaseResourcesBit: Releases memory back to system (slower, frees memory)</item>
    /// </list>
    /// 
    /// <para>
    /// Typically called once per frame after command buffers have been submitted
    /// and executed. This allows reusing the same command buffers for the next frame.
    /// </para>
    /// 
    /// <para><strong>Important:</strong></para>
    /// Must wait for all command buffers from this pool to finish executing
    /// before calling Reset(). Use fences or vkDeviceWaitIdle() to ensure safety.
    /// </remarks>
    void Reset(CommandPoolResetFlags flags = CommandPoolResetFlags.None);

    /// <summary>
    /// Trims the pool to reduce memory usage.
    /// Releases unused memory allocations back to the device.
    /// </summary>
    /// <remarks>
    /// Optional optimization for long-running applications. Called after resetting
    /// the pool to reclaim memory from temporary allocations. Has a performance cost,
    /// so use sparingly (e.g., after loading screens or level transitions).
    /// </remarks>
    void Trim();

    /// <summary>
    /// Gets statistics about this command pool's usage.
    /// Useful for debugging and profiling.
    /// </summary>
    /// <returns>Statistics including allocation counts and memory usage</returns>
    CommandPoolStatistics GetStatistics();
}
