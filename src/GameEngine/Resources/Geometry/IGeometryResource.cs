using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Interface for GPU geometry resources (vertex buffer and memory).
/// </summary>
public interface IGeometryResource
{
    /// <summary>
    /// Vulkan vertex buffer handle.
    /// </summary>
    Buffer Buffer { get; }
    
    /// <summary>
    /// Vulkan device memory allocated for the vertex buffer.
    /// </summary>
    DeviceMemory Memory { get; }
    
    /// <summary>
    /// Number of vertices in this geometry.
    /// </summary>
    uint VertexCount { get; }
    
    /// <summary>
    /// Size in bytes of each vertex (stride).
    /// </summary>
    uint Stride { get; }
    
    /// <summary>
    /// Name of this geometry resource (from definition).
    /// </summary>
    string Name { get; }
}
