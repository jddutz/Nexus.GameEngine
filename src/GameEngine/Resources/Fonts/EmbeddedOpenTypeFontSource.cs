using System.Reflection;

namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Loads OpenType fonts from embedded resources.
/// Currently scaffolded - implementation pending.
/// </summary>
public class EmbeddedOpenTypeFontSource : IFontSource
{
    private readonly string _fontPath;
    private readonly Assembly _sourceAssembly;
    
    /// <summary>
    /// Creates a source for loading OpenType fonts from embedded resources.
    /// </summary>
    /// <param name="fontPath">Path to the OTF font file within embedded resources</param>
    /// <param name="sourceAssembly">Assembly containing the embedded font resource</param>
    /// <exception cref="ArgumentException">Thrown if font path doesn't end with .otf</exception>
    public EmbeddedOpenTypeFontSource(string fontPath, Assembly sourceAssembly)
    {
        if (!fontPath.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Font path must end with .otf", nameof(fontPath));
        
        _fontPath = fontPath;
        _sourceAssembly = sourceAssembly;
    }
    
    /// <summary>
    /// Loads the font data from embedded resources.
    /// </summary>
    /// <exception cref="NotImplementedException">OpenType font loading not yet implemented</exception>
    public FontSourceData Load()
    {
        throw new NotImplementedException(
            "OpenType font loading not yet implemented. " +
            "StbTrueType may support OTF - needs testing.");
        
        // If StbTrueType supports OTF, implementation would be identical to TrueType:
        // var fontData = LoadFontFromEmbeddedResource(_fontPath, _sourceAssembly);
        // return new FontSourceData { FontFileData = fontData, FontName = ... };
    }
}
