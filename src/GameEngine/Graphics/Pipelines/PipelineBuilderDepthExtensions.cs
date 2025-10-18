namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Extension methods for adding depth/stencil configuration to pipeline builders.
/// </summary>
public static class PipelineBuilderDepthExtensions
{
    /// <summary>
    /// Enables or disables depth testing (default: enabled).
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="enable">True to enable depth testing, false to disable.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public static IPipelineBuilder WithDepthTest(this IPipelineBuilder builder, bool enable = true)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.EnableDepthTest = enable;
        }
        return builder;
    }
    
    /// <summary>
    /// Enables or disables depth writes (default: enabled).
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="enable">True to enable depth writes, false to disable.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public static IPipelineBuilder WithDepthWrite(this IPipelineBuilder builder, bool enable = true)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.EnableDepthWrite = enable;
        }
        return builder;
    }
}
