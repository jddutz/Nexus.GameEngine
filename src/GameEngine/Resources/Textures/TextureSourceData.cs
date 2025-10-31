namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Raw texture data returned by texture sources.
/// Contains pixel data and metadata needed to create a Vulkan image.
/// </summary>
public record TextureSourceData
{
    /// <summary>
    /// Raw pixel data in the specified format.
    /// </summary>
    public required byte[] PixelData { get; init; }
    
    /// <summary>
    /// Texture width in pixels.
    /// </summary>
    public required int Width { get; init; }
    
    /// <summary>
    /// Texture height in pixels.
    /// </summary>
    public required int Height { get; init; }
    
    /// <summary>
    /// Vulkan pixel format of the data.
    /// </summary>
    public required Format Format { get; init; }
}
