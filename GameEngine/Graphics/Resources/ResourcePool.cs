using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using System.Collections.Concurrent;

namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// Object pool for graphics resources to minimize allocation overhead
/// </summary>
public class ResourcePool : IResourcePool
{
    private readonly GL _gl;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, List<PooledResource>> _resourcePools;
    private readonly object _lockObject = new();

    private bool _disposed;

    /// <summary>
    /// Gets whether this pool has been disposed
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the total number of active resources in the pool
    /// </summary>
    public int ActiveResourceCount
    {
        get
        {
            lock (_lockObject)
            {
                return _resourcePools.Values.Sum(pool => pool.Count);
            }
        }
    }

    public ResourcePool(GL gl, ILogger logger)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourcePools = new ConcurrentDictionary<string, List<PooledResource>>();

        _logger.LogDebug("Created ResourcePool");
    }

    /// <summary>
    /// Rents a vertex buffer from the pool
    /// </summary>
    /// <param name="size">Size of the buffer in bytes</param>
    /// <returns>A pooled vertex buffer</returns>
    public PooledVertexBuffer RentVertexBuffer(int size)
    {
        ThrowIfDisposed();

        if (size <= 0)
            throw new ArgumentException("Buffer size must be positive", nameof(size));

        var key = $"VertexBuffer_{size}";
        var pool = _resourcePools.GetOrAdd(key, _ => []);

        lock (_lockObject)
        {
            // Try to find an available buffer
            var availableBuffer = pool.OfType<PooledVertexBuffer>()
                .FirstOrDefault(b => !b.IsRented);

            if (availableBuffer != null)
            {
                availableBuffer.OnRent();
                _logger.LogDebug("Reused vertex buffer {ResourceId} with size {Size}",
                    availableBuffer.ResourceId, size);
                return availableBuffer;
            }

            // Create new buffer
            var bufferId = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, bufferId);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)size, in IntPtr.Zero, BufferUsageARB.DynamicDraw);

            var newBuffer = new PooledVertexBuffer(bufferId, size);
            newBuffer.OnRent();
            pool.Add(newBuffer);

            _logger.LogDebug("Created new vertex buffer {ResourceId} with size {Size}",
                bufferId, size);

            return newBuffer;
        }
    }

    /// <summary>
    /// Returns a vertex buffer to the pool
    /// </summary>
    /// <param name="buffer">The buffer to return</param>
    public void ReturnVertexBuffer(PooledVertexBuffer buffer)
    {
        ThrowIfDisposed();

        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        if (!buffer.IsRented)
            throw new InvalidOperationException("Buffer is not currently rented");

        buffer.OnReturn();
        _logger.LogDebug("Returned vertex buffer {ResourceId} to pool", buffer.ResourceId);
    }

    /// <summary>
    /// Rents a texture from the pool
    /// </summary>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="format">Texture format</param>
    /// <returns>A pooled texture</returns>
    public PooledTexture RentTexture(int width, int height, TextureFormat format)
    {
        ThrowIfDisposed();

        if (width <= 0 || height <= 0)
            throw new ArgumentException("Texture dimensions must be positive");

        var key = $"Texture_{width}x{height}_{format}";
        var pool = _resourcePools.GetOrAdd(key, _ => []);

        lock (_lockObject)
        {
            // Try to find an available texture
            var availableTexture = pool.OfType<PooledTexture>()
                .FirstOrDefault(t => !t.IsRented);

            if (availableTexture != null)
            {
                availableTexture.OnRent();
                _logger.LogDebug("Reused texture {ResourceId} {Width}x{Height} {Format}",
                    availableTexture.ResourceId, width, height, format);
                return availableTexture;
            }

            // Create new texture
            var textureId = _gl.GenTexture();

            var newTexture = new PooledTexture(textureId, width, height, format);
            newTexture.OnRent();
            pool.Add(newTexture);

            _logger.LogDebug("Created new texture {ResourceId} {Width}x{Height} {Format}",
                textureId, width, height, format);

            return newTexture;
        }
    }

    /// <summary>
    /// Returns a texture to the pool
    /// </summary>
    /// <param name="texture">The texture to return</param>
    public void ReturnTexture(PooledTexture texture)
    {
        ThrowIfDisposed();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        if (!texture.IsRented)
            throw new InvalidOperationException("Texture is not currently rented");

        texture.OnReturn();
        _logger.LogDebug("Returned texture {ResourceId} to pool", texture.ResourceId);
    }

    /// <summary>
    /// Rents a render target from the pool
    /// </summary>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="includeDepth">Whether to include a depth buffer</param>
    /// <returns>A pooled render target</returns>
    public PooledRenderTarget RentRenderTarget(int width, int height, bool includeDepth = true)
    {
        ThrowIfDisposed();

        if (width <= 0 || height <= 0)
            throw new ArgumentException("Render target dimensions must be positive");

        var key = $"RenderTarget_{width}x{height}_{includeDepth}";
        var pool = _resourcePools.GetOrAdd(key, _ => []);

        lock (_lockObject)
        {
            // Try to find an available render target
            var availableTarget = pool.OfType<PooledRenderTarget>()
                .FirstOrDefault(rt => !rt.IsRented);

            if (availableTarget != null)
            {
                availableTarget.OnRent();
                _logger.LogDebug("Reused render target {ResourceId} {Width}x{Height}",
                    availableTarget.ResourceId, width, height);
                return availableTarget;
            }

            // Create new render target
            var framebufferId = _gl.GenFramebuffer();
            var colorTextureId = _gl.GenTexture();
            uint? depthTextureId = includeDepth ? _gl.GenTexture() : null;

            var newTarget = new PooledRenderTarget(framebufferId, width, height,
                framebufferId, colorTextureId, depthTextureId);
            newTarget.OnRent();
            pool.Add(newTarget);

            _logger.LogDebug("Created new render target {ResourceId} {Width}x{Height} Depth:{IncludeDepth}",
                framebufferId, width, height, includeDepth);

            return newTarget;
        }
    }

    /// <summary>
    /// Returns a render target to the pool
    /// </summary>
    /// <param name="renderTarget">The render target to return</param>
    public void ReturnRenderTarget(PooledRenderTarget renderTarget)
    {
        ThrowIfDisposed();

        if (renderTarget == null)
            throw new ArgumentNullException(nameof(renderTarget));

        if (!renderTarget.IsRented)
            throw new InvalidOperationException("Render target is not currently rented");

        renderTarget.OnReturn();
        _logger.LogDebug("Returned render target {ResourceId} to pool", renderTarget.ResourceId);
    }

    /// <summary>
    /// Cleans up unused resources older than the specified age
    /// </summary>
    /// <param name="maxAge">Maximum age for unused resources</param>
    public void CleanupUnusedResources(TimeSpan maxAge)
    {
        ThrowIfDisposed();

        var cutoffTime = DateTime.UtcNow - maxAge;
        var cleanedCount = 0;

        lock (_lockObject)
        {
            foreach (var poolEntry in _resourcePools.ToArray())
            {
                var pool = poolEntry.Value;
                var resourcesToRemove = pool.Where(r => !r.IsRented && r.LastReturnTime < cutoffTime).ToList();

                foreach (var resource in resourcesToRemove)
                {
                    pool.Remove(resource);
                    DeleteResource(resource);
                    cleanedCount++;
                }

                // Remove empty pools
                if (pool.Count == 0)
                {
                    _resourcePools.TryRemove(poolEntry.Key, out _);
                }
            }
        }

        if (cleanedCount > 0)
        {
            _logger.LogDebug("Cleaned up {Count} unused resources older than {MaxAge}",
                cleanedCount, maxAge);
        }
    }

    /// <summary>
    /// Gets current resource pool statistics
    /// </summary>
    /// <returns>Pool statistics</returns>
    public ResourcePoolStatistics GetStatistics()
    {
        ThrowIfDisposed();

        lock (_lockObject)
        {
            var allResources = _resourcePools.Values.SelectMany(pool => pool).ToArray();
            var totalResources = allResources.Length;
            var rentedResources = allResources.Count(r => r.IsRented);
            var memoryUsage = allResources.Sum(r => (long)r.EstimatedMemoryUsage);

            var vertexBufferCount = allResources.Count(r => r.ResourceType == PooledResourceType.VertexBuffer);
            var textureCount = allResources.Count(r => r.ResourceType == PooledResourceType.Texture);
            var renderTargetCount = allResources.Count(r => r.ResourceType == PooledResourceType.RenderTarget);

            return new ResourcePoolStatistics(
                totalResources,
                rentedResources,
                memoryUsage,
                vertexBufferCount,
                textureCount,
                renderTargetCount);
        }
    }

    /// <summary>
    /// Deletes a specific resource from OpenGL
    /// </summary>
    private void DeleteResource(PooledResource resource)
    {
        switch (resource.ResourceType)
        {
            case PooledResourceType.VertexBuffer:
                _gl.DeleteBuffer(resource.ResourceId);
                break;
            case PooledResourceType.Texture:
                _gl.DeleteTexture(resource.ResourceId);
                break;
            case PooledResourceType.RenderTarget:
                if (resource is PooledRenderTarget rt)
                {
                    _gl.DeleteFramebuffer(rt.FramebufferId);
                    _gl.DeleteTexture(rt.ColorTextureId);
                    if (rt.DepthTextureId.HasValue)
                        _gl.DeleteTexture(rt.DepthTextureId.Value);
                }
                break;
        }

        _logger.LogDebug("Deleted {ResourceType} resource {ResourceId}",
            resource.ResourceType, resource.ResourceId);
    }

    /// <summary>
    /// Throws if the pool has been disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ResourcePool));
    }

    /// <summary>
    /// Disposes the pool and releases all resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("Disposing ResourcePool with {PoolCount} pools", _resourcePools.Count);

        lock (_lockObject)
        {
            // Delete all resources
            foreach (var pool in _resourcePools.Values)
            {
                foreach (var resource in pool)
                {
                    DeleteResource(resource);
                }
            }

            _resourcePools.Clear();
        }

        _disposed = true;
    }
}