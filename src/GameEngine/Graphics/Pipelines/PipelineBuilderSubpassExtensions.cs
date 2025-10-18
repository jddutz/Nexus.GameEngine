namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Extension methods for adding subpass configuration to pipeline builders.
/// </summary>
public static class PipelineBuilderSubpassExtensions
{
    /// <summary>
    /// Sets the subpass index within the render pass (default: 0).
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="subpass">The subpass index.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public static IPipelineBuilder WithSubpass(this IPipelineBuilder builder, uint subpass)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.Subpass = subpass;
        }
        return builder;
    }
}
