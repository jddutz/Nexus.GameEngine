using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Extension methods for adding render pass configuration to pipeline builders.
/// </summary>
public static class PipelineBuilderRenderPassExtensions
{
    /// <summary>
    /// Sets the target render pass for this pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="renderPass">The render pass to target.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public static IPipelineBuilder WithRenderPass(this IPipelineBuilder builder, RenderPass renderPass)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.RenderPass = renderPass;
        }
        return builder;
    }
}
