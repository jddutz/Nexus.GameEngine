using System.Reflection;

namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Loads TrueType fonts from embedded resources.
/// </summary>
public class EmbeddedTrueTypeFontSource : IFontSource
{
    private readonly string _fontPath;
    private readonly Assembly _sourceAssembly;
    
    /// <summary>
    /// Creates a source for loading TrueType fonts from embedded resources.
    /// </summary>
    /// <param name="fontPath">Path to the TTF font file within embedded resources (e.g., "EmbeddedResources/Fonts/Roboto-Regular.ttf")</param>
    /// <param name="sourceAssembly">Assembly containing the embedded font resource</param>
    /// <exception cref="ArgumentException">Thrown if font path doesn't end with .ttf</exception>
    public EmbeddedTrueTypeFontSource(string fontPath, Assembly sourceAssembly)
    {
        if (!fontPath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Font path must end with .ttf", nameof(fontPath));
        
        _fontPath = fontPath ?? throw new ArgumentNullException(nameof(fontPath));
        _sourceAssembly = sourceAssembly ?? throw new ArgumentNullException(nameof(sourceAssembly));
    }
    
    /// <summary>
    /// Loads the font data from embedded resources.
    /// </summary>
    public FontSourceData Load()
    {
        var fontData = LoadFontFromEmbeddedResource(_fontPath, _sourceAssembly);
        
        return new FontSourceData
        {
            FontFileData = fontData,
            FontName = Path.GetFileNameWithoutExtension(_fontPath)
        };
    }
    
    private byte[] LoadFontFromEmbeddedResource(string fontPath, Assembly sourceAssembly)
    {
        // Convert path to embedded resource name
        // Example: "EmbeddedResources/Fonts/Roboto-Regular.ttf" -> "AssemblyName.EmbeddedResources.Fonts.Roboto-Regular.ttf"
        var assemblyName = sourceAssembly.GetName().Name;
        var resourceName = $"{assemblyName}.{fontPath.Replace('/', '.')}";
        
        var stream = sourceAssembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
        {
            // Try without path prefix (e.g., "Roboto-Regular.ttf" -> "AssemblyName.EmbeddedResources.Fonts.Roboto-Regular.ttf")
            var fileName = Path.GetFileName(fontPath);
            resourceName = $"{assemblyName}.EmbeddedResources.Fonts.{fileName}";
            
            stream = sourceAssembly.GetManifestResourceStream(resourceName);
            
            if (stream == null)
            {
                throw new FileNotFoundException(
                    $"Font resource not found: {resourceName} in assembly {assemblyName} (from path: {fontPath})");
            }
        }
        
        using (stream)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
