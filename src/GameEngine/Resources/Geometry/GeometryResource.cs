using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Handle to GPU geometry resources (vertex buffer and memory).
/// Managed by GeometrySubManager - components should not create or destroy these directly.
/// </summary>
public class GeometryResource
{
    /// <summary>
    /// Vulkan vertex buffer handle.
    /// </summary>
    public Buffer Buffer { get; }
    
    /// <summary>
    /// Vulkan device memory allocated for the vertex buffer.
    /// </summary>
    public DeviceMemory Memory { get; }
    
    /// <summary>
    /// Number of vertices in this geometry.
    /// </summary>
    public uint VertexCount { get; }
    
    /// <summary>
    /// Size in bytes of each vertex (stride).
    /// </summary>
    public uint Stride { get; }
    
    /// <summary>
    /// Name of this geometry resource (from definition).
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Internal constructor - only GeometrySubManager should create these.
    /// </summary>
    internal GeometryResource(Buffer buffer, DeviceMemory memory, uint vertexCount, uint stride, string name)
    {
        Buffer = buffer;
        Memory = memory;
        VertexCount = vertexCount;
        Stride = stride;
        Name = name;
    }
}
