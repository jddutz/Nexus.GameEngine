namespace Nexus.GameEngine.Resources.Fonts.FontSource;

/// <summary>
/// Abstraction for loading font data from different sources (embedded resources, files, URIs, etc.).
/// </summary>
public interface IFontSource
{
    /// <summary>
    /// Loads the TrueType font data as a byte array.
    /// </summary>
    /// <returns>TrueType font data bytes.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the font cannot be found.</exception>
    /// <exception cref="IOException">Thrown if the font cannot be read.</exception>
    byte[] LoadFontData();
    
    /// <summary>
    /// Gets a unique identifier for this font source, used for caching.
    /// </summary>
    /// <returns>Unique name identifying this font (e.g., "Roboto-Regular.ttf").</returns>
    string GetUniqueName();
}
