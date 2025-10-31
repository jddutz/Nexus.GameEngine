namespace Nexus.GameEngine.Graphics.Pipelines;

public static partial class PipelineDefinitions
{
    /// <summary>
    /// Pipeline for radial gradient backgrounds.
    /// Uses RadialGradient shader with UBO for gradient definition and push constants for center/radius.
    /// Renders to Main pass with depth testing enabled.
    /// </summary>
    public static readonly PipelineDefinition RadialGradient = new(
        name: "RadialGradient_Background",
        configure: builder => builder
            .WithShader(ShaderDefinitions.RadialGradient)
            .WithRenderPasses(RenderPasses.Main)
            .WithTopology(PrimitiveTopology.TriangleStrip)
            .WithCullMode(CullModeFlags.None)
            .WithDepthTest()
            .WithDepthWrite());
}
