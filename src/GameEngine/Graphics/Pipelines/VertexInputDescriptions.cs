using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Provides static vertex input descriptions for common vertex formats.
/// </summary>
public static class VertexInputDescriptions
{
    /// <summary>
    /// Vertex input for position-only vertices (Vec2).
    /// Used by: BiaxialGradient, LinearGradient, RadialGradient shaders.
    /// Format: Position (vec2) = 8 bytes per vertex.
    /// </summary>
    public static VertexInputDescription Position2D { get; } = new()
    {
        Bindings =
        [
            new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = 8,  // 2 floats (x, y)
                InputRate = VertexInputRate.Vertex
            }
        ],
        Attributes =
        [
            new VertexInputAttributeDescription
            {
                Location = 0,
                Binding = 0,
                Format = Format.R32G32Sfloat,  // vec2
                Offset = 0
            }
        ]
    };

    /// <summary>
    /// Vertex input for position + color vertices (Vec2 + Vec4).
    /// Used by: ColoredGeometry shader.
    /// Format: Position (vec2) + Color (vec4) = 24 bytes per vertex.
    /// </summary>
    public static VertexInputDescription Position2DColor { get; } = new()
    {
        Bindings =
        [
            new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = 24,  // 2 floats (pos) + 4 floats (color)
                InputRate = VertexInputRate.Vertex
            }
        ],
        Attributes =
        [
            new VertexInputAttributeDescription
            {
                Location = 0,
                Binding = 0,
                Format = Format.R32G32Sfloat,  // vec2 position
                Offset = 0
            },
            new VertexInputAttributeDescription
            {
                Location = 1,
                Binding = 0,
                Format = Format.R32G32B32A32Sfloat,  // vec4 color
                Offset = 8
            }
        ]
    };

    /// <summary>
    /// Vertex input for position + texcoord vertices (Vec2 + Vec2).
    /// Used by: ImageTexture shader.
    /// Format: Position (vec2) + TexCoord (vec2) = 16 bytes per vertex.
    /// </summary>
    public static VertexInputDescription Position2DTexCoord { get; } = new()
    {
        Bindings =
        [
            new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = 16,  // 2 floats (pos) + 2 floats (uv)
                InputRate = VertexInputRate.Vertex
            }
        ],
        Attributes =
        [
            new VertexInputAttributeDescription
            {
                Location = 0,
                Binding = 0,
                Format = Format.R32G32Sfloat,  // vec2 position
                Offset = 0
            },
            new VertexInputAttributeDescription
            {
                Location = 1,
                Binding = 0,
                Format = Format.R32G32Sfloat,  // vec2 texcoord
                Offset = 8
            }
        ]
    };
}
