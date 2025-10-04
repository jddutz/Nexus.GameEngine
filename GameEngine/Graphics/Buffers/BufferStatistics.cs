namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Statistics about buffer usage
/// </summary>
public readonly struct BufferStatistics(int totalSize, int usedSize, int allocationCount, int orphanCount = 0)
{
    /// <summary>
    /// Total size of the buffer in bytes
    /// </summary>
    public int TotalSize { get; } = totalSize;

    /// <summary>
    /// Currently used size in bytes
    /// </summary>
    public int UsedSize { get; } = usedSize;

    /// <summary>
    /// Available size in bytes
    /// </summary>
    public int AvailableSize { get; } = totalSize - usedSize;

    /// <summary>
    /// Number of active allocations
    /// </summary>
    public int AllocationCount { get; } = allocationCount;

    /// <summary>
    /// Number of times the buffer has been orphaned
    /// </summary>
    public int OrphanCount { get; } = orphanCount;

    /// <summary>
    /// Gets the usage percentage (0.0 to 1.0)
    /// </summary>
    public float UsagePercentage => TotalSize > 0 ? (float)UsedSize / TotalSize : 0f;

    public override string ToString() =>
        $"Buffer: {UsedSize}/{TotalSize} bytes ({UsagePercentage:P1}), {AllocationCount} allocations, {OrphanCount} orphans";
}