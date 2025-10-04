namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Predefined geometry resources
/// </summary>
public static partial class Geometry
{
    /// <summary>
    /// Full-screen quad for background rendering and post-processing effects
    /// Matches the Silk.NET sample format exactly
    /// </summary>
    public static readonly GeometryDefinition FullScreenQuad = new()
    {
        Name = "FullScreenQuad",

        // Vertex data: Position (x,y,z) - fullscreen quad in NDC coordinates
        // Format: [x, y, z] per vertex
        Vertices = new float[]
        {
            //X     Y     Z
            -1.0f, -1.0f, 0.0f,  // 0: bottom-left
             1.0f, -1.0f, 0.0f,  // 1: bottom-right
             1.0f,  1.0f, 0.0f,  // 2: top-right
            -1.0f,  1.0f, 0.0f   // 3: top-left
        },

        // Triangle indices (two triangles forming a quad) - matching Silk.NET sample
        Indices = new uint[] { 0, 1, 3, 1, 2, 3 },

        // Vertex attributes - matching Silk.NET sample
        Attributes = new[]
        {
            // Position attribute (location 0): 3 floats starting at offset 0
            VertexAttribute.Position3D(location: 0)
        },

        UsageHint = BufferUsageHint.StaticDraw
    };
}