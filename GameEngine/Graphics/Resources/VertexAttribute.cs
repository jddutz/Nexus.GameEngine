using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// Represents a vertex attribute definition for OpenGL vertex buffer layout.
/// </summary>
public record VertexAttribute
{
    /// <summary>
    /// The attribute location index in the vertex shader.
    /// </summary>
    public int Location { get; init; }

    /// <summary>
    /// The number of components per vertex attribute (1, 2, 3, or 4).
    /// </summary>
    public int Size { get; init; }

    /// <summary>
    /// The data type of each component in the attribute.
    /// </summary>
    public VertexAttribPointerType Type { get; init; }

    /// <summary>
    /// The byte offset between consecutive vertex attributes.
    /// </summary>
    public int Stride { get; init; }

    /// <summary>
    /// The byte offset of the first component of this attribute in the vertex buffer.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Whether fixed-point data values should be normalized when accessed.
    /// </summary>
    public bool Normalized { get; init; } = false;
}