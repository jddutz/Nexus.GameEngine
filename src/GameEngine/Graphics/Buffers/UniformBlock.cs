namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Represents a uniform buffer block that can be shared across shaders
/// </summary>
public class UniformBlock(string name, int size, uint bufferId, int bindingPoint)
{
    /// <summary>
    /// Name of the uniform block
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// Size of the block in bytes
    /// </summary>
    public int Size { get; } = size;

    /// <summary>
    /// OpenGL buffer ID
    /// </summary>
    public uint BufferId { get; } = bufferId;

    /// <summary>
    /// Binding point for this uniform block
    /// </summary>
    public int BindingPoint { get; } = bindingPoint;

    /// <summary>
    /// Whether this block has been bound in the current frame
    /// </summary>
    public bool IsBound { get; internal set; }

    /// <summary>
    /// Last frame this block was updated
    /// </summary>
    public long LastUpdateFrame { get; internal set; }

    public override string ToString() => $"UniformBlock[{Name}] Size:{Size} BindingPoint:{BindingPoint} BufferId:{BufferId}";
}