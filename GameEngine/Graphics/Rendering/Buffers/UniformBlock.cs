namespace Nexus.GameEngine.Graphics.Rendering.Buffers;

/// <summary>
/// Represents a uniform buffer block that can be shared across shaders
/// </summary>
public class UniformBlock
{
    /// <summary>
    /// Name of the uniform block
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Size of the block in bytes
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// OpenGL buffer ID
    /// </summary>
    public uint BufferId { get; }

    /// <summary>
    /// Binding point for this uniform block
    /// </summary>
    public int BindingPoint { get; }

    /// <summary>
    /// Whether this block has been bound in the current frame
    /// </summary>
    public bool IsBound { get; internal set; }

    /// <summary>
    /// Last frame this block was updated
    /// </summary>
    public long LastUpdateFrame { get; internal set; }

    public UniformBlock(string name, int size, uint bufferId, int bindingPoint)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Size = size;
        BufferId = bufferId;
        BindingPoint = bindingPoint;
    }

    public override string ToString() => $"UniformBlock[{Name}] Size:{Size} BindingPoint:{BindingPoint} BufferId:{BufferId}";
}