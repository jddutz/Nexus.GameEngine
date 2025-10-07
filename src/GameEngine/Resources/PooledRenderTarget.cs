namespace Nexus.GameEngine.Resources;

/// <summary>
/// A pooled render target resource
/// </summary>
public class PooledRenderTarget(uint resourceId, int width, int height, uint framebufferId, uint colorTextureId, uint? depthTextureId = null) : PooledResource(resourceId, PooledResourceType.RenderTarget)
{
    /// <summary>
    /// Width of the render target in pixels
    /// </summary>
    public int Width { get; } = width;

    /// <summary>
    /// Height of the render target in pixels
    /// </summary>
    public int Height { get; } = height;

    /// <summary>
    /// Framebuffer object ID
    /// </summary>
    public uint FramebufferId { get; } = framebufferId;

    /// <summary>
    /// Vector4D<float> texture ID
    /// </summary>
    public uint ColorTextureId { get; } = colorTextureId;

    /// <summary>
    /// Depth texture ID (optional)
    /// </summary>
    public uint? DepthTextureId { get; } = depthTextureId;

    public override int EstimatedMemoryUsage
    {
        get
        {
            // Vector4D<float> buffer (RGBA8) + optional depth buffer (Depth24Stencil8)
            var colorMemory = Width * Height * 4;
            var depthMemory = DepthTextureId.HasValue ? Width * Height * 4 : 0;
            return colorMemory + depthMemory;
        }
    }

    public override string ToString() => $"RenderTarget[{ResourceId}] {Width}x{Height} FBO:{FramebufferId} Rented:{IsRented}";
}