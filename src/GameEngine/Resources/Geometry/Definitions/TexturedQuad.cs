namespace Nexus.GameEngine.Resources.Geometry.Definitions;

/// <summary>
/// Provides static geometry definitions for common primitives.
/// These definitions are used to create and cache GPU geometry resources.
/// </summary>
public static partial class GeometryDefinitions
{
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
}
