namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// Types of pooled resources
/// </summary>
public enum PooledResourceType
{
    VertexBuffer,
    IndexBuffer,
    Texture,
    RenderTarget,
    UniformBuffer
}

/// <summary>
/// Texture formats supported by the resource pool
/// </summary>
public enum TextureFormat
{
    RGBA8,
    RGB8,
    RGBA16F,
    RGBA32F,
    Depth24Stencil8,
    Depth32F,
    R8,
    RG8,
    R16F,
    RG16F
}