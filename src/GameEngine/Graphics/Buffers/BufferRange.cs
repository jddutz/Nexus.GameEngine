namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Represents a memory range within a buffer
/// </summary>
public readonly struct BufferRange(int offset, int size)
{
    /// <summary>
    /// The offset in bytes from the start of the buffer
    /// </summary>
    public int Offset { get; } = offset;

    /// <summary>
    /// The size in bytes of this range
    /// </summary>
    public int Size { get; } = size;

    /// <summary>
    /// Whether this range is valid
    /// </summary>
    public bool IsValid => Size > 0;

    /// <summary>
    /// Gets the end offset of this range
    /// </summary>
    public int End => Offset + Size;

    /// <summary>
    /// Checks if this range overlaps with another range
    /// </summary>
    public bool Overlaps(BufferRange other)
    {
        return Offset < other.End && End > other.Offset;
    }

    /// <summary>
    /// Checks if this range contains the specified offset
    /// </summary>
    public bool Contains(int offset)
    {
        return offset >= Offset && offset < End;
    }

    public override string ToString() => $"Range[{Offset}..{End}] ({Size} bytes)";
}