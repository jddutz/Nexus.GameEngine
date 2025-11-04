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
    /// Descriptor set layout bindings grouped by descriptor set index.
    /// Key = descriptor set index (0, 1, 2, etc.), Value = bindings for that set.
    /// Use this for shaders that bind resources to multiple descriptor sets.
    /// </summary>
    /// <remarks>
    /// Example: ViewProjection UBO at set=0, Textures at set=1:
    /// DescriptorSetLayouts = new Dictionary&lt;uint, DescriptorSetLayoutBinding[]&gt;
    /// {
    ///     [0] = [new DescriptorSetLayoutBinding { Binding = 0, DescriptorType = UniformBuffer, ... }],
    ///     [1] = [new DescriptorSetLayoutBinding { Binding = 0, DescriptorType = CombinedImageSampler, ... }]
    /// }
    /// </remarks>
    public IReadOnlyDictionary<uint, DescriptorSetLayoutBinding[]>? DescriptorSetLayouts { get; init; }
}
