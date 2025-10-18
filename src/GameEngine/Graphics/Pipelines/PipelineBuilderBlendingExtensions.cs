namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Extension methods for adding blending configuration to pipeline builders.
/// </summary>
public static class PipelineBuilderBlendingExtensions
{
    /// <summary>
    /// Enables or disables blending (default: disabled).
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="enable">True to enable blending, false to disable.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public static IPipelineBuilder WithBlending(this IPipelineBuilder builder, bool enable = true)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.EnableBlending = enable;
        }
        return builder;
    }
}
