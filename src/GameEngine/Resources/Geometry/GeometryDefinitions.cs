namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Provides static geometry definitions for common primitives.
/// These definitions are used to create and cache GPU geometry resources.
/// </summary>
public static class GeometryDefinitions
{
    /// <summary>
    /// Position-only quad for uniform color rendering (color from push constants).
    /// Vertex format: Position(Vec2) = 8 bytes per vertex.
    /// </summary>
    public static readonly GeometryDefinition UniformColorQuad = new()
    {
        Name = "UniformColorQuad",
        Source = new VertexArrayGeometrySource<Vector2D<float>>(
        [
            new(-1f, -1f),  // Top-left
            new(-1f,  1f),  // Bottom-left
            new( 1f, -1f),  // Top-right
            new( 1f,  1f),  // Bottom-right
        ])
    };

    /// <summary>
    /// Position-only quad for per-vertex color rendering (colors from UBO).
    /// Vertex format: Position(Vec2) = 8 bytes per vertex.
    /// </summary>
    public static readonly GeometryDefinition PerVertexColorQuad = new()
    {
        Name = "PerVertexColorQuad",
        Source = new VertexArrayGeometrySource<Vector2D<float>>(
        [
            new(-1f, -1f),  // Top-left
            new(-1f,  1f),  // Bottom-left
            new( 1f, -1f),  // Top-right
            new( 1f,  1f),  // Bottom-right
        ])
    };

    /// <summary>
    /// Textured quad with UV coordinates.
    /// Vertex format: Position(Vec2) + TexCoord(Vec2) = 16 bytes per vertex.
    /// </summary>
    public static readonly GeometryDefinition TexturedQuad = new()
    {
        Name = "TexturedQuad",
        Source = new VertexArrayGeometrySource<Vertex<Vector2D<float>, Vector2D<float>>>(
        [
            new() { Position = new(-1f, -1f), Attribute1 = new(0f, 0f) }, // Top-left
            new() { Position = new(-1f,  1f), Attribute1 = new(0f, 1f) }, // Bottom-left
            new() { Position = new( 1f, -1f), Attribute1 = new(1f, 0f) }, // Top-right
            new() { Position = new( 1f,  1f), Attribute1 = new(1f, 1f) }  // Bottom-right
        ])
    };

    /// <summary>
    /// Creates a colored quad with per-vertex colors embedded in vertex data.
    /// Vertex format: Position(Vec2) + Color(Vec4) = 24 bytes per vertex.
    /// </summary>
    /// <param name="colors">Four colors for TL, BL, TR, BR vertices</param>
    /// <returns>Geometry definition for the colored quad</returns>
    public static GeometryDefinition CreateColorQuad(Vector4D<float>[] colors)
    {
        if (colors.Length != 4)
            throw new ArgumentException("Colors array must have exactly 4 elements (TL, BL, TR, BR)", nameof(colors));

        return new GeometryDefinition
        {
            Name = $"ColorQuad_{colors[0].GetHashCode()}_{colors[1].GetHashCode()}_{colors[2].GetHashCode()}_{colors[3].GetHashCode()}",
            Source = new ProceduralGeometrySource(() =>
            {
                var positions = new Vector2D<float>[]
                {
                    new(-1f, -1f),  // Top-left
                    new(-1f,  1f),  // Bottom-left
                    new( 1f, -1f),  // Top-right
                    new( 1f,  1f),  // Bottom-right
                };

                // Interleave position and color data
                var vertexData = new float[positions.Length * 6];  // 6 floats per vertex
                
                for (int i = 0; i < positions.Length; i++)
                {
                    int baseIndex = i * 6;
                    vertexData[baseIndex + 0] = positions[i].X;
                    vertexData[baseIndex + 1] = positions[i].Y;
                    vertexData[baseIndex + 2] = colors[i].X;
                    vertexData[baseIndex + 3] = colors[i].Y;
                    vertexData[baseIndex + 4] = colors[i].Z;
                    vertexData[baseIndex + 5] = colors[i].W;
                }

                var bytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(vertexData.AsSpan()).ToArray();

                return new GeometrySourceData
                {
                    VertexData = bytes,
                    VertexCount = (uint)positions.Length,
                    Stride = 24  // 8 bytes (pos) + 16 bytes (color)
                };
            })
        };
    }
}
