using Nexus.GameEngine.Graphics.Pipelines;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Immutable record defining a shader program with vertex and fragment shaders.
/// Includes complete shader metadata for pipeline creation.
/// Records provide value-based equality, making them ideal for cache keys.
/// </summary>
public record ShaderDefinition : IResourceDefinition
{
    /// <summary>
    /// Unique name identifying this shader definition.
    /// Used as cache key in shader resource manager.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The source that knows how to load this shader's compiled SPIR-V code.
    /// </summary>
    public required IShaderSource Source { get; init; }
    
    /// <summary>
    /// Description of vertex input format this shader expects.
    /// Defines locations, formats, and offsets of vertex attributes.
    /// </summary>
    public required VertexInputDescription InputDescription { get; init; }
    
    /// <summary>
    /// Push constant ranges used by this shader.
    /// Null or empty if shader doesn't use push constants.
    /// </summary>
    public PushConstantRange[]? PushConstantRanges { get; init; }
    
    /// <summary>
    /// Descriptor set layout bindings used by this shader.
    /// Null or empty if shader doesn't use descriptor sets (UBOs, textures, etc.).
    /// </summary>
    /// <remarks>
    /// For shaders that use uniform buffers or other resources, this defines
    /// the descriptor set layout bindings. The descriptor manager will create
    /// descriptor set layouts from these bindings.
    /// </remarks>
    public DescriptorSetLayoutBinding[]? DescriptorSetLayoutBindings { get; init; }
}
