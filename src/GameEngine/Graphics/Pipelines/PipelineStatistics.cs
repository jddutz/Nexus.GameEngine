namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Statistics about pipeline cache performance and usage.
/// Useful for profiling and debugging pipeline management.
/// </summary>
public record PipelineStatistics
{
    /// <summary>
    /// Total number of pipelines currently cached in memory.
    /// </summary>
    public int CachedPipelineCount { get; init; }

    /// <summary>
    /// Total number of pipeline creation requests.
    /// </summary>
    public int TotalCreateRequests { get; init; }

    /// <summary>
    /// Number of times a pipeline was returned from cache (cache hit).
    /// </summary>
    public int CacheHits { get; init; }

    /// <summary>
    /// Number of times a new pipeline had to be created (cache miss).
    /// </summary>
    public int CacheMisses { get; init; }

    /// <summary>
    /// Number of pipelines that failed to compile.
    /// </summary>
    public int CompilationFailures { get; init; }

    /// <summary>
    /// Number of pipelines invalidated and removed from cache.
    /// </summary>
    public int InvalidationCount { get; init; }

    /// <summary>
    /// Total time spent creating pipelines (milliseconds).
    /// </summary>
    public double TotalCreationTimeMs { get; init; }

    /// <summary>
    /// Average time to create a pipeline (milliseconds).
    /// </summary>
    public double AverageCreationTimeMs => 
        CacheMisses > 0 ? TotalCreationTimeMs / CacheMisses : 0;

    /// <summary>
    /// Cache hit rate (percentage 0-100).
    /// </summary>
    public double CacheHitRate => 
        TotalCreateRequests > 0 ? (CacheHits * 100.0 / TotalCreateRequests) : 0;

    /// <summary>
    /// Estimated GPU memory usage by all cached pipelines (bytes).
    /// </summary>
    public long EstimatedMemoryUsageBytes { get; init; }
}
