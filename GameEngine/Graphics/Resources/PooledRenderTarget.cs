namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// A pooled render target resource
/// </summary>
public class PooledRenderTarget : PooledResource
{
    /// <summary>
    /// Width of the render target in pixels
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height of the render target in pixels
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Framebuffer object ID
    /// </summary>
    public uint FramebufferId { get; }

    /// <summary>
    /// Vector4D<float> texture ID
    /// </summary>
    public uint ColorTextureId { get; }

    /// <summary>
    /// Depth texture ID (optional)
    /// </summary>
    public uint? DepthTextureId { get; }

    public PooledRenderTarget(uint resourceId, int width, int height, uint framebufferId, uint colorTextureId, uint? depthTextureId = null)
        : base(resourceId, PooledResourceType.RenderTarget)
    {
        Width = width;
        Height = height;
        FramebufferId = framebufferId;
        ColorTextureId = colorTextureId;
        DepthTextureId = depthTextureId;
    }

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