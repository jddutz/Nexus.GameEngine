namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Pure data definition for geometry resources
/// </summary>
public record GeometryDefinition : IResourceDefinition
{
    /// <summary>
    /// Unique name for this geometry resource
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Vertex data for the geometry
    /// </summary>
    public required ReadOnlyMemory<float> Vertices { get; init; }

    /// <summary>
    /// Index data for the geometry (optional - if null, uses vertex order)
    /// </summary>
    public ReadOnlyMemory<uint>? Indices { get; init; }

    /// <summary>
    /// Vertex attribute layout description
    /// </summary>
    public required IReadOnlyList<VertexAttribute> Attributes { get; init; }

    /// <summary>
    /// Usage hint for the buffer (Static, Dynamic, Stream)
    /// </summary>
    public BufferUsageHint UsageHint { get; init; } = BufferUsageHint.StaticDraw;
}