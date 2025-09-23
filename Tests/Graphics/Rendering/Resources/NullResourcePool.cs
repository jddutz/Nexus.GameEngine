using Nexus.GameEngine.Graphics.Rendering.Resources;

namespace Tests.Graphics.Rendering.Resources;

/// <summary>
/// Null implementation of IResourcePool for testing
/// </summary>
public class NullResourcePool : IResourcePool
{
    public bool IsDisposed { get; private set; }
    public int ActiveResourceCount => 0;

    public PooledVertexBuffer RentVertexBuffer(int size)
    {
        return new NullPooledVertexBuffer();
    }

    public void ReturnVertexBuffer(PooledVertexBuffer buffer)
    {
        // No-op for testing
    }

    public PooledTexture RentTexture(int width, int height, TextureFormat format)
    {
        return new NullPooledTexture();
    }

    public void ReturnTexture(PooledTexture texture)
    {
        // No-op for testing
    }

    public PooledRenderTarget RentRenderTarget(int width, int height, bool includeDepth = true)
    {
        return new NullPooledRenderTarget();
    }

    public void ReturnRenderTarget(PooledRenderTarget target)
    {
        // No-op for testing
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

/// <summary>
/// Null implementation for testing
/// </summary>
public class NullPooledVertexBuffer : PooledVertexBuffer
{
    public NullPooledVertexBuffer() : base(1, 1)
    {
    }
}

/// <summary>
/// Null implementation for testing
/// </summary>
public class NullPooledTexture : PooledTexture
{
    public NullPooledTexture() : base(1, 1, 1, TextureFormat.RGBA8)
    {
    }
}

/// <summary>
/// Null implementation for testing
/// </summary>
public class NullPooledRenderTarget : PooledRenderTarget
{
    public NullPooledRenderTarget() : base(1, 1024, 1024, 1, 1, null)
    {
    }
}