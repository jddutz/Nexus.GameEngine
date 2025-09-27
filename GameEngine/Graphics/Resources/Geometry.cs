using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// Static geometry resource definitions for commonly used shapes.
/// </summary>
public static class Geometry
{
    /// <summary>
    /// Full-screen quad for background rendering and post-processing effects.
    /// Uses NDC coordinates (-1 to 1) for direct screen mapping.
    /// </summary>
    public static readonly GeometryDefinition FullScreenQuad = new()
    {
        Name = "FullScreenQuad",
        IsPersistent = true,  // Core geometry used by many components - never purge
        Vertices = [
            // Position        // TexCoords (NDC coordinates -1 to 1)
            -1.0f, -1.0f, 0.0f,  0.0f, 0.0f, // Bottom-left
             1.0f, -1.0f, 0.0f,  1.0f, 0.0f, // Bottom-right
             1.0f,  1.0f, 0.0f,  1.0f, 1.0f, // Top-right
            -1.0f,  1.0f, 0.0f,  0.0f, 1.0f  // Top-left
        ],
        Indices = [0, 1, 2, 2, 3, 0],
        Attributes = [
            new VertexAttribute { Location = 0, Size = 3, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 0 },
            new VertexAttribute { Location = 1, Size = 2, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 3 * sizeof(float) }
        ]
    };

    /// <summary>
    /// Sprite quad for 2D sprite rendering.
    /// Uses object space coordinates (-0.5 to 0.5) for transformation flexibility.
    /// </summary>
    public static readonly GeometryDefinition SpriteQuad = new()
    {
        Name = "SpriteQuad",
        IsPersistent = true,  // Core geometry used by many components - never purge
        Vertices = [
            // Position        // TexCoords (Object space -0.5 to 0.5)
            -0.5f, -0.5f, 0.0f,  0.0f, 0.0f, // Bottom-left
             0.5f, -0.5f, 0.0f,  1.0f, 0.0f, // Bottom-right
             0.5f,  0.5f, 0.0f,  1.0f, 1.0f, // Top-right
            -0.5f,  0.5f, 0.0f,  0.0f, 1.0f  // Top-left
        ],
        Indices = [0, 1, 2, 2, 3, 0],
        Attributes = [
            new VertexAttribute { Location = 0, Size = 3, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 0 },
            new VertexAttribute { Location = 1, Size = 2, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 3 * sizeof(float) }
        ]
    };

    /// <summary>
    /// UI quad for user interface elements.
    /// Uses normalized coordinates (0 to 1) for UI layout systems.
    /// </summary>
    public static readonly GeometryDefinition UIQuad = new()
    {
        Name = "UIQuad",
        IsPersistent = true,  // Core UI geometry - never purge
        Vertices = [
            // Position     // TexCoords (UI space 0 to 1)
            0.0f, 0.0f, 0.0f,  0.0f, 0.0f, // Bottom-left
            1.0f, 0.0f, 0.0f,  1.0f, 0.0f, // Bottom-right
            1.0f, 1.0f, 0.0f,  1.0f, 1.0f, // Top-right
            0.0f, 1.0f, 0.0f,  0.0f, 1.0f  // Top-left
        ],
        Indices = [0, 1, 2, 2, 3, 0],
        Attributes = [
            new VertexAttribute { Location = 0, Size = 3, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 0 },
            new VertexAttribute { Location = 1, Size = 2, Type = VertexAttribPointerType.Float, Stride = 5 * sizeof(float), Offset = 3 * sizeof(float) }
        ]
    };
}