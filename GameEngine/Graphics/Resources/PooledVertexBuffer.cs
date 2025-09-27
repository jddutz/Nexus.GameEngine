namespace Nexus.GameEngine.Graphics.Rendering.Resources;

/// <summary>
/// A pooled vertex buffer resource
/// </summary>
public class PooledVertexBuffer : PooledResource
{
    /// <summary>
    /// Size of the buffer in bytes
    /// </summary>
    public int Size { get; }

    public PooledVertexBuffer(uint resourceId, int size)
        : base(resourceId, PooledResourceType.VertexBuffer)
    {
        Size = size;
    }

    public override int EstimatedMemoryUsage => Size;

    public override string ToString() => $"VertexBuffer[{ResourceId}] Size:{Size} Rented:{IsRented}";
}