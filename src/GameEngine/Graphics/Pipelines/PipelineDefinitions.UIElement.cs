namespace Nexus.GameEngine.Graphics.Pipelines;

public static partial class PipelineDefinitions
{
    /// <summary>
    /// Pipeline for UI elements with uniform color rendering.
    /// Uses UniformColorQuad shader with push constants for transform and color.
    /// Renders to UI pass with depth testing enabled.
    /// </summary>
    public static readonly PipelineDefinition UIElement = new(
        name: "UI_Element",
        configure: builder => builder
            .WithShader(ShaderDefinitions.UniformColorQuad)
            .WithRenderPasses(RenderPasses.UI)
            .WithTopology(PrimitiveTopology.TriangleStrip)
            .WithCullMode(CullModeFlags.None)
            .WithDepthTest()
            .WithDepthWrite());
}
