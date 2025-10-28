namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Extension methods for configuring pipelines with render pass masks.
/// Provides a cleaner API for specifying render passes using RenderPasses constants.
/// </summary>
public static class PipelineBuilderRenderPassMaskExtensions
{
    /// <summary>
    /// Sets the target render pass for this pipeline using a render pass mask.
    /// Automatically resolves the mask to the correct RenderPass from the swap chain.
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="renderPassMask">The render pass mask (e.g., RenderPasses.Main).</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <remarks>
    /// This is a convenience method that automatically converts a render pass mask
    /// to the correct render pass index. Supports only single-pass masks.
    /// For multi-pass scenarios, use WithRenderPass() with explicit pass selection.
    /// </remarks>
    /// <exception cref="ArgumentException">If mask contains multiple bits set or is zero.</exception>
    public static IPipelineBuilder WithRenderPasses(
        this IPipelineBuilder builder, 
        uint renderPassMask)
    {
        // Ensure only a single bit is set
        if (renderPassMask == 0 || (renderPassMask & (renderPassMask - 1)) != 0)
        {
            throw new ArgumentException(
                "WithRenderPasses only supports a single render pass. " +
                "Use WithRenderPass() directly for multi-pass scenarios or pass selection.", 
                nameof(renderPassMask));
        }

        if (builder is not PipelineBuilder impl)
        {
            throw new InvalidOperationException("Builder must be a PipelineBuilder instance");
        }

        // Convert bit flag to array index using Log2
        int passIndex = (int)Math.Log2(renderPassMask);
        var renderPass = impl.SwapChain.Passes[passIndex];
        
        return builder.WithRenderPass(renderPass);
    }
}
