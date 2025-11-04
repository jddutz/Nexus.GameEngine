using Nexus.GameEngine.Resources.Geometry.Definitions;

namespace Nexus.GameEngine.GUI.BackgroundLayers;

/// <summary>
/// Full-screen background with a linear gradient.
/// Supports unlimited color stops and any rotation angle.
/// Uses UBO for gradient definition and push constants for angle animation.
/// Renders in Main pass at priority 0 (background layer).
/// </summary>
public partial class LinearGradientBackground(
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
    [TemplateProperty]
    protected GradientDefinition _gradient = GradientDefinition.TwoColor(
        new Vector4D<float>(0, 0, 0, 1),
        new Vector4D<float>(1, 1, 1, 1));

    /// <summary>
    /// Gradient rotation angle in radians. Can be animated.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected float _angle = 0f;

    public override PipelineHandle Pipeline =>
        PipelineManager.GetOrCreate(PipelineDefinitions.LinearGradient);

    protected override void OnActivate()
    {
        base.OnActivate();

        // Validate gradient
        _gradient.Validate();

        // Create position-only full-screen quad geometry
        _geometry = ResourceManager.Geometry.GetOrCreate(GeometryDefinitions.TexturedQuad);

        // Create UBO and descriptor set for gradient
        CreateGradientUBO(_gradient);
    }

    /// <summary>
    /// Creates a UBO buffer and descriptor set for the gradient definition.
    /// </summary>
    private void CreateGradientUBO(GradientDefinition gradient)
    {
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
        if (shader.DescriptorSetLayouts == null || !shader.DescriptorSetLayouts.ContainsKey(0))
        {
            throw new InvalidOperationException(
                $"Shader {shader.Name} does not define descriptor set layout for set 0");
        }
        
        var layout = descriptorManager.CreateDescriptorSetLayout(shader.DescriptorSetLayouts[0]);
        
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

        // Use push constants for the angle (allows animation)
        var pushConstants = new LinearGradientPushConstants { Angle = _angle };

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
            ResourceManager.Geometry.Release(GeometryDefinitions.TexturedQuad);
            _geometry = null;
        }
        
        base.OnDeactivate();
    }
}
