using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Extension methods for adding topology configuration to pipeline builders.
/// </summary>
public static class PipelineBuilderTopologyExtensions
{
    /// <summary>
    /// Sets the primitive topology (default: TriangleList).
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="topology">The primitive topology to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public static IPipelineBuilder WithTopology(this IPipelineBuilder builder, PrimitiveTopology topology)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.Topology = topology;
        }
        return builder;
    }
}
