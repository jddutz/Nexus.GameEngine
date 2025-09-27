namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// Statistics about resource pool usage
/// </summary>
public readonly struct ResourcePoolStatistics
{
    /// <summary>
    /// Total number of resources in the pool
    /// </summary>
    public int TotalResources { get; }

    /// <summary>
    /// Number of currently rented resources
    /// </summary>
    public int RentedResources { get; }

    /// <summary>
    /// Number of available resources
    /// </summary>
    public int AvailableResources { get; }

    /// <summary>
    /// Estimated total memory usage in bytes
    /// </summary>
    public long MemoryUsage { get; }

    /// <summary>
    /// Number of vertex buffers
    /// </summary>
    public int VertexBufferCount { get; }

    /// <summary>
    /// Number of textures
    /// </summary>
    public int TextureCount { get; }

    /// <summary>
    /// Number of render targets
    /// </summary>
    public int RenderTargetCount { get; }

    /// <summary>
    /// Pool efficiency (0.0 to 1.0)
    /// </summary>
    public float PoolEfficiency { get; }

    public ResourcePoolStatistics(int totalResources, int rentedResources, long memoryUsage,
        int vertexBufferCount, int textureCount, int renderTargetCount)
    {
        TotalResources = totalResources;
        RentedResources = rentedResources;
        AvailableResources = totalResources - rentedResources;
        MemoryUsage = memoryUsage;
        VertexBufferCount = vertexBufferCount;
        TextureCount = textureCount;
        RenderTargetCount = renderTargetCount;
        PoolEfficiency = totalResources > 0 ? (float)rentedResources / totalResources : 0f;
    }

    public override string ToString() =>
        $"ResourcePool: {RentedResources}/{TotalResources} rented ({PoolEfficiency:P1}), {MemoryUsage:N0} bytes, VB:{VertexBufferCount} TX:{TextureCount} RT:{RenderTargetCount}";
}