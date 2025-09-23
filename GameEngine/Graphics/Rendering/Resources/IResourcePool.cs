namespace Nexus.GameEngine.Graphics.Rendering.Resources;

/// <summary>
/// Interface for resource pooling to enable testing
/// </summary>
public interface IResourcePool : IDisposable
{
    /// <summary>
    /// Gets whether this pool has been disposed
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Gets the total number of active resources in the pool
    /// </summary>
    int ActiveResourceCount { get; }

    /// <summary>
    /// Rents a vertex buffer from the pool
    /// </summary>
    /// <param name="size">Size of the buffer in bytes</param>
    /// <returns>A pooled vertex buffer</returns>
    PooledVertexBuffer RentVertexBuffer(int size);

    /// <summary>
    /// Returns a vertex buffer to the pool
    /// </summary>
    /// <param name="buffer">The buffer to return</param>
    void ReturnVertexBuffer(PooledVertexBuffer buffer);

    /// <summary>
    /// Rents a texture from the pool
    /// </summary>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="format">Texture format</param>
    /// <returns>A pooled texture</returns>
    PooledTexture RentTexture(int width, int height, TextureFormat format);

    /// <summary>
    /// Returns a texture to the pool
    /// </summary>
    /// <param name="texture">The texture to return</param>
    void ReturnTexture(PooledTexture texture);

    /// <summary>
    /// Rents a render target from the pool
    /// </summary>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="includeDepth">Whether to include a depth buffer</param>
    /// <returns>A pooled render target</returns>
    PooledRenderTarget RentRenderTarget(int width, int height, bool includeDepth = true);

    /// <summary>
    /// Returns a render target to the pool
    /// </summary>
    /// <param name="target">The render target to return</param>
    void ReturnRenderTarget(PooledRenderTarget target);
}