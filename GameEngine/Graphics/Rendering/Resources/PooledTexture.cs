namespace Nexus.GameEngine.Graphics.Rendering.Resources;

/// <summary>
/// A pooled texture resource
/// </summary>
public class PooledTexture : PooledResource
{
    /// <summary>
    /// Width of the texture in pixels
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height of the texture in pixels
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Format of the texture
    /// </summary>
    public TextureFormat Format { get; }

    public PooledTexture(uint resourceId, int width, int height, TextureFormat format)
        : base(resourceId, PooledResourceType.Texture)
    {
        Width = width;
        Height = height;
        Format = format;
    }

    public override int EstimatedMemoryUsage => CalculateTextureMemory(Width, Height, Format);

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

    public override string ToString() => $"Texture[{ResourceId}] {Width}x{Height} {Format} Rented:{IsRented}";
}