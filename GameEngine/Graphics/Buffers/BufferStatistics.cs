namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Statistics about buffer usage
/// </summary>
public readonly struct BufferStatistics
{
    /// <summary>
    /// Total size of the buffer in bytes
    /// </summary>
    public int TotalSize { get; }

    /// <summary>
    /// Currently used size in bytes
    /// </summary>
    public int UsedSize { get; }

    /// <summary>
    /// Available size in bytes
    /// </summary>
    public int AvailableSize { get; }

    /// <summary>
    /// Number of active allocations
    /// </summary>
    public int AllocationCount { get; }

    /// <summary>
    /// Number of times the buffer has been orphaned
    /// </summary>
    public int OrphanCount { get; }

    public BufferStatistics(int totalSize, int usedSize, int allocationCount, int orphanCount = 0)
    {
        TotalSize = totalSize;
        UsedSize = usedSize;
        AvailableSize = totalSize - usedSize;
        AllocationCount = allocationCount;
        OrphanCount = orphanCount;
    }

    /// <summary>
    /// Gets the usage percentage (0.0 to 1.0)
    /// </summary>
    public float UsagePercentage => TotalSize > 0 ? (float)UsedSize / TotalSize : 0f;

    public override string ToString() =>
        $"Buffer: {UsedSize}/{TotalSize} bytes ({UsagePercentage:P1}), {AllocationCount} allocations, {OrphanCount} orphans";
}