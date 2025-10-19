using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Animation;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Resources.Shaders.Definitions;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Full-screen background layer that renders backgrounds using various modes.
/// UniformColor mode uses UniformColorQuad with push constants (optimized).
/// PerVertexColor mode uses ColorQuad with vertex colors (legacy, to be replaced with UBO).
/// Gradient modes (LinearGradient, RadialGradient) require descriptor set support.
/// Renders in Main pass with priority 0 (first to render).
/// </summary>
public partial class BackgroundLayer(
    IPipelineManager pipelineManager,
    IResourceManager resources)
    : RenderableBase(), IRenderable
{

    /// <summary>
    /// Template for configuring BackgroundLayer components.
    /// Defines the background rendering mode and associated properties.
    /// </summary>
    public new record Template : RenderableBase.Template
    {
        public BackgroundLayerModeEnum Mode { get; set; } = BackgroundLayerModeEnum.UniformColor;

        public Vector4D<float> UniformColor { get; set; } = Colors.Black;

        public Vector4D<float>[] VertexColors { get; set; } = [
            Colors.Black,
            Colors.Black,
            Colors.Black,
            Colors.Black
        ];

        public string ImageTextureName { get; set; } = string.Empty;
    }

    // TODO: Which of these properties should be moved to RenderableBase?
    private GeometryResource? _geometry;
    private PipelineHandle _pipeline;
    private int _drawCallCount = 0;

    public BackgroundLayerModeEnum BackgroundLayerMode { get; private set; } = BackgroundLayerModeEnum.UniformColor;

    [ComponentProperty(Duration = AnimationDuration.Slow, Interpolation = InterpolationMode.Linear)]
    private Vector4D<float> _uniformColor = Colors.Black;

    [ComponentProperty(Duration = AnimationDuration.Slow, Interpolation = InterpolationMode.Linear)]
    private Vector4D<float>[] _vertexColors = [
        Colors.Black,
        Colors.Black,
        Colors.Black,
        Colors.Black
    ];

    /// <summary>
    /// Partial method called when VertexColors property changes
    /// </summary>
    partial void OnVertexColorsChanged(Vector4D<float>[]? oldValue)
    {
        Logger?.LogDebug("OnVertexColorsChanged called - Old: [{O0}, {O1}, {O2}, {O3}], New: [{N0}, {N1}, {N2}, {N3}]",
            oldValue?[0], oldValue?[1], oldValue?[2], oldValue?[3],
            _vertexColors[0], _vertexColors[1], _vertexColors[2], _vertexColors[3]);

        // If we're active and have geometry and in PerVertexColor mode, recreate it with new colors
        if (IsActive && _geometry != null && BackgroundLayerMode == BackgroundLayerModeEnum.PerVertexColor)
        {
            Logger?.LogDebug("Recreating geometry with new vertex colors");
            
            // Release old geometry
            resources.Geometry.Release(new ColorQuad(oldValue ?? _vertexColors));
            
            // Create new geometry with updated colors
            _geometry = resources.Geometry.GetOrCreate(new ColorQuad(_vertexColors));
            
            Logger?.LogDebug("Geometry recreated successfully");
        }
    }

    /// <summary>
    /// Partial method called when UniformColor property changes
    /// </summary>
    partial void OnUniformColorChanged(Vector4D<float> oldValue)
    {
        Logger?.LogDebug("OnUniformColorChanged called - Old: {Old}, New: {New}", oldValue, _uniformColor);

        // UniformColor mode doesn't need to recreate geometry - color comes from push constants
        // Geometry is just positions, so no need to update it when color changes
    }

    /// <summary>
    /// Render at the very beginning of the Main pass (background)
    /// </summary>
    protected override uint GetDefaultRenderMask() => RenderPasses.Main;

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            BackgroundLayerMode = template.Mode;
            UniformColor = template.UniformColor;
            VertexColors = template.VertexColors;
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        Logger?.LogInformation("BackgroundLayer.OnActivate called - Mode: {Mode}", BackgroundLayerMode);

        // Subscribe to animation events for debugging
        AnimationStarted += (sender, e) =>
        {
            Logger?.LogDebug("Animation STARTED for property: {PropertyName}", e.PropertyName);
        };
        
        AnimationEnded += (sender, e) =>
        {
            Logger?.LogDebug("Animation ENDED for property: {PropertyName}", e.PropertyName);
        };

        try
        {
            // Build pipeline based on mode
            _pipeline = BackgroundLayerMode switch
            {
                BackgroundLayerModeEnum.UniformColor => 
                    pipelineManager.GetBuilder()
                        .WithShader(new UniformColorQuadShader())
                        .WithRenderPasses(RenderPasses.Main)
                        .WithTopology(PrimitiveTopology.TriangleStrip)
                        .WithCullMode(CullModeFlags.None)  // No culling for full-screen quad
                        .WithDepthTest()
                        .WithDepthWrite()
                        .Build("BackgroundLayer_UniformColorPipeline"),
                
                BackgroundLayerModeEnum.PerVertexColor => 
                    pipelineManager.GetBuilder()
                        .WithShader(new ColoredGeometryShader())
                        .WithRenderPasses(RenderPasses.Main)
                        .WithTopology(PrimitiveTopology.TriangleStrip)
                        .WithCullMode(CullModeFlags.None)  // No culling for full-screen quad
                        .WithDepthTest()
                        .WithDepthWrite()
                        .Build("BackgroundLayer_PerVertexColorPipeline"),
                
                BackgroundLayerModeEnum.LinearGradient or BackgroundLayerModeEnum.RadialGradient =>
                    throw new NotSupportedException(
                        $"Mode {BackgroundLayerMode} requires descriptor set support (not yet implemented). " +
                        "See .temp/agent/ubo-and-buffer-management-explanation.md for details."),
                
                _ => throw new NotSupportedException($"Unknown BackgroundLayerMode: {BackgroundLayerMode}")
            };

            Logger?.LogInformation("BackgroundLayer pipeline created successfully");

            // Create full-screen quad geometry based on mode
            if (BackgroundLayerMode == BackgroundLayerModeEnum.UniformColor)
            {
                // UniformColor: position-only geometry, color from push constants
                _geometry = resources.Geometry.GetOrCreate(new UniformColorQuad());
            }
            else // PerVertexColor
            {
                // PerVertexColor: geometry with vertex colors (legacy approach)
                _geometry = resources.Geometry.GetOrCreate(new ColorQuad(_vertexColors));
            }

            Logger?.LogInformation("BackgroundLayer geometry created. Name: {Name}, VertexCount: {VertexCount}, Mode: {Mode}",
                _geometry.Name, _geometry.VertexCount, BackgroundLayerMode);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "BackgroundLayer initialization failed");
            throw;
        }
    }

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_geometry == null)
            yield break;

        _drawCallCount++;
        
        // Log current colors on first call and every 100 frames
        if (_drawCallCount == 1 || _drawCallCount % 100 == 0)
        {
            if (BackgroundLayerMode == BackgroundLayerModeEnum.UniformColor)
            {
                Logger?.LogInformation("DrawCall {Count}: Mode={Mode}, UniformColor={Color}",
                    _drawCallCount, BackgroundLayerMode, _uniformColor);
            }
            else
            {
                Logger?.LogInformation("DrawCall {Count}: Mode={Mode}, VertexColors=[{C0}, {C1}, {C2}, {C3}]",
                    _drawCallCount, BackgroundLayerMode,
                    _vertexColors[0], _vertexColors[1], _vertexColors[2], _vertexColors[3]);
            }
        }

        // Push constants based on mode
        object? pushConstants = BackgroundLayerMode switch
        {
            BackgroundLayerModeEnum.UniformColor => UniformColorPushConstants.FromColor(_uniformColor),
            BackgroundLayerModeEnum.PerVertexColor => null,  // Colors are in vertex data for legacy mode
            _ => null
        };

        yield return new DrawCommand
        {
            RenderMask = RenderMask,
            Pipeline = _pipeline,
            VertexBuffer = _geometry.Buffer,
            VertexCount = _geometry.VertexCount,
            InstanceCount = 1,
            PushConstants = pushConstants
        };
    }

    protected override void OnDeactivate()
    {
        if (_geometry != null)
        {
            // Release geometry based on mode
            if (BackgroundLayerMode == BackgroundLayerModeEnum.UniformColor)
            {
                resources.Geometry.Release(new UniformColorQuad());
            }
            else // PerVertexColor
            {
                resources.Geometry.Release(new ColorQuad(_vertexColors));
            }
            
            _geometry = null;
        }
        base.OnDeactivate();
    }
}