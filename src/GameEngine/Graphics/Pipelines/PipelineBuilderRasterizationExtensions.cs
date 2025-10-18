using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Extension methods for adding rasterization configuration to pipeline builders.
/// </summary>
public static class PipelineBuilderRasterizationExtensions
{
    /// <summary>
    /// Sets the face culling mode (default: BackBit).
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="cullMode">The face culling mode to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public static IPipelineBuilder WithCullMode(this IPipelineBuilder builder, CullModeFlags cullMode)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.CullMode = cullMode;
        }
        return builder;
    }
    
    /// <summary>
    /// Sets the front face winding order (default: CounterClockwise).
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="frontFace">The front face winding order.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public static IPipelineBuilder WithFrontFace(this IPipelineBuilder builder, FrontFace frontFace)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.FrontFace = frontFace;
        }
        return builder;
    }
}
