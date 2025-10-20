using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Animation;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Data;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Buffers;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Resources.Shaders.Definitions;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Full-screen background with a radial gradient.
/// Supports unlimited color stops with configurable center point and radius.
/// Uses UBO for gradient definition and push constants for center/radius animation.
/// Renders in Main pass at priority 0 (background layer).
/// </summary>
public partial class RadialGradientBackground(
    IPipelineManager pipelineManager,
    IResourceManager resources,
    IBufferManager bufferManager,
    IDescriptorManager descriptorManager)
    : RuntimeComponent, IDrawable
{
    /// <summary>
    /// Template for configuring RadialGradientBackground components.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// The gradient definition (color stops).
        /// Required. Use GradientDefinition.TwoColor() for simple gradients.
        /// </summary>
        public GradientDefinition? Gradient { get; set; }
        
        /// <summary>
        /// Center point of the radial gradient in normalized coordinates [0, 1].
        /// (0, 0) = top-left corner
        /// (0.5, 0.5) = screen center
        /// (1, 1) = bottom-right corner
        /// Default: (0.5, 0.5) - center of screen
        /// </summary>
        public Vector2D<float> Center { get; set; } = new(0.5f, 0.5f);
        
        /// <summary>
        /// Radius of the radial gradient in normalized [0,1] units.
        /// 0.5 = reaches from center to edges (for a centered gradient)
        /// 1.0 = reaches corners
        /// Default: 0.5
        /// </summary>
        public float Radius { get; set; } = 0.5f;
        
        /// <summary>
        /// Scale factors for elliptical gradients.
        /// (1, 1) = use automatic aspect ratio correction for circular gradient (default)
        /// (2, 1) = ellipse stretched 2x horizontally
        /// (1, 2) = ellipse stretched 2x vertically
        /// Set to (1, 1) for circular gradients on any viewport size.
        /// Default: (1, 1) - circular with aspect correction
        /// </summary>
        public Vector2D<float> Scale { get; set; } = new(1f, 1f);
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
    /// Center point of the radial gradient in normalized [0,1] coordinates. Can be animated.
    /// </summary>
    [ComponentProperty(Duration = AnimationDuration.Slow, Interpolation = InterpolationMode.Linear)]
    private Vector2D<float> _center = new(0.5f, 0.5f);

    /// <summary>
    /// Radius of the radial gradient. Can be animated.
    /// </summary>
    [ComponentProperty(Duration = AnimationDuration.Slow, Interpolation = InterpolationMode.Linear)]
    private float _radius = 0.5f;

    /// <summary>
    /// Scale factors for elliptical gradients. Can be animated.
    /// </summary>
    [ComponentProperty(Duration = AnimationDuration.Slow, Interpolation = InterpolationMode.Linear)]
    private Vector2D<float> _scale = new(1f, 1f);

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        
        if (componentTemplate is Template template)
        {
            _gradientDefinition = template.Gradient;
            _center = template.Center;
            _radius = template.Radius;
            _scale = template.Scale;
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        Logger?.LogInformation("RadialGradientBackground.OnActivate - Center: {Center}, Radius: {Radius}",
            _center, _radius);

        if (_gradientDefinition == null)
        {
            throw new InvalidOperationException("RadialGradientBackground requires a Gradient definition");
        }

        try
        {
            // Validate gradient
            _gradientDefinition.Validate();
            
            // Build pipeline for radial gradient rendering
            _pipeline = pipelineManager.GetBuilder()
                .WithShader(new RadialGradientShader())
                .WithRenderPasses(RenderPasses.Main)
                .WithTopology(PrimitiveTopology.TriangleStrip)
                .WithCullMode(CullModeFlags.None)  // No culling for full-screen quad
                .WithDepthTest()
                .WithDepthWrite()
                .Build("RadialGradientBackground_Pipeline");

            Logger?.LogInformation("RadialGradientBackground pipeline created successfully");

            // Create position-only full-screen quad geometry
            _geometry = resources.Geometry.GetOrCreate(new UniformColorQuad());

            Logger?.LogInformation("RadialGradientBackground geometry created. Name: {Name}, VertexCount: {VertexCount}",
                _geometry.Name, _geometry.VertexCount);

            // Create UBO and descriptor set for gradient
            CreateGradientUBO(_gradientDefinition);
            
            Logger?.LogInformation("Radial gradient UBO created with {StopCount} stops",
                _gradientDefinition.Stops.Length);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "RadialGradientBackground initialization failed");
            throw;
        }
    }

    /// <summary>
    /// Creates a UBO buffer and descriptor set for the gradient definition.
    /// </summary>
    private void CreateGradientUBO(GradientDefinition gradient)
    {
        Logger?.LogDebug("Creating gradient UBO with {StopCount} stops", gradient.Stops.Length);
        
        // Log gradient stops for debugging
        for (int i = 0; i < gradient.Stops.Length; i++)
        {
            var stop = gradient.Stops[i];
            Logger?.LogDebug("  Stop[{Index}]: Position={Pos}, Color=RGBA({R}, {G}, {B}, {A})",
                i, stop.Position, stop.Color.X, stop.Color.Y, stop.Color.Z, stop.Color.W);
        }
        
        // Convert gradient definition to UBO structure
        var ubo = GradientUBO.FromGradientDefinition(gradient);
        
        // Create uniform buffer
        var uboSize = GradientUBO.SizeInBytes;
        (_gradientUboBuffer, _gradientUboMemory) = bufferManager.CreateUniformBuffer(uboSize);
        
        Logger?.LogDebug("UBO buffer created: size={Size} bytes, handle={Handle}",
            uboSize, _gradientUboBuffer.Value.Handle);
        
        // Upload UBO data to buffer
        var uboBytes = ubo.AsBytes();
        bufferManager.UpdateUniformBuffer(_gradientUboMemory.Value, uboBytes);
        
        Logger?.LogDebug("UBO data uploaded to buffer");
        
        // Get or create descriptor set layout
        var shader = new RadialGradientShader();
        if (shader.DescriptorSetLayoutBindings == null || shader.DescriptorSetLayoutBindings.Length == 0)
        {
            throw new InvalidOperationException(
                $"Shader {shader.Name} does not define descriptor set layout bindings");
        }
        
        var layout = descriptorManager.CreateDescriptorSetLayout(shader.DescriptorSetLayoutBindings);
        
        // Allocate descriptor set
        _gradientDescriptorSet = descriptorManager.AllocateDescriptorSet(layout);
        
        Logger?.LogDebug("Descriptor set allocated: handle={Handle}, layout={Layout}",
            _gradientDescriptorSet.Value.Handle, layout.Handle);
        
        // Update descriptor set to point to our UBO buffer
        descriptorManager.UpdateDescriptorSet(
            _gradientDescriptorSet.Value,
            _gradientUboBuffer.Value,
            uboSize,
            0);  // binding = 0
        
        Logger?.LogDebug("Descriptor set updated: set={Set}, buffer={Buffer}, size={Size}, binding=0",
            _gradientDescriptorSet.Value.Handle, _gradientUboBuffer.Value.Handle, uboSize);
    }

    public IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_geometry == null || !_gradientDescriptorSet.HasValue)
            yield break;

        _drawCallCount++;
        
        // Log current state on first call and every 100 frames
        if (_drawCallCount == 1 || _drawCallCount % 100 == 0)
        {
            Logger?.LogInformation("DrawCall {Count}: Center={Center}, Radius={Radius}, Scale={Scale}, DescriptorSet={Set}",
                _drawCallCount, _center, _radius, _scale, _gradientDescriptorSet.Value.Handle);
        }

        // Calculate aspect-corrected scale for circular gradients
        var viewport = context.Viewport.VulkanViewport;
        float aspectRatio = viewport.Width / viewport.Height;
        
        // Apply aspect ratio correction to user-provided scale
        // If user wants circular (1,1), this becomes (aspectRatio, 1) for non-square viewports
        var effectiveScale = new Vector2D<float>(_scale.X * aspectRatio, _scale.Y);

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
            
            Logger?.LogDebug("Gradient UBO buffer destroyed");
        }
        
        // Descriptor sets are freed automatically when the pool is reset
        if (_gradientDescriptorSet.HasValue)
        {
            _gradientDescriptorSet = null;
            Logger?.LogDebug("Gradient descriptor set reference cleared");
        }
        
        if (_geometry != null)
        {
            resources.Geometry.Release(new UniformColorQuad());
            _geometry = null;
        }
        
        base.OnDeactivate();
    }
}
