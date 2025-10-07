namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Statistics about uniform buffer usage
/// </summary>
public readonly struct UniformBufferStatistics(int blockCount, int totalMemoryUsed, int boundBlocksThisFrame = 0, int updatesThisFrame = 0)
{
    /// <summary>
    /// Total number of uniform blocks
    /// </summary>
    public int BlockCount { get; } = blockCount;

    /// <summary>
    /// Total memory used by all blocks in bytes
    /// </summary>
    public int TotalMemoryUsed { get; } = totalMemoryUsed;

    /// <summary>
    /// Average size per block in bytes
    /// </summary>
    public float AverageBlockSize { get; } = blockCount > 0 ? (float)totalMemoryUsed / blockCount : 0f;

    /// <summary>
    /// Number of blocks bound in the current frame
    /// </summary>
    public int BoundBlocksThisFrame { get; } = boundBlocksThisFrame;

    /// <summary>
    /// Number of block updates in the current frame
    /// </summary>
    public int UpdatesThisFrame { get; } = updatesThisFrame;

    public override string ToString() =>
        $"UniformBuffers: {BlockCount} blocks, {TotalMemoryUsed} bytes, {AverageBlockSize:F1} avg size, {BoundBlocksThisFrame} bound, {UpdatesThisFrame} updates";
}