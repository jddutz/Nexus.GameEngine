namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Describes a vertex attribute (position, normal, texture coordinate, etc.)
/// </summary>
public record VertexAttribute
{
    /// <summary>
    /// Attribute location/index in the shader
    /// </summary>
    public required uint Location { get; init; }

    /// <summary>
    /// Number of components (1, 2, 3, or 4)
    /// </summary>
    public required int ComponentCount { get; init; }

    /// <summary>
    /// Data type of the components
    /// </summary>
    public VertexAttribPointerType Type { get; init; } = VertexAttribPointerType.Float;

    /// <summary>
    /// Whether to normalize integer values to [0,1] or [-1,1]
    /// </summary>
    public bool Normalized { get; init; } = false;

    /// <summary>
    /// Stride between consecutive vertices (0 = tightly packed)
    /// </summary>
    public uint Stride { get; init; } = 0;

    /// <summary>
    /// Offset within the vertex data
    /// </summary>
    public nint Offset { get; init; } = 0;

    /// <summary>
    /// Creates a float position attribute at location 0
    /// </summary>
    public static VertexAttribute Position2D(uint location = 0) => new()
    {
        Location = location,
        ComponentCount = 2,
        Type = VertexAttribPointerType.Float,
        Stride = 0,
        Offset = 0
    };

    /// <summary>
    /// Creates a float position attribute at location 0
    /// </summary>
    public static VertexAttribute Position3D(uint location = 0) => new()
    {
        Location = location,
        ComponentCount = 3,
        Type = VertexAttribPointerType.Float,
        Stride = 0,
        Offset = 0
    };

    /// <summary>
    /// Creates a float texture coordinate attribute
    /// </summary>
    public static VertexAttribute TexCoord2D(uint location, nint offset = 0, uint stride = 0) => new()
    {
        Location = location,
        ComponentCount = 2,
        Type = VertexAttribPointerType.Float,
        Stride = stride,
        Offset = offset
    };
}