namespace Nexus.GameEngine.Graphics.Textures;

/// <summary>
/// Statistics about texture management
/// </summary>
public readonly struct TextureManagerStatistics
{
    /// <summary>
    /// Total number of textures
    /// </summary>
    public int TotalTextures { get; }

    /// <summary>
    /// Number of loaded textures (excluding render targets)
    /// </summary>
    public int LoadedTextures { get; }

    /// <summary>
    /// Number of render textures
    /// </summary>
    public int RenderTextures { get; }

    /// <summary>
    /// Total memory usage in bytes
    /// </summary>
    public long MemoryUsage { get; }

    /// <summary>
    /// Average texture size in bytes
    /// </summary>
    public float AverageTextureSize { get; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0)
    /// </summary>
    public float CacheHitRatio { get; }

    /// <summary>
    /// Number of cache hits
    /// </summary>
    public int CacheHits { get; }

    /// <summary>
    /// Number of cache misses
    /// </summary>
    public int CacheMisses { get; }

    public TextureManagerStatistics(int totalTextures, int loadedTextures, int renderTextures,
        long memoryUsage, int cacheHits, int cacheMisses)
    {
        TotalTextures = totalTextures;
        LoadedTextures = loadedTextures;
        RenderTextures = renderTextures;
        MemoryUsage = memoryUsage;
        AverageTextureSize = totalTextures > 0 ? (float)memoryUsage / totalTextures : 0f;
        CacheHits = cacheHits;
        CacheMisses = cacheMisses;

        var totalRequests = cacheHits + cacheMisses;
        CacheHitRatio = totalRequests > 0 ? (float)cacheHits / totalRequests : 0f;
    }

    public override string ToString() =>
        $"TextureManager: {TotalTextures} textures ({LoadedTextures} loaded, {RenderTextures} RT), {MemoryUsage:N0} bytes, Cache: {CacheHitRatio:P1} ({CacheHits}/{CacheHits + CacheMisses})";
}