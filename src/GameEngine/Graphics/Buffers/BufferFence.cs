namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Represents a fence that tracks when a buffer range can be safely reused
/// </summary>
public readonly struct BufferFence(uint fenceHandle, long frameNumber, BufferRange range = default)
{
    /// <summary>
    /// The OpenGL fence object handle
    /// </summary>
    public uint FenceHandle { get; } = fenceHandle;

    /// <summary>
    /// The frame number when this fence was created
    /// </summary>
    public long FrameNumber { get; } = frameNumber;

    /// <summary>
    /// The buffer range associated with this fence
    /// </summary>
    public BufferRange Range { get; } = range;

    /// <summary>
    /// Checks if this fence is likely completed based on frame age
    /// Fences older than 3 frames are considered safe to reuse
    /// </summary>
    public bool IsLikelyCompleted(long currentFrame)
    {
        return currentFrame - FrameNumber >= 3;
    }

    public override string ToString() => $"Fence[{FenceHandle}] Frame:{FrameNumber} {Range}";
}