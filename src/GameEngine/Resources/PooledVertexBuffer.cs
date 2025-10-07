namespace Nexus.GameEngine.Resources;

/// <summary>
/// A pooled vertex buffer resource
/// </summary>
public class PooledVertexBuffer(uint resourceId, int size) : PooledResource(resourceId, PooledResourceType.VertexBuffer)
{
    /// <summary>
    /// Size of the buffer in bytes
    /// </summary>
    public int Size { get; } = size;

    public override int EstimatedMemoryUsage => Size;

    public override string ToString() => $"VertexBuffer[{ResourceId}] Size:{Size} Rented:{IsRented}";
}