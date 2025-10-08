using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Describes a single render element - what to draw and how.
/// This is the Vulkan-compatible replacement for the old OpenGL ElementData.
/// </summary>
public class ElementData
{
    /// <summary>
    /// Vertex array object ID (will map to Vulkan buffer later)
    /// </summary>
    public uint VertexBufferId { get; init; }

    /// <summary>
    /// Index buffer ID (if using indexed rendering)
    /// </summary>
    public uint? IndexBufferId { get; init; }

    /// <summary>
    /// Shader/Pipeline ID to use for rendering
    /// </summary>
    public uint PipelineId { get; init; }

    /// <summary>
    /// Number of vertices or indices to draw
    /// </summary>
    public uint DrawCount { get; init; }

    /// <summary>
    /// Model transformation matrix
    /// </summary>
    public Matrix4X4<float> ModelMatrix { get; init; } = Matrix4X4<float>.Identity;

    /// <summary>
    /// Optional texture IDs for this element
    /// </summary>
    public uint[]? TextureIds { get; init; }

    /// <summary>
    /// Custom uniform data for this element
    /// </summary>
    public Dictionary<string, object>? Uniforms { get; init; }

    /// <summary>
    /// Render pass flags (bit field for which passes this element participates in)
    /// </summary>
    public uint RenderPassFlags { get; init; } = 1; // Default to main pass

    /// <summary>
    /// Bounding box for frustum culling
    /// </summary>
    public Box3D<float> BoundingBox { get; init; }
}
