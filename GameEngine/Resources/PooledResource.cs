namespace Nexus.GameEngine.Resources;

/// <summary>
/// Base class for pooled resources
/// </summary>
public abstract class PooledResource(uint resourceId, PooledResourceType resourceType)
{
    /// <summary>
    /// Unique identifier for this resource
    /// </summary>
    public uint ResourceId { get; } = resourceId;

    /// <summary>
    /// Type of this resource
    /// </summary>
    public PooledResourceType ResourceType { get; } = resourceType;

    /// <summary>
    /// Whether this resource is currently rented
    /// </summary>
    public bool IsRented { get; internal set; } = false;

    /// <summary>
    /// When this resource was last returned to the pool
    /// </summary>
    public DateTime LastReturnTime { get; internal set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of times this resource has been rented
    /// </summary>
    public int RentCount { get; internal set; } = 0;

    /// <summary>
    /// Called when the resource is rented from the pool
    /// </summary>
    internal virtual void OnRent()
    {
        IsRented = true;
        RentCount++;
    }

    /// <summary>
    /// Called when the resource is returned to the pool
    /// </summary>
    internal virtual void OnReturn()
    {
        IsRented = false;
        LastReturnTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the estimated memory usage of this resource in bytes
    /// </summary>
    public abstract int EstimatedMemoryUsage { get; }

    public override string ToString() => $"{ResourceType}[{ResourceId}] Rented:{IsRented} Count:{RentCount}";
}