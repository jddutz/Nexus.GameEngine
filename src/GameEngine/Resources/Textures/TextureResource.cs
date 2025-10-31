namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Represents a GPU texture resource with all Vulkan handles.
/// Managed by TextureResourceManager - do not create directly.
/// </summary>
public class TextureResource
{
    /// <summary>
    /// Vulkan image handle.
    /// </summary>
    public Image Image { get; }
    
    /// <summary>
    /// Device memory backing the image.
    /// </summary>
    public DeviceMemory ImageMemory { get; }
    
    /// <summary>
    /// Image view for shader access.
    /// </summary>
    public ImageView ImageView { get; }
    
    /// <summary>
    /// Sampler for texture filtering and addressing.
    /// </summary>
    public Sampler Sampler { get; }
    
    /// <summary>
    /// Image width in pixels.
    /// </summary>
    public uint Width { get; }
    
    /// <summary>
    /// Image height in pixels.
    /// </summary>
    public uint Height { get; }
    
    /// <summary>
    /// Vulkan image format.
    /// </summary>
    public Format Format { get; }
    
    /// <summary>
    /// Resource name (from definition).
    /// </summary>
    public string Name { get; }
    
    public TextureResource(
        Image image,
        DeviceMemory imageMemory,
        ImageView imageView,
        Sampler sampler,
        uint width,
        uint height,
        Format format,
        string name)
    {
        Image = image;
        ImageMemory = imageMemory;
        ImageView = imageView;
        Sampler = sampler;
        Width = width;
        Height = height;
        Format = format;
        Name = name;
    }
}
