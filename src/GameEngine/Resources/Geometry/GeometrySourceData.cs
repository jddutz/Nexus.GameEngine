namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Raw geometry data returned by geometry sources.
/// Contains vertex data ready for GPU buffer creation.
/// </summary>
public record GeometrySourceData
{
    /// <summary>
    /// Raw vertex data bytes.
    /// </summary>
    public required byte[] VertexData { get; init; }
    
    /// <summary>
    /// Number of vertices in the geometry.
    /// </summary>
    public required uint VertexCount { get; init; }
    
    /// <summary>
    /// Size in bytes of each vertex (stride).
    /// </summary>
    public required uint Stride { get; init; }
}
