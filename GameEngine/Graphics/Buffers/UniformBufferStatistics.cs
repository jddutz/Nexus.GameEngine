namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Statistics about uniform buffer usage
/// </summary>
public readonly struct UniformBufferStatistics
{
    /// <summary>
    /// Total number of uniform blocks
    /// </summary>
    public int BlockCount { get; }

    /// <summary>
    /// Total memory used by all blocks in bytes
    /// </summary>
    public int TotalMemoryUsed { get; }

    /// <summary>
    /// Average size per block in bytes
    /// </summary>
    public float AverageBlockSize { get; }

    /// <summary>
    /// Number of blocks bound in the current frame
    /// </summary>
    public int BoundBlocksThisFrame { get; }

    /// <summary>
    /// Number of block updates in the current frame
    /// </summary>
    public int UpdatesThisFrame { get; }

    public UniformBufferStatistics(int blockCount, int totalMemoryUsed, int boundBlocksThisFrame = 0, int updatesThisFrame = 0)
    {
        BlockCount = blockCount;
        TotalMemoryUsed = totalMemoryUsed;
        AverageBlockSize = blockCount > 0 ? (float)totalMemoryUsed / blockCount : 0f;
        BoundBlocksThisFrame = boundBlocksThisFrame;
        UpdatesThisFrame = updatesThisFrame;
    }

    public override string ToString() =>
        $"UniformBuffers: {BlockCount} blocks, {TotalMemoryUsed} bytes, {AverageBlockSize:F1} avg size, {BoundBlocksThisFrame} bound, {UpdatesThisFrame} updates";
}