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
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.GUI.Components;

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
    : RenderableBase(), IRenderable
{
    /// <summary>
    /// Template for configuring LinearGradientBackground components.
    /// </summary>
    public new record Template : RenderableBase.Template
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
    [ComponentProperty(Duration = AnimationDuration.Slow, Interpolation = InterpolationMode.Linear)]
    private float _angle = 0f;

    /// <summary>
    /// Render in the Main pass (background priority).
    /// </summary>
    protected override uint GetDefaultRenderMask() => RenderPasses.Main;

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        
        if (componentTemplate is Template template)
        {
            _gradientDefinition = template.Gradient;
            _angle = template.Angle;
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        Logger?.LogInformation("LinearGradientBackground.OnActivate - Angle: {Angle}", _angle);

        if (_gradientDefinition == null)
        {
            throw new InvalidOperationException("LinearGradientBackground requires a Gradient definition");
        }

        try
        {
            // Validate gradient
            _gradientDefinition.Validate();
            
            // Build pipeline for linear gradient rendering
            _pipeline = pipelineManager.GetBuilder()
                .WithShader(new LinearGradientShader())
                .WithRenderPasses(RenderPasses.Main)
                .WithTopology(PrimitiveTopology.TriangleStrip)
                .WithCullMode(CullModeFlags.None)  // No culling for full-screen quad
                .WithDepthTest()
                .WithDepthWrite()
                .Build("LinearGradientBackground_Pipeline");

            Logger?.LogInformation("LinearGradientBackground pipeline created successfully");

            // Create position-only full-screen quad geometry
            _geometry = resources.Geometry.GetOrCreate(new UniformColorQuad());

            Logger?.LogInformation("LinearGradientBackground geometry created. Name: {Name}, VertexCount: {VertexCount}",
                _geometry.Name, _geometry.VertexCount);

            // Create UBO and descriptor set for gradient
            CreateGradientUBO(_gradientDefinition);
            
            Logger?.LogInformation("Linear gradient UBO created with {StopCount} stops",
                _gradientDefinition.Stops.Length);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "LinearGradientBackground initialization failed");
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
        var shader = new LinearGradientShader();
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

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_geometry == null || !_gradientDescriptorSet.HasValue)
            yield break;

        _drawCallCount++;
        
        // Log current state on first call and every 100 frames
        if (_drawCallCount == 1 || _drawCallCount % 100 == 0)
        {
            Logger?.LogInformation("DrawCall {Count}: Angle={Angle}, DescriptorSet={Set}",
                _drawCallCount, _angle, _gradientDescriptorSet.Value.Handle);
        }

        // Use push constants for the angle (allows animation)
        var pushConstants = new LinearGradientPushConstants { Angle = _angle };

        yield return new DrawCommand
        {
            RenderMask = RenderMask,
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
