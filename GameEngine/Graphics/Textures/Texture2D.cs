using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Textures;

/// <summary>
/// Represents a 2D texture for graphics rendering.
/// </summary>
public class Texture2D : IDisposable
{
    private readonly GL _gl;
    private uint _handle;
    private bool _disposed;

    /// <summary>
    /// Initializes a new 2D texture.
    /// </summary>
    /// <param name="gl">OpenGL context</param>
    /// <param name="width">Texture width</param>
    /// <param name="height">Texture height</param>
    public Texture2D(GL gl, int width, int height)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        Width = width;
        Height = height;

        _handle = _gl.GenTexture();
        Bind();

        // Set default texture parameters
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
    }

    /// <summary>
    /// Gets the texture width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the texture height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the OpenGL texture handle.
    /// </summary>
    public uint Handle => _handle;

    /// <summary>
    /// Gets the texture ID (same as Handle for compatibility).
    /// </summary>
    public uint Id => _handle;

    /// <summary>
    /// Binds this texture for rendering.
    /// </summary>
    public void Bind()
    {
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    /// <summary>
    /// Unbinds the current texture.
    /// </summary>
    public void Unbind()
    {
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
    /// Loads texture data from a byte array.
    /// </summary>
    /// <param name="data">Pixel data</param>
    /// <param name="format">Pixel format</param>
    public void LoadData(ReadOnlySpan<byte> data, PixelFormat format = PixelFormat.Rgba)
    {
        Bind();
        unsafe
        {
            fixed (byte* dataPtr = data)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)Width, (uint)Height, 0, format, PixelType.UnsignedByte, dataPtr);
            }
        }
        _gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    /// <summary>
    /// Disposes the texture resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _gl.DeleteTexture(_handle);
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}