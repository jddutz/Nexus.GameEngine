using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Describes a single Vulkan draw command - what to draw and how.
/// Contains all information needed for batching, state management, and rendering.
/// </summary>
public readonly struct DrawCommand
{
    // === RENDER PASS FILTERING ===
    
    /// <summary>
    /// Bit mask indicating which render passes this command participates in.
    /// Each bit corresponds to a render pass index (bit 0 = pass 0, bit 1 = pass 1, etc.).
    /// Used by renderer to filter commands per pass.
    /// </summary>
    public required uint RenderMask { get; init; }
    
    // === BATCHING KEYS (sorted by cost to change) ===
    
    /// <summary>
    /// Vulkan pipeline handle. Different pipelines = different shaders, blend modes, etc.
    /// Switching pipelines is expensive - batch strategy groups by this first.
    /// </summary>
    public required ulong PipelineHandle { get; init; }
    
    /// <summary>
    /// Descriptor set handle (textures, uniforms, samplers).
    /// Switching descriptor sets requires vkCmdBindDescriptorSets.
    /// </summary>
    public required ulong DescriptorSetHandle { get; init; }
    
    /// <summary>
    /// Vertex buffer handle.
    /// Switching vertex buffers requires vkCmdBindVertexBuffers.
    /// </summary>
    public required ulong VertexBufferHandle { get; init; }
    
    /// <summary>
    /// Index buffer handle. Set to 0 for non-indexed rendering.
    /// Switching index buffers requires vkCmdBindIndexBuffer.
    /// </summary>
    public ulong IndexBufferHandle { get; init; }
    
    // === DRAW PARAMETERS ===
    
    /// <summary>Number of vertices to draw (for non-indexed) or indices to draw (for indexed).</summary>
    public uint VertexCount { get; init; }
    
    /// <summary>Number of indices to draw (indexed rendering only).</summary>
    public uint IndexCount { get; init; }
    
    /// <summary>Number of instances to draw (instanced rendering). Default: 1.</summary>
    public uint InstanceCount { get; init; }
    
    // === PER-DRAW DATA (push constants) ===
    
    /// <summary>
    /// Model transformation matrix.
    /// Typically pushed via vkCmdPushConstants for per-draw uniqueness.
    /// </summary>
    public Matrix4X4<float> ModelMatrix { get; init; }
    
    /// <summary>
    /// Optional tint/color modulation for sprites and UI.
    /// Typically pushed via vkCmdPushConstants.
    /// </summary>
    public Vector4D<float> TintColor { get; init; }
}
