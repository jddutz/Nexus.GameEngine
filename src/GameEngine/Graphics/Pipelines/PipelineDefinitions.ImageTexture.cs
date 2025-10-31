namespace Nexus.GameEngine.Graphics.Pipelines;

public static partial class PipelineDefinitions
{
    /// <summary>
    /// Pipeline for image texture backgrounds.
    /// Uses ImageTexture shader with combined image sampler and UV push constants.
    /// Renders to Main pass with depth testing enabled.
    /// </summary>
    public static readonly PipelineDefinition ImageTexture = new(
        name: "ImageTexture_Background",
        configure: builder => builder
            .WithShader(ShaderDefinitions.ImageTexture)
            .WithRenderPasses(RenderPasses.Main)
            .WithTopology(PrimitiveTopology.TriangleStrip)
            .WithCullMode(CullModeFlags.None)
            .WithDepthTest()
            .WithDepthWrite());
}
