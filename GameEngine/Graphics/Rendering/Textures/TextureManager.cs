using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using Nexus.GameEngine.Graphics.Rendering.Resources;
using System.Collections.Concurrent;

namespace Nexus.GameEngine.Graphics.Rendering.Textures;

/// <summary>
/// Manages texture loading, caching, and GPU memory optimization
/// </summary>
public class TextureManager : IDisposable
{
    private readonly GL _gl;
    private readonly ILogger _logger;
    private readonly ResourcePool _resourcePool;
    private readonly ConcurrentDictionary<string, ManagedTexture> _textureCache;
    private readonly object _lockObject = new();

    private int _cacheHits;
    private int _cacheMisses;
    private bool _disposed;

    /// <summary>
    /// Gets whether this manager has been disposed
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the number of loaded textures
    /// </summary>
    public int LoadedTextureCount => _textureCache.Count;

    public TextureManager(GL gl, ILogger logger, ResourcePool resourcePool)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourcePool = resourcePool ?? throw new ArgumentNullException(nameof(resourcePool));
        _textureCache = new ConcurrentDictionary<string, ManagedTexture>();

        _logger.LogDebug("Created TextureManager");
    }

    /// <summary>
    /// Loads a texture from raw data with caching
    /// </summary>
    /// <param name="filePath">File path for caching key</param>
    /// <param name="data">Raw texture data</param>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="format">Texture format</param>
    /// <param name="usage">Usage hint for optimization</param>
    /// <returns>Managed texture</returns>
    public ManagedTexture LoadTexture(string filePath, ReadOnlySpan<byte> data, int width, int height,
        TextureFormat format, TextureUsage usage = TextureUsage.Static)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        // Check cache first
        if (_textureCache.TryGetValue(filePath, out var cachedTexture))
        {
            cachedTexture.OnAccess();
            _cacheHits++;
            _logger.LogDebug("Cache hit for texture: {FilePath}", filePath);
            return cachedTexture;
        }

        _cacheMisses++;

        // Create new texture
        var texture = CreateTextureInternal(filePath, data, width, height, format, usage, filePath);
        _textureCache.TryAdd(filePath, texture);

        _logger.LogDebug("Loaded texture: {FilePath} ({Width}x{Height} {Format})",
            filePath, width, height, format);

        return texture;
    }

    /// <summary>
    /// Creates a runtime texture (not cached by file path)
    /// </summary>
    /// <param name="name">Unique name for the texture</param>
    /// <param name="data">Raw texture data</param>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="format">Texture format</param>
    /// <param name="usage">Usage hint for optimization</param>
    /// <returns>Managed texture</returns>
    public ManagedTexture CreateTexture(string name, ReadOnlySpan<byte> data, int width, int height,
        TextureFormat format, TextureUsage usage = TextureUsage.Static)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        if (width <= 0 || height <= 0)
            throw new ArgumentException("Texture dimensions must be positive");

        // Check if name already exists
        if (_textureCache.TryGetValue(name, out var existingTexture))
        {
            existingTexture.OnAccess();
            return existingTexture;
        }

        var texture = CreateTextureInternal(name, data, width, height, format, usage);
        _textureCache.TryAdd(name, texture);

        _logger.LogDebug("Created runtime texture: {Name} ({Width}x{Height} {Format})",
            name, width, height, format);

        return texture;
    }

    /// <summary>
    /// Creates a render texture for use as a render target
    /// </summary>
    /// <param name="name">Unique name for the render texture</param>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="format">Texture format</param>
    /// <returns>Managed render texture</returns>
    public ManagedTexture CreateRenderTexture(string name, int width, int height, TextureFormat format)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        if (width <= 0 || height <= 0)
            throw new ArgumentException("Render texture dimensions must be positive");

        // Check if name already exists
        if (_textureCache.TryGetValue(name, out var existingTexture))
        {
            existingTexture.OnAccess();
            return existingTexture;
        }

        var textureId = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, textureId);

        // Create empty texture for render target
        var (internalFormat, pixelFormat, pixelType) = GetGLFormats(format);
        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)width, (uint)height,
                0, pixelFormat, pixelType, (void*)0);
        }

        // Set default parameters for render textures
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        var texture = new ManagedTexture(name, textureId, width, height, format, isRenderTarget: true);
        _textureCache.TryAdd(name, texture);

        _logger.LogDebug("Created render texture: {Name} ({Width}x{Height} {Format})",
            name, width, height, format);

        return texture;
    }

    /// <summary>
    /// Gets a texture by name
    /// </summary>
    /// <param name="name">Name or file path of the texture</param>
    /// <returns>Managed texture or null if not found</returns>
    public ManagedTexture? GetTexture(string name)
    {
        ThrowIfDisposed();

        if (_textureCache.TryGetValue(name, out var texture))
        {
            texture.OnAccess();
            return texture;
        }

        return null;
    }

    /// <summary>
    /// Unloads a texture and removes it from cache
    /// </summary>
    /// <param name="name">Name or file path of the texture</param>
    /// <returns>True if texture was found and unloaded</returns>
    public bool UnloadTexture(string name)
    {
        ThrowIfDisposed();

        if (_textureCache.TryRemove(name, out var texture))
        {
            _gl.DeleteTexture(texture.TextureId);
            _logger.LogDebug("Unloaded texture: {Name}", name);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Generates mipmaps for a texture
    /// </summary>
    /// <param name="texture">Texture to generate mipmaps for</param>
    public void GenerateMipmaps(ManagedTexture texture)
    {
        ThrowIfDisposed();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        _gl.BindTexture(TextureTarget.Texture2D, texture.TextureId);
        _gl.GenerateMipmap(TextureTarget.Texture2D);

        _logger.LogDebug("Generated mipmaps for texture: {Name}", texture.Name);
    }

    /// <summary>
    /// Sets texture filtering parameters
    /// </summary>
    /// <param name="texture">Texture to update</param>
    /// <param name="minFilter">Minification filter</param>
    /// <param name="magFilter">Magnification filter</param>
    public void SetTextureFiltering(ManagedTexture texture, TextureFilter minFilter, TextureFilter magFilter)
    {
        ThrowIfDisposed();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        _gl.BindTexture(TextureTarget.Texture2D, texture.TextureId);

        var glMinFilter = ConvertMinFilter(minFilter);
        var glMagFilter = ConvertMagFilter(magFilter);

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)glMinFilter);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)glMagFilter);

        _logger.LogDebug("Set filtering for texture {Name}: Min={MinFilter}, Mag={MagFilter}",
            texture.Name, minFilter, magFilter);
    }

    /// <summary>
    /// Sets texture wrapping parameters
    /// </summary>
    /// <param name="texture">Texture to update</param>
    /// <param name="wrapS">S (horizontal) wrapping mode</param>
    /// <param name="wrapT">T (vertical) wrapping mode</param>
    public void SetTextureWrapping(ManagedTexture texture, TextureWrap wrapS, TextureWrap wrapT)
    {
        ThrowIfDisposed();

        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        _gl.BindTexture(TextureTarget.Texture2D, texture.TextureId);

        var glWrapS = ConvertWrapMode(wrapS);
        var glWrapT = ConvertWrapMode(wrapT);

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)glWrapS);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)glWrapT);

        _logger.LogDebug("Set wrapping for texture {Name}: S={WrapS}, T={WrapT}",
            texture.Name, wrapS, wrapT);
    }

    /// <summary>
    /// Gets the total memory usage of all loaded textures
    /// </summary>
    /// <returns>Memory usage in bytes</returns>
    public long GetMemoryUsage()
    {
        ThrowIfDisposed();

        return _textureCache.Values.Sum(t => (long)t.EstimatedMemoryUsage);
    }

    /// <summary>
    /// Gets texture manager statistics
    /// </summary>
    /// <returns>Statistics</returns>
    public TextureManagerStatistics GetStatistics()
    {
        ThrowIfDisposed();

        var textures = _textureCache.Values.ToArray();
        var totalTextures = textures.Length;
        var loadedTextures = textures.Count(t => !t.IsRenderTarget);
        var renderTextures = textures.Count(t => t.IsRenderTarget);
        var memoryUsage = textures.Sum(t => (long)t.EstimatedMemoryUsage);

        return new TextureManagerStatistics(
            totalTextures,
            loadedTextures,
            renderTextures,
            memoryUsage,
            _cacheHits,
            _cacheMisses);
    }

    /// <summary>
    /// Creates the actual texture object
    /// </summary>
    private ManagedTexture CreateTextureInternal(string name, ReadOnlySpan<byte> data, int width, int height,
        TextureFormat format, TextureUsage usage, string? filePath = null)
    {
        var textureId = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, textureId);

        var (internalFormat, pixelFormat, pixelType) = GetGLFormats(format);
        unsafe
        {
            fixed (byte* ptr = data)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)width, (uint)height,
                    0, pixelFormat, pixelType, ptr);
            }
        }

        // Set default parameters
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        return new ManagedTexture(name, textureId, width, height, format, isRenderTarget: false, filePath, usage);
    }

    /// <summary>
    /// Converts TextureFormat to OpenGL formats
    /// </summary>
    private static (InternalFormat, PixelFormat, PixelType) GetGLFormats(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.RGBA8 => (InternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte),
            TextureFormat.RGB8 => (InternalFormat.Rgb8, PixelFormat.Rgb, PixelType.UnsignedByte),
            TextureFormat.RGBA16F => (InternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat),
            TextureFormat.RGBA32F => (InternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
            TextureFormat.R8 => (InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
            TextureFormat.RG8 => (InternalFormat.RG8, PixelFormat.RG, PixelType.UnsignedByte),
            TextureFormat.R16F => (InternalFormat.R16f, PixelFormat.Red, PixelType.HalfFloat),
            TextureFormat.RG16F => (InternalFormat.RG16f, PixelFormat.RG, PixelType.HalfFloat),
            TextureFormat.Depth24Stencil8 => (InternalFormat.Depth24Stencil8, PixelFormat.DepthStencil, PixelType.UnsignedInt248),
            TextureFormat.Depth32F => (InternalFormat.DepthComponent32f, PixelFormat.DepthComponent, PixelType.Float),
            _ => (InternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte)
        };
    }

    /// <summary>
    /// Converts TextureFilter to OpenGL minification filter
    /// </summary>
    private static TextureMinFilter ConvertMinFilter(TextureFilter filter)
    {
        return filter switch
        {
            TextureFilter.Nearest => TextureMinFilter.Nearest,
            TextureFilter.Linear => TextureMinFilter.Linear,
            TextureFilter.NearestMipmapNearest => TextureMinFilter.NearestMipmapNearest,
            TextureFilter.LinearMipmapNearest => TextureMinFilter.LinearMipmapNearest,
            TextureFilter.NearestMipmapLinear => TextureMinFilter.NearestMipmapLinear,
            TextureFilter.LinearMipmapLinear => TextureMinFilter.LinearMipmapLinear,
            _ => TextureMinFilter.Linear
        };
    }

    /// <summary>
    /// Converts TextureFilter to OpenGL magnification filter
    /// </summary>
    private static TextureMagFilter ConvertMagFilter(TextureFilter filter)
    {
        return filter switch
        {
            TextureFilter.Nearest => TextureMagFilter.Nearest,
            _ => TextureMagFilter.Linear // Mag filter only supports Nearest or Linear
        };
    }

    /// <summary>
    /// Converts TextureWrap to OpenGL wrap mode
    /// </summary>
    private static TextureWrapMode ConvertWrapMode(TextureWrap wrap)
    {
        return wrap switch
        {
            TextureWrap.Repeat => TextureWrapMode.Repeat,
            TextureWrap.ClampToEdge => TextureWrapMode.ClampToEdge,
            TextureWrap.ClampToBorder => TextureWrapMode.ClampToBorder,
            TextureWrap.MirroredRepeat => TextureWrapMode.MirroredRepeat,
            _ => TextureWrapMode.Repeat
        };
    }

    /// <summary>
    /// Throws if the manager has been disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TextureManager));
    }

    /// <summary>
    /// Disposes the manager and releases all textures
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("Disposing TextureManager with {TextureCount} textures", _textureCache.Count);

        // Delete all textures
        foreach (var texture in _textureCache.Values)
        {
            _gl.DeleteTexture(texture.TextureId);
            _logger.LogDebug("Deleted texture {Name} (ID: {TextureId})", texture.Name, texture.TextureId);
        }

        _textureCache.Clear();
        _disposed = true;
    }
}