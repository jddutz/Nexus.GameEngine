namespace Nexus.GameEngine.Graphics.Pipelines;

public static partial class PipelineDefinitions
{
    /// <summary>
    /// Pipeline for linear gradient backgrounds.
    /// Uses LinearGradient shader with UBO for gradient definition and push constants for angle.
    /// Renders to Main pass with depth testing enabled.
    /// </summary>
    public static readonly PipelineDefinition LinearGradient = new(
        name: "LinearGradient_Background",
        configure: builder => builder
            .WithShader(ShaderDefinitions.LinearGradient)
            .WithRenderPasses(RenderPasses.Main)
            .WithTopology(PrimitiveTopology.TriangleStrip)
            .WithCullMode(CullModeFlags.None)
            .WithDepthTest()
            .WithDepthWrite());
}
