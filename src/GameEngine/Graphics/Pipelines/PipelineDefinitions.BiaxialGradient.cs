namespace Nexus.GameEngine.Graphics.Pipelines;

public static partial class PipelineDefinitions
{
    /// <summary>
    /// Pipeline for biaxial (4-corner) gradient backgrounds.
    /// Uses BiaxialGradient shader with UBO for corner colors.
    /// Renders to Main pass with depth testing enabled.
    /// </summary>
    public static readonly PipelineDefinition BiaxialGradient = new(
        name: "BiaxialGradient_Background",
        configure: builder => builder
            .WithShader(ShaderDefinitions.BiaxialGradient)
            .WithRenderPasses(RenderPasses.Main)
            .WithTopology(PrimitiveTopology.TriangleStrip)
            .WithCullMode(CullModeFlags.None)
            .WithDepthTest()
            .WithDepthWrite());
}
