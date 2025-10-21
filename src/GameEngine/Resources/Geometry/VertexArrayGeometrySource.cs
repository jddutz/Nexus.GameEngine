using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Creates geometry from a vertex array.
/// Most common source for UI primitives and procedural geometry.
/// </summary>
/// <typeparam name="TVertex">Vertex struct type</typeparam>
public class VertexArrayGeometrySource<TVertex> : IGeometrySource
    where TVertex : struct
{
    private readonly TVertex[] _vertices;
    
    /// <summary>
    /// Creates a geometry source from a vertex array.
    /// </summary>
    /// <param name="vertices">Array of vertices</param>
    public VertexArrayGeometrySource(TVertex[] vertices)
    {
        _vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
    }
    
    /// <summary>
    /// Loads the geometry data by converting the vertex array to bytes.
    /// </summary>
    public GeometrySourceData Load()
    {
        var stride = (uint)Unsafe.SizeOf<TVertex>();
        var vertexData = MemoryMarshal.AsBytes(_vertices.AsSpan()).ToArray();
        
        return new GeometrySourceData
        {
            VertexData = vertexData,
            VertexCount = (uint)_vertices.Length,
            Stride = stride
        };
    }
}
