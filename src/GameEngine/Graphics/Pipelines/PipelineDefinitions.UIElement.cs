namespace Nexus.GameEngine.Graphics.Pipelines;

public static partial class PipelineDefinitions
{
    /// <summary>
    /// Pipeline for UI elements with textured rendering (uber-shader).
    /// Uses UIElement shader with push constants for transform and tint color.
    /// Supports both solid colors (via UniformColor texture) and real textures.
    /// Renders to UI pass with alpha blending enabled.
    /// </summary>
    public static readonly PipelineDefinition UIElement = new(
        name: "UI_Element",
        configure: builder => builder
            .WithShader(ShaderDefinitions.UIElement)
            .WithRenderPasses(RenderPasses.UI)
            .WithTopology(PrimitiveTopology.TriangleStrip)
            .WithCullMode(CullModeFlags.None)
            .WithBlending()
            .WithDepthTest(false)
            .WithDepthWrite(false));
}
