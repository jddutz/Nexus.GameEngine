using Microsoft.Extensions.Logging;
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
/// Full-screen background layer that renders a solid color quad.
/// Renders in Main pass with priority 0 (first to render).
/// Animates colors over 1200 frames using push constants.
/// </summary>
public partial class BackgroundLayer(
    IPipelineManager pipelineManager,
    IResourceManager resources)
    : RenderableBase(), IRenderable
{
    private GeometryResource? _geometry;
    private PipelineHandle _pipeline;

    // Color animation state
    private const int AnimationFrames = 1200;
    private int _frameCount = 0;
    
    // Start colors (Frame 0) - Red on left side
    private readonly Vector4D<float> _startTopLeft = Resources.Colors.Red;
    private readonly Vector4D<float> _startBottomLeft = Resources.Colors.Red;
    private readonly Vector4D<float> _startTopRight = Resources.Colors.Green;
    private readonly Vector4D<float> _startBottomRight = Resources.Colors.Green;
    
    // End colors (Frame 1200) - Green on left side (swapped)
    private readonly Vector4D<float> _endTopLeft = Resources.Colors.Green;
    private readonly Vector4D<float> _endBottomLeft = Resources.Colors.Green;
    private readonly Vector4D<float> _endTopRight = Resources.Colors.Red;
    private readonly Vector4D<float> _endBottomRight = Resources.Colors.Red;
    
    // Current interpolated colors
    private Vector4D<float> _currentTopLeft;
    private Vector4D<float> _currentBottomLeft;
    private Vector4D<float> _currentTopRight;
    private Vector4D<float> _currentBottomRight;

    /// <summary>
    /// Render at the very beginning of the Main pass (background)
    /// </summary>
    protected override uint GetDefaultRenderMask() => RenderPasses.Main;

    protected override void OnActivate()
    {
        base.OnActivate();
        Logger?.LogInformation("BackgroundLayer.OnActivate called - creating pipeline and geometry");

        try
        {
            // Initialize colors to start state
            _currentTopLeft = _startTopLeft;
            _currentBottomLeft = _startBottomLeft;
            _currentTopRight = _startTopRight;
            _currentBottomRight = _startBottomRight;
            _frameCount = 0;

            // Build pipeline using fluent API - renders full-screen colored quad
            _pipeline = pipelineManager.GetBuilder()
                .WithShader(new ColoredGeometryShader())
                .WithRenderPasses(RenderPasses.Main)
                .WithTopology(PrimitiveTopology.TriangleStrip)
                .WithCullMode(CullModeFlags.None)  // No culling for full-screen quad
                .WithDepthTest()
                .WithDepthWrite()
                .Build("BackgroundLayerPipeline");

            Logger?.LogInformation("BackgroundLayer pipeline created successfully");

            // Use ColorQuad but it will fill the screen in normalized device coordinates
            _geometry = resources.Geometry.GetOrCreate(new ColorQuad());

            Logger?.LogInformation("BackgroundLayer geometry resource created. Name: {Name}, VertexCount: {VertexCount}",
                _geometry.Name, _geometry.VertexCount);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "BackgroundLayer initialization failed");
            throw;
        }
    }

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        // Animate colors over 40 frames
        if (_frameCount < AnimationFrames)
        {
            _frameCount++;
            float t = _frameCount / (float)AnimationFrames; // 0.0 to 1.0
            
            // Lerp each corner color
            _currentTopLeft = Vector4D.Lerp(_startTopLeft, _endTopLeft, t);
            _currentBottomLeft = Vector4D.Lerp(_startBottomLeft, _endBottomLeft, t);
            _currentTopRight = Vector4D.Lerp(_startTopRight, _endTopRight, t);
            _currentBottomRight = Vector4D.Lerp(_startBottomRight, _endBottomRight, t);
        }
    }

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_geometry == null)
            yield break;

        // Define colors for each vertex via push constants (interpolated)
        var pushConstants = VertexColorsPushConstants.FromColors(
            _currentTopLeft,       // Top-left (Red → Yellow)
            _currentBottomLeft,    // Bottom-left (Green → Cyan)
            _currentTopRight,      // Top-right (Black → Magenta)
            _currentBottomRight    // Bottom-right (Blue → White)
        );

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
            resources.Geometry.Release(new ColorQuad());
            _geometry = null;
        }
        base.OnDeactivate();
    }
}