namespace Nexus.GameEngine.GUI.BackgroundLayers;

/// <summary>
/// Full-screen background with a radial gradient.
/// Supports unlimited color stops with configurable center point and radius.
/// Uses UBO for gradient definition and push constants for center/radius animation.
/// Renders in Main pass at priority 0 (background layer).
/// </summary>
public partial class RadialGradientBackground(
    IBufferManager bufferManager,
    IDescriptorManager descriptorManager)
    : Drawable, IDrawable
{
    private GeometryResource? _geometry;
    
    // UBO and descriptor set for gradient definition
    private Silk.NET.Vulkan.Buffer? _gradientUboBuffer;
    private DeviceMemory? _gradientUboMemory;
    private DescriptorSet? _gradientDescriptorSet;
    
    [ComponentProperty]
    protected GradientDefinition _gradient = GradientDefinition.TwoColor(
        new Vector4D<float>(0, 0, 0, 1),
        new Vector4D<float>(1, 1, 1, 1));

    /// <summary>
    /// Center point of the radial gradient in normalized [0,1] coordinates. Can be animated.
    /// </summary>
    [ComponentProperty]
    protected Vector2D<float> _center = new(0.5f, 0.5f);

    /// <summary>
    /// Radius of the radial gradient. Can be animated.
    /// </summary>
    [ComponentProperty]
    protected float _radius = 0.5f;

    /// <summary>
    /// Scale factors for elliptical gradients. Can be animated.
    /// </summary>
    [ComponentProperty]
    protected Vector2D<float> _gradientScale = new(1f, 1f);

    public override PipelineHandle Pipeline =>
        PipelineManager.GetOrCreate(PipelineDefinitions.RadialGradient);

    protected override void OnActivate()
    {
        base.OnActivate();

        // Validate gradient
        _gradient.Validate();

        // Create position-only full-screen quad geometry
        _geometry = ResourceManager.Geometry.GetOrCreate(GeometryDefinitions.UniformColorQuad);

        // Create UBO and descriptor set for gradient
        CreateGradientUBO(_gradient);
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
        var shader = ShaderDefinitions.RadialGradient;
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

        // Calculate aspect-corrected scale for circular gradients
        var viewport = context.Viewport.VulkanViewport;
        float aspectRatio = viewport.Width / viewport.Height;
        
        // Apply aspect ratio correction to user-provided scale
        // If user wants circular (1,1), this becomes (aspectRatio, 1) for non-square viewports
        var effectiveScale = new Vector2D<float>(_gradientScale.X * aspectRatio, _gradientScale.Y);

        // Use push constants for center, radius, and scale (allows animation)
        var pushConstants = new RadialGradientPushConstants 
        { 
            Center = _center,
            Radius = _radius,
            Scale = effectiveScale
        };

        yield return new DrawCommand
        {
            RenderMask = RenderPasses.Main,
            Pipeline = Pipeline,
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
            ResourceManager.Geometry.Release(GeometryDefinitions.UniformColorQuad);
            _geometry = null;
        }
        
        base.OnDeactivate();
    }
}
