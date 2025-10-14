namespace Nexus.GameEngine.Graphics.Commands;

/// <summary>
/// Statistics about command pool usage and performance.
/// Useful for debugging, profiling, and memory management.
/// </summary>
public record CommandPoolStatistics
{
    /// <summary>
    /// Total number of command buffers currently allocated from this pool.
    /// </summary>
    public int TotalAllocatedBuffers { get; init; }

    /// <summary>
    /// Number of primary command buffers allocated.
    /// </summary>
    public int PrimaryBufferCount { get; init; }

    /// <summary>
    /// Number of secondary command buffers allocated.
    /// </summary>
    public int SecondaryBufferCount { get; init; }

    /// <summary>
    /// Total number of allocation requests made to this pool.
    /// </summary>
    public int TotalAllocationRequests { get; init; }

    /// <summary>
    /// Total number of free requests made to this pool.
    /// </summary>
    public int TotalFreeRequests { get; init; }

    /// <summary>
    /// Number of times this pool has been reset.
    /// </summary>
    public int ResetCount { get; init; }

    /// <summary>
    /// Number of times this pool has been trimmed.
    /// </summary>
    public int TrimCount { get; init; }

    /// <summary>
    /// Estimated memory usage by this pool (bytes).
    /// This is an approximation based on Vulkan's internal allocation strategy.
    /// </summary>
    public long EstimatedMemoryUsageBytes { get; init; }

    /// <summary>
    /// Queue family index this pool is associated with.
    /// </summary>
    public uint QueueFamilyIndex { get; init; }

    /// <summary>
    /// Whether individual command buffer reset is allowed.
    /// </summary>
    public bool AllowsIndividualReset { get; init; }

    /// <summary>
    /// Time when this pool was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Time when this pool was last reset.
    /// </summary>
    public DateTime? LastResetAt { get; init; }
}
