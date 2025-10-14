namespace Nexus.GameEngine.Graphics.Commands;

/// <summary>
/// Aggregated statistics for all command pools managed by ICommandPoolManager.
/// </summary>
public record CommandPoolManagerStatistics
{
    /// <summary>
    /// Total number of managed command pools.
    /// </summary>
    public int TotalPools { get; init; }

    /// <summary>
    /// Total command buffers allocated across all pools.
    /// </summary>
    public int TotalAllocatedBuffers { get; init; }

    /// <summary>
    /// Total primary buffers across all pools.
    /// </summary>
    public int TotalPrimaryBuffers { get; init; }

    /// <summary>
    /// Total secondary buffers across all pools.
    /// </summary>
    public int TotalSecondaryBuffers { get; init; }

    /// <summary>
    /// Total allocation requests across all pools.
    /// </summary>
    public int TotalAllocationRequests { get; init; }

    /// <summary>
    /// Total free requests across all pools.
    /// </summary>
    public int TotalFreeRequests { get; init; }

    /// <summary>
    /// Total pool reset operations.
    /// </summary>
    public int TotalResets { get; init; }

    /// <summary>
    /// Total estimated memory usage across all pools (bytes).
    /// </summary>
    public long TotalEstimatedMemoryBytes { get; init; }

    /// <summary>
    /// Number of graphics pools.
    /// </summary>
    public int GraphicsPoolCount { get; init; }

    /// <summary>
    /// Number of transfer pools.
    /// </summary>
    public int TransferPoolCount { get; init; }

    /// <summary>
    /// Number of compute pools.
    /// </summary>
    public int ComputePoolCount { get; init; }
}
