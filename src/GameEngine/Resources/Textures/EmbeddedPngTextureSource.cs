using StbImageSharp;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Loads PNG/JPEG textures from embedded resources using StbImageSharp.
/// Supports both sRGB and linear color spaces.
/// </summary>
public class EmbeddedPngTextureSource : ITextureSource
{
    private readonly string _filePath;
    private readonly Assembly _sourceAssembly;
    private readonly bool _isSrgb;
    
    static EmbeddedPngTextureSource()
    {
        // Configure StbImage: do NOT flip images vertically on load
        // PNG images are stored with (0,0) at top-left, matching our UV coordinate system
        // where V=0 is top and V=1 is bottom (standard Vulkan/D3D texture coordinates).
        StbImage.stbi_set_flip_vertically_on_load(0);
    }
    
    /// <summary>
    /// Creates a source for loading PNG/JPEG textures from embedded resources.
    /// </summary>
    /// <param name="filePath">Path to the texture file within embedded resources (e.g., "Resources/Textures/image.png")</param>
    /// <param name="sourceAssembly">Assembly containing the embedded resource</param>
    /// <param name="isSrgb">True for sRGB color space (default for color textures), false for linear (normals, data textures)</param>
    public EmbeddedPngTextureSource(string filePath, Assembly sourceAssembly, bool isSrgb = true)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _sourceAssembly = sourceAssembly ?? throw new ArgumentNullException(nameof(sourceAssembly));
        _isSrgb = isSrgb;
    }
    
    /// <summary>
    /// Loads the texture data from embedded resources.
    /// </summary>
    public TextureSourceData Load()
    {
        var imageData = LoadImageFromEmbeddedResource(_filePath, _sourceAssembly);
        
        try
        {
            // Determine Vulkan format based on color space and component count
            var format = DetermineFormat(_isSrgb, imageData.Components);
            
            // Convert to byte array
            var pixelData = ConvertToByteArray(imageData.Pixels, imageData.Width, imageData.Height, imageData.Components);
            
            return new TextureSourceData
            {
                PixelData = pixelData,
                Width = imageData.Width,
                Height = imageData.Height,
                Format = format
            };
        }
        finally
        {
            // Free pixel data from StbImage
            Marshal.FreeHGlobal(imageData.Pixels);
        }
    }
    
    private struct ImageData
    {
        public IntPtr Pixels;
        public int Width;
        public int Height;
        public ColorComponents Components;
    }
    
    private ImageData LoadImageFromEmbeddedResource(string filePath, Assembly sourceAssembly)
    {
        // Convert path to embedded resource name
        // Example: "Resources/Textures/uvmap.png" -> "AssemblyName.Resources.Textures.uvmap.png"
        var assemblyName = sourceAssembly.GetName().Name;
        var resourceName = $"{assemblyName}.{filePath.Replace('/', '.')}";
        
        using var stream = sourceAssembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Texture resource not found: {resourceName} in assembly {assemblyName} (from path: {filePath})");
        }
        
        // Read stream into memory
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var imageDataBytes = memoryStream.ToArray();
        
        // Load image with StbImage
        ImageResult image;
        unsafe
        {
            fixed (byte* ptr = imageDataBytes)
            {
                using var imageStream = new UnmanagedMemoryStream(ptr, imageDataBytes.Length);
                image = ImageResult.FromStream(imageStream, ColorComponents.RedGreenBlueAlpha);
            }
        }
        
        if (image == null)
        {
            throw new InvalidOperationException($"Failed to load image from: {filePath}");
        }
        
        // Allocate unmanaged memory for pixel data (will be freed by caller)
        var pixelDataSize = image.Width * image.Height * (int)image.Comp;
        var pixels = Marshal.AllocHGlobal(pixelDataSize);
        Marshal.Copy(image.Data, 0, pixels, pixelDataSize);
        
        return new ImageData
        {
            Pixels = pixels,
            Width = image.Width,
            Height = image.Height,
            Components = image.Comp
        };
    }
    
    private Format DetermineFormat(bool isSrgb, ColorComponents components)
    {
        return (isSrgb, components) switch
        {
            (true, ColorComponents.RedGreenBlueAlpha) => Format.R8G8B8A8Srgb,
            (true, ColorComponents.RedGreenBlue) => Format.R8G8B8Srgb,
            (true, ColorComponents.GreyAlpha) => Format.R8G8Srgb,
            (true, ColorComponents.Grey) => Format.R8Srgb,
            (false, ColorComponents.RedGreenBlueAlpha) => Format.R8G8B8A8Unorm,
            (false, ColorComponents.RedGreenBlue) => Format.R8G8B8Unorm,
            (false, ColorComponents.GreyAlpha) => Format.R8G8Unorm,
            (false, ColorComponents.Grey) => Format.R8Unorm,
            _ => throw new NotSupportedException($"Unsupported image format: {components}")
        };
    }
    
    private byte[] ConvertToByteArray(IntPtr pixels, int width, int height, ColorComponents components)
    {
        var pixelDataSize = width * height * (int)components;
        var pixelData = new byte[pixelDataSize];
        Marshal.Copy(pixels, pixelData, 0, pixelDataSize);
        return pixelData;
    }
}
