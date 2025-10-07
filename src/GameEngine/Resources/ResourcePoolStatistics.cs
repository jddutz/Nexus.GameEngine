namespace Nexus.GameEngine.Resources;

/// <summary>
/// Statistics about resource pool usage
/// </summary>
public readonly struct ResourcePoolStatistics(int totalResources, int rentedResources, long memoryUsage,
    int vertexBufferCount, int textureCount, int renderTargetCount)
{
    /// <summary>
    /// Total number of resources in the pool
    /// </summary>
    public int TotalResources { get; } = totalResources;

    /// <summary>
    /// Number of currently rented resources
    /// </summary>
    public int RentedResources { get; } = rentedResources;

    /// <summary>
    /// Number of available resources
    /// </summary>
    public int AvailableResources { get; } = totalResources - rentedResources;

    /// <summary>
    /// Estimated total memory usage in bytes
    /// </summary>
    public long MemoryUsage { get; } = memoryUsage;

    /// <summary>
    /// Number of vertex buffers
    /// </summary>
    public int VertexBufferCount { get; } = vertexBufferCount;

    /// <summary>
    /// Number of textures
    /// </summary>
    public int TextureCount { get; } = textureCount;

    /// <summary>
    /// Number of render targets
    /// </summary>
    public int RenderTargetCount { get; } = renderTargetCount;

    /// <summary>
    /// Pool efficiency (0.0 to 1.0)
    /// </summary>
    public float PoolEfficiency { get; } = totalResources > 0 ? (float)rentedResources / totalResources : 0f;

    public override string ToString() =>
        $"ResourcePool: {RentedResources}/{TotalResources} rented ({PoolEfficiency:P1}), {MemoryUsage:N0} bytes, VB:{VertexBufferCount} TX:{TextureCount} RT:{RenderTargetCount}";
}