using Nexus.GameEngine.Graphics.Rendering.Resources;

namespace Nexus.GameEngine.Graphics.Rendering.Textures;

/// <summary>
/// Represents a managed texture resource
/// </summary>
public class ManagedTexture
{
    /// <summary>
    /// Unique name or identifier for this texture
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// File path if loaded from disk, null for runtime textures
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// OpenGL texture ID
    /// </summary>
    public uint TextureId { get; }

    /// <summary>
    /// Width in pixels
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height in pixels
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Texture format
    /// </summary>
    public TextureFormat Format { get; }

    /// <summary>
    /// Whether this texture is a render target
    /// </summary>
    public bool IsRenderTarget { get; }

    /// <summary>
    /// Whether this texture is currently loaded
    /// </summary>
    public bool IsLoaded { get; internal set; }

    /// <summary>
    /// When this texture was last accessed
    /// </summary>
    public DateTime LastAccessTime { get; internal set; }

    /// <summary>
    /// Number of times this texture has been accessed
    /// </summary>
    public int AccessCount { get; internal set; }

    /// <summary>
    /// Usage hint for optimization
    /// </summary>
    public TextureUsage Usage { get; }

    public ManagedTexture(string name, uint textureId, int width, int height,
        TextureFormat format, bool isRenderTarget = false, string? filePath = null,
        TextureUsage usage = TextureUsage.Static)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TextureId = textureId;
        Width = width;
        Height = height;
        Format = format;
        IsRenderTarget = isRenderTarget;
        FilePath = filePath;
        Usage = usage;
        IsLoaded = true;
        LastAccessTime = DateTime.UtcNow;
        AccessCount = 0;
    }

    /// <summary>
    /// Called when the texture is accessed
    /// </summary>
    internal void OnAccess()
    {
        LastAccessTime = DateTime.UtcNow;
        AccessCount++;
    }

    /// <summary>
    /// Gets the estimated memory usage of this texture in bytes
    /// </summary>
    public int EstimatedMemoryUsage => CalculateTextureMemory(Width, Height, Format);

    /// <summary>
    /// Calculates the estimated memory usage for a texture
    /// </summary>
    private static int CalculateTextureMemory(int width, int height, TextureFormat format)
    {
        var bytesPerPixel = format switch
        {
            TextureFormat.RGBA8 => 4,
            TextureFormat.RGB8 => 3,
            TextureFormat.RGBA16F => 8,
            TextureFormat.RGBA32F => 16,
            TextureFormat.Depth24Stencil8 => 4,
            TextureFormat.Depth32F => 4,
            TextureFormat.R8 => 1,
            TextureFormat.RG8 => 2,
            TextureFormat.R16F => 2,
            TextureFormat.RG16F => 4,
            _ => 4 // Default to RGBA8
        };

        return width * height * bytesPerPixel;
    }

    public override string ToString() => $"Texture[{Name}] {Width}x{Height} {Format} ID:{TextureId} RT:{IsRenderTarget}";
}