using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Creates a texture from an array of RGBA colors.
/// Useful for solid colors, gradients, patterns, and test textures.
/// </summary>
public class ArgbArrayTextureSource : ITextureSource
{
    private readonly int _width;
    private readonly int _height;
    private readonly Vector4D<float>[] _colors;
    private readonly Format _format;
    
    /// <summary>
    /// Creates a texture from an array of RGBA colors.
    /// </summary>
    /// <param name="width">Texture width in pixels</param>
    /// <param name="height">Texture height in pixels</param>
    /// <param name="colors">Array of RGBA colors (length must equal width * height). Colors use 0-1 range.</param>
    /// <param name="format">Vulkan format (default: R8G8B8A8Unorm for linear color data)</param>
    /// <exception cref="ArgumentException">Thrown if color array length doesn't match width * height</exception>
    public ArgbArrayTextureSource(
        int width, 
        int height, 
        Vector4D<float>[] colors,
        Format format = Format.R8G8B8A8Unorm)
    {
        if (colors.Length != width * height)
            throw new ArgumentException(
                $"Color array length ({colors.Length}) must equal width * height ({width * height})", 
                nameof(colors));
        
        _width = width;
        _height = height;
        _colors = colors;
        _format = format;
    }
    
    /// <summary>
    /// Loads the texture data by converting float colors (0-1) to byte colors (0-255).
    /// </summary>
    public TextureSourceData Load()
    {
        // Convert float colors (0-1) to byte colors (0-255)
        // Assuming RGBA format = 4 bytes per pixel
        var pixelData = new byte[_width * _height * 4];
        
        for (int i = 0; i < _colors.Length; i++)
        {
            var color = _colors[i];
            pixelData[i * 4 + 0] = (byte)(Math.Clamp(color.X, 0f, 1f) * 255); // R
            pixelData[i * 4 + 1] = (byte)(Math.Clamp(color.Y, 0f, 1f) * 255); // G
            pixelData[i * 4 + 2] = (byte)(Math.Clamp(color.Z, 0f, 1f) * 255); // B
            pixelData[i * 4 + 3] = (byte)(Math.Clamp(color.W, 0f, 1f) * 255); // A
        }
        
        return new TextureSourceData
        {
            PixelData = pixelData,
            Width = _width,
            Height = _height,
            Format = _format
        };
    }
}
