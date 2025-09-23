namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Provides comprehensive statistics for monitoring rendering performance and batching efficiency
/// </summary>
public record RenderStatistics
{
    /// <summary>
    /// Time taken for the most recent render operation
    /// </summary>
    public TimeSpan LastRenderTime { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Average time per render operation across all frames
    /// </summary>
    public TimeSpan AverageRenderTime { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Number of components rendered in the last frame
    /// </summary>
    public int RenderedComponentCount { get; init; } = 0;

    /// <summary>
    /// Total number of frames rendered since last reset
    /// </summary>
    public long TotalFramesRendered { get; init; } = 0;

    /// <summary>
    /// Number of individual OpenGL draw calls made in the last frame
    /// </summary>
    public int DrawCalls { get; init; } = 0;

    /// <summary>
    /// Number of batches executed in the last frame
    /// </summary>
    public int BatchCount { get; init; } = 0;

    /// <summary>
    /// Number of draw calls that used GPU instancing
    /// </summary>
    public int InstancedDraws { get; init; } = 0;

    /// <summary>
    /// Number of GPU state changes (shader, texture, blend mode) in the last frame
    /// </summary>
    public int StateChanges { get; init; } = 0;

    /// <summary>
    /// Total number of vertices rendered in the last frame
    /// </summary>
    public int VerticesRendered { get; init; } = 0;

    /// <summary>
    /// Number of components culled by frustum culling
    /// </summary>
    public int FrustumCulledComponents { get; init; } = 0;

    /// <summary>
    /// Number of component tree branches skipped due to ShouldRenderChildren = false
    /// </summary>
    public int SpatialCulledBranches { get; init; } = 0;

    /// <summary>
    /// Average batch size (draw commands per batch)
    /// </summary>
    public float AverageBatchSize => BatchCount > 0 ? (float)DrawCalls / BatchCount : 0f;

    /// <summary>
    /// Batching efficiency ratio (0.0 = no batching, 1.0 = perfect batching)
    /// </summary>
    public float BatchingEfficiency => RenderedComponentCount > 0 ? 1.0f - ((float)DrawCalls / RenderedComponentCount) : 0f;

    /// <summary>
    /// Gets a comprehensive summary string of the render statistics
    /// </summary>
    public override string ToString()
    {
        return $"Frame: {LastRenderTime.TotalMilliseconds:F2}ms | " +
               $"Components: {RenderedComponentCount} | " +
               $"Draw Calls: {DrawCalls} ({InstancedDraws} instanced) | " +
               $"Batches: {BatchCount} (avg {AverageBatchSize:F1} cmds/batch) | " +
               $"State Changes: {StateChanges} | " +
               $"Vertices: {VerticesRendered:N0} | " +
               $"Culled: {FrustumCulledComponents} frustum, {SpatialCulledBranches} spatial | " +
               $"Batch Efficiency: {BatchingEfficiency:P1}";
    }

    /// <summary>
    /// Gets a concise performance summary
    /// </summary>
    public string GetPerformanceSummary()
    {
        return $"{LastRenderTime.TotalMilliseconds:F1}ms | {DrawCalls} draws | {BatchingEfficiency:P0} batched";
    }
}