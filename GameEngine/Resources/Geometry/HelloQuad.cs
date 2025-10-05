namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Predefined geometry resources
/// </summary>
public static partial class GeometryDefinitions
{
    /// <summary>
    /// Basic quad geometry matching the Silk.NET HelloQuad example
    /// </summary>
    public static readonly GeometryDefinition HelloQuad = new()
    {
        Name = "HelloQuad",

        // Vertex data from the HelloQuad example
        Vertices = [
            //X     Y     Z
            0.5f,  0.5f,  0.0f,
            0.5f,  -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.5f
        ],

        // Index data
        Indices = [0, 1, 3, 1, 2, 3],

        // Vertex attributes - position only
        Attributes =
        [
            // Position attribute (location 0): 3 floats starting at offset 0
            VertexAttribute.Position3D(location: 0)
        ],

        UsageHint = BufferUsageHint.StaticDraw
    };
}

