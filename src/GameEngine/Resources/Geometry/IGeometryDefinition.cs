namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Defines geometry data for GPU vertex buffers.
/// Geometry definitions are pure data - no Vulkan resources.
/// </summary>
public interface IGeometryDefinition : IResourceDefinition
{    
    /// <summary>
    /// Gets the vertex data as a byte span for uploading to GPU.
    /// </summary>
    ReadOnlySpan<byte> GetVertexData();
    
    /// <summary>
    /// Number of vertices in this geometry.
    /// </summary>
    uint VertexCount { get; }
    
    /// <summary>
    /// Size in bytes of each vertex (stride).
    /// </summary>
    uint Stride { get; }
}
