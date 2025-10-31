namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Complete description of a graphics pipeline's configuration.
/// Used as a key for pipeline caching and as input for pipeline creation.
/// </summary>
/// <remarks>
/// <para><strong>Design Notes:</strong></para>
/// <list type="bullet">
/// <item>Immutable record type - safe to use as dictionary key</item>
/// <item>Implements value equality for proper caching</item>
/// <item>Contains all state needed to create a Vulkan graphics pipeline</item>
/// <item>Hash code computed from all properties for fast lookups</item>
/// </list>
/// 
/// <para><strong>Usage:</strong></para>
/// <code>
/// var descriptor = new PipelineDescriptor
/// {
///     Name = "MyPipeline",
///     VertexShaderPath = "shaders/vert.spv",
///     FragmentShaderPath = "shaders/frag.spv",
///     VertexInputDescription = MyVertex.GetDescription(),
///     Topology = PrimitiveTopology.TriangleList,
///     RenderPass = renderPass,
///     Subpass = 0,
///     EnableDepthTest = true,
///     EnableBlending = false
/// };
/// </code>
/// </remarks>
public record PipelineDescriptor
{
    /// <summary>
    /// Unique name for this pipeline (used for debugging and caching).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Shader resource containing compiled shader modules.
    /// If provided, takes precedence over shader paths.
    /// </summary>
    public ShaderResource? ShaderResource { get; init; }

    /// <summary>
    /// Path to compiled SPIR-V vertex shader (.spv file).
    /// Legacy - only used if ShaderResource is not provided.
    /// </summary>
    public string? VertexShaderPath { get; init; }

    /// <summary>
    /// Path to compiled SPIR-V fragment shader (.spv file).
    /// Legacy - only used if ShaderResource is not provided.
    /// </summary>
    public string? FragmentShaderPath { get; init; }

    /// <summary>
    /// Optional path to compiled SPIR-V geometry shader (.spv file).
    /// Legacy - only used if ShaderResource is not provided.
    /// </summary>
    public string? GeometryShaderPath { get; init; }

    /// <summary>
    /// Vertex input description (bindings and attributes).
    /// Describes the layout of vertex data passed to the vertex shader.
    /// </summary>
    public required VertexInputDescription VertexInputDescription { get; init; }

    /// <summary>
    /// Primitive topology (point list, line list, triangle list, etc.).
    /// </summary>
    public PrimitiveTopology Topology { get; init; } = PrimitiveTopology.TriangleList;

    /// <summary>
    /// Target render pass this pipeline will be used with.
    /// Pipeline must be compatible with this render pass.
    /// </summary>
    public required RenderPass RenderPass { get; init; }

    /// <summary>
    /// Subpass index within the render pass.
    /// </summary>
    public uint Subpass { get; init; } = 0;

    /// <summary>
    /// Enable depth testing (write and compare against depth buffer).
    /// </summary>
    public bool EnableDepthTest { get; init; } = true;

    /// <summary>
    /// Enable depth writes (update depth buffer).
    /// </summary>
    public bool EnableDepthWrite { get; init; } = true;

    /// <summary>
    /// Depth comparison operation (Less, LessOrEqual, Greater, etc.).
    /// </summary>
    public CompareOp DepthCompareOp { get; init; } = CompareOp.Less;

    /// <summary>
    /// Enable alpha blending for color attachments.
    /// </summary>
    public bool EnableBlending { get; init; } = false;

    /// <summary>
    /// Source blend factor (for color blending).
    /// </summary>
    public BlendFactor SrcBlendFactor { get; init; } = BlendFactor.SrcAlpha;

    /// <summary>
    /// Destination blend factor (for color blending).
    /// </summary>
    public BlendFactor DstBlendFactor { get; init; } = BlendFactor.OneMinusSrcAlpha;

    /// <summary>
    /// Blend operation (Add, Subtract, etc.).
    /// </summary>
    public BlendOp BlendOp { get; init; } = BlendOp.Add;

    /// <summary>
    /// Polygon rasterization mode (Fill, Line, Point).
    /// </summary>
    public PolygonMode PolygonMode { get; init; } = PolygonMode.Fill;

    /// <summary>
    /// Face culling mode (None, Front, Back, FrontAndBack).
    /// </summary>
    public CullModeFlags CullMode { get; init; } = CullModeFlags.BackBit;

    /// <summary>
    /// Front face winding order (Clockwise, CounterClockwise).
    /// </summary>
    public FrontFace FrontFace { get; init; } = FrontFace.Clockwise;

    /// <summary>
    /// Line width (for line rendering).
    /// Must be 1.0 unless wideLines feature is enabled.
    /// </summary>
    public float LineWidth { get; init; } = 1.0f;

    /// <summary>
    /// Enable dynamic viewport (can be set per command buffer).
    /// </summary>
    public bool DynamicViewport { get; init; } = false;

    /// <summary>
    /// Enable dynamic scissor (can be set per command buffer).
    /// </summary>
    public bool DynamicScissor { get; init; } = false;

    /// <summary>
    /// Push constant ranges for shader uniforms.
    /// </summary>
    public PushConstantRange[]? PushConstantRanges { get; init; }

    /// <summary>
    /// Descriptor set layouts for shader resources (textures, buffers, etc.).
    /// </summary>
    public DescriptorSetLayout[]? DescriptorSetLayouts { get; init; }
}
