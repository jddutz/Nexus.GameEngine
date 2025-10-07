using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Models;

/// <summary>
/// Represents a 3D mesh with vertex and index data
/// </summary>
public class Mesh
{
    /// <summary>
    /// Unique identifier for the mesh
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// OpenGL vertex buffer object ID
    /// </summary>
    public uint VertexBufferId { get; set; }

    /// <summary>
    /// OpenGL index buffer object ID (optional)
    /// </summary>
    public uint? IndexBufferId { get; set; }

    /// <summary>
    /// Number of vertices in the mesh
    /// </summary>
    public int VertexCount { get; set; }

    /// <summary>
    /// Number of indices in the mesh (for indexed rendering)
    /// </summary>
    public int IndexCount { get; set; }

    /// <summary>
    /// Material ID associated with this mesh
    /// </summary>
    public int MaterialId { get; set; }

    /// <summary>
    /// Vertex Array Object ID for OpenGL state management
    /// </summary>
    public uint? VertexArrayId { get; set; }

    /// <summary>
    /// Bounding box for frustum culling
    /// </summary>
    public BoundingBox BoundingBox { get; set; }

    /// <summary>
    /// Create a mesh with vertex data only
    /// </summary>
    /// <param name="vertexBufferId">Vertex buffer object ID</param>
    /// <param name="vertexCount">Number of vertices</param>
    /// <param name="materialId">Material ID</param>
    public Mesh(uint vertexBufferId, int vertexCount, int materialId = 0)
    {
        VertexBufferId = vertexBufferId;
        VertexCount = vertexCount;
        MaterialId = materialId;
    }

    /// <summary>
    /// Create a mesh with vertex and index data
    /// </summary>
    /// <param name="vertexBufferId">Vertex buffer object ID</param>
    /// <param name="indexBufferId">Index buffer object ID</param>
    /// <param name="vertexCount">Number of vertices</param>
    /// <param name="indexCount">Number of indices</param>
    /// <param name="materialId">Material ID</param>
    public Mesh(uint vertexBufferId, uint indexBufferId, int vertexCount, int indexCount, int materialId = 0)
    {
        VertexBufferId = vertexBufferId;
        IndexBufferId = indexBufferId;
        VertexCount = vertexCount;
        IndexCount = indexCount;
        MaterialId = materialId;
    }
}

/// <summary>
/// Axis-aligned bounding box for culling calculations
/// </summary>
public struct BoundingBox(Vector3D<float> min, Vector3D<float> max)
{
    public Vector3D<float> Min { get; set; } = min;
    public Vector3D<float> Max { get; set; } = max;

    /// <summary>
    /// Get the center point of the bounding box
    /// </summary>
    public Vector3D<float> Center => (Min + Max) * 0.5f;

    /// <summary>
    /// Get the size of the bounding box
    /// </summary>
    public Vector3D<float> Size => Max - Min;
}