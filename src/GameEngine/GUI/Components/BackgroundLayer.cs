using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Resources.Shaders.Definitions;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// Full-screen background layer that renders a solid color quad.
/// Renders in Main pass with priority 0 (first to render).
/// </summary>
public partial class BackgroundLayer(
    IPipelineManager pipelineManager,
    IResourceManager resources)
    : RenderableBase(), IRenderable
{
    private GeometryResource? _geometry;
    private Pipeline _pipeline;

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

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (_geometry == null)
            yield break;

        yield return new DrawCommand
        {
            RenderMask = RenderMask,
            Pipeline = _pipeline,
            VertexBuffer = _geometry.Buffer,
            VertexCount = _geometry.VertexCount,
            InstanceCount = 1
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