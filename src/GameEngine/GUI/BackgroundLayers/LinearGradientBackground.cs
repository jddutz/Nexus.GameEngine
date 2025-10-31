namespace Nexus.GameEngine.GUI.BackgroundLayers;

/// <summary>
/// Full-screen background with a linear gradient.
/// Supports unlimited color stops and any rotation angle.
/// Uses UBO for gradient definition and push constants for angle animation.
/// Renders in Main pass at priority 0 (background layer).
/// </summary>
public partial class LinearGradientBackground(
    IPipelineManager pipelineManager,
    IResourceManager resources,
    IBufferManager bufferManager,
    IDescriptorManager descriptorManager)
    : Drawable, IDrawable
{
    /// <summary>
    /// Template for configuring LinearGradientBackground components.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// The gradient definition (color stops).
        /// Required. Use GradientDefinition.TwoColor() for simple gradients.
        /// </summary>
        public GradientDefinition? Gradient { get; set; }
        
        /// <summary>
        /// Rotation angle in radians.
        /// 0 = horizontal (left to right)
        /// PI/2 = vertical (top to bottom)
        /// PI = horizontal (right to left)
        /// Default: 0 (horizontal)
        /// </summary>
        public float Angle { get; set; } = 0f;
    }

    private GeometryResource? _geometry;
    private PipelineHandle _pipeline;
    private int _drawCallCount = 0;
    
    // UBO and descriptor set for gradient definition
    private Silk.NET.Vulkan.Buffer? _gradientUboBuffer;
    private DeviceMemory? _gradientUboMemory;
    private DescriptorSet? _gradientDescriptorSet;
    
    private GradientDefinition? _gradientDefinition;

    /// <summary>
    /// Gradient rotation angle in radians. Can be animated.
    /// </summary>
    [ComponentProperty]
    private float _angle = 0f;

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        base.OnLoad(componentTemplate);
        
        if (componentTemplate is Template template)
        {
            _gradientDefinition = template.Gradient;
            SetAngle(template.Angle);
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        if (_gradientDefinition == null)
        {
            throw new InvalidOperationException("LinearGradientBackground requires a Gradient definition");
        }

        // Validate gradient
        _gradientDefinition.Validate();
        
        // Build pipeline for linear gradient rendering
        _pipeline = pipelineManager.GetBuilder()
            .WithShader(ShaderDefinitions.LinearGradient)
            .WithRenderPasses(RenderPasses.Main)
            .WithTopology(PrimitiveTopology.TriangleStrip)
            .WithCullMode(CullModeFlags.None)  // No culling for full-screen quad
            .WithDepthTest()
            .WithDepthWrite()
            .Build("LinearGradientBackground_Pipeline");


        // Create position-only full-screen quad geometry
        _geometry = resources.Geometry.GetOrCreate(GeometryDefinitions.UniformColorQuad);

        // Create UBO and descriptor set for gradient
        CreateGradientUBO(_gradientDefinition);
    }

    /// <summary>
    /// Creates a UBO buffer and descriptor set for the gradient definition.
    /// </summary>
    private void CreateGradientUBO(GradientDefinition gradient)
    {
        
        // Log gradient stops for debugging
        for (int i = 0; i < gradient.Stops.Length; i++)
        {
            var stop = gradient.Stops[i];
        }
        
        // Convert gradient definition to UBO structure
        var ubo = GradientUBO.FromGradientDefinition(gradient);
        
        // Create uniform buffer
        var uboSize = GradientUBO.SizeInBytes;
        (_gradientUboBuffer, _gradientUboMemory) = bufferManager.CreateUniformBuffer(uboSize);
        
        // Upload UBO data to buffer
        var uboBytes = ubo.AsBytes();
        bufferManager.UpdateUniformBuffer(_gradientUboMemory.Value, uboBytes);
        
        
        // Get or create descriptor set layout
        var shader = ShaderDefinitions.LinearGradient;
        if (shader.DescriptorSetLayoutBindings == null || shader.DescriptorSetLayoutBindings.Length == 0)
        {
            throw new InvalidOperationException(
                $"Shader {shader.Name} does not define descriptor set layout bindings");
        }
        
        var layout = descriptorManager.CreateDescriptorSetLayout(shader.DescriptorSetLayoutBindings);
        
        // Allocate descriptor set
        _gradientDescriptorSet = descriptorManager.AllocateDescriptorSet(layout);
        
        // Update descriptor set to point to our UBO buffer
        descriptorManager.UpdateDescriptorSet(
            _gradientDescriptorSet.Value,
            _gradientUboBuffer.Value,
            uboSize,
            0);  // binding = 0
    }

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_geometry == null || !_gradientDescriptorSet.HasValue)
            yield break;

        _drawCallCount++;

        // Use push constants for the angle (allows animation)
        var pushConstants = new LinearGradientPushConstants { Angle = _angle };

        yield return new DrawCommand
        {
            RenderMask = RenderPasses.Main,
            Pipeline = _pipeline,
            VertexBuffer = _geometry.Buffer,
            VertexCount = _geometry.VertexCount,
            InstanceCount = 1,
            PushConstants = pushConstants,
            DescriptorSet = _gradientDescriptorSet.Value
        };
    }

    protected override void OnDeactivate()
    {
        // Clean up UBO resources
        if (_gradientUboBuffer.HasValue && _gradientUboMemory.HasValue)
        {
            bufferManager.DestroyBuffer(_gradientUboBuffer.Value, _gradientUboMemory.Value);
            _gradientUboBuffer = null;
            _gradientUboMemory = null;
            
        }
        
        // Descriptor sets are freed automatically when the pool is reset
        if (_gradientDescriptorSet.HasValue)
        {
            _gradientDescriptorSet = null;
        }
        
        if (_geometry != null)
        {
            resources.Geometry.Release(GeometryDefinitions.UniformColorQuad);
            _geometry = null;
        }
        
        base.OnDeactivate();
    }
}
