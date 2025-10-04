namespace Nexus.GameEngine.Resources;

/// <summary>
/// Statistics about resource manager usage
/// </summary>
public readonly struct ResourceManagerStatistics
{
    /// <summary>
    /// Total number of resources managed
    /// </summary>
    public int TotalResources { get; init; }

    /// <summary>
    /// Number of geometry resources
    /// </summary>
    public int GeometryCount { get; init; }

    /// <summary>
    /// Number of shader resources
    /// </summary>
    public int ShaderCount { get; init; }

    /// <summary>
    /// Number of texture resources
    /// </summary>
    public int TextureCount { get; init; }

    /// <summary>
    /// Estimated total memory usage in bytes
    /// </summary>
    public long EstimatedMemoryUsage { get; init; }

    /// <summary>
    /// Number of cache hits
    /// </summary>
    public long CacheHits { get; init; }

    /// <summary>
    /// Number of cache misses (resource creations)
    /// </summary>
    public long CacheMisses { get; init; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0)
    /// </summary>
    public double CacheHitRatio => CacheHits + CacheMisses == 0 ? 0.0 : (double)CacheHits / (CacheHits + CacheMisses);

    public override string ToString()
    {
        return $"Resources: {TotalResources} (Geometry: {GeometryCount}, Shaders: {ShaderCount}, Textures: {TextureCount}), " +
               $"Memory: {EstimatedMemoryUsage / 1024 / 1024}MB, Cache: {CacheHitRatio:P1}";
    }
}