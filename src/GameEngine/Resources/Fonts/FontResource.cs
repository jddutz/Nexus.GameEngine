using Nexus.GameEngine.Resources.Textures;

namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Represents a loaded font with its GPU texture atlas and glyph metrics.
/// Managed by IFontResourceManager - do not create directly.
/// </summary>
public class FontResource
{
    /// <summary>
    /// The GPU texture containing the font atlas.
    /// </summary>
    public TextureResource AtlasTexture { get; }

    /// <summary>
    /// Dictionary mapping characters to their glyph information.
    /// </summary>
    public IReadOnlyDictionary<char, GlyphInfo> Glyphs { get; }

    /// <summary>
    /// Distance between baselines of consecutive lines of text.
    /// </summary>
    public int LineHeight { get; }

    /// <summary>
    /// Distance from baseline to the highest point of any glyph.
    /// </summary>
    public int Ascender { get; }

    /// <summary>
    /// Distance from baseline to the lowest point of any glyph (typically negative).
    /// </summary>
    public int Descender { get; }

    /// <summary>
    /// Font size in pixels used for rasterization.
    /// </summary>
    public int FontSize { get; }

    /// <summary>
    /// Creates a new font resource.
    /// </summary>
    /// <param name="atlasTexture">The GPU texture containing the font atlas.</param>
    /// <param name="glyphs">Dictionary mapping characters to their glyph information.</param>
    /// <param name="lineHeight">Distance between baselines of consecutive lines.</param>
    /// <param name="ascender">Distance from baseline to highest point.</param>
    /// <param name="descender">Distance from baseline to lowest point.</param>
    /// <param name="fontSize">Font size in pixels.</param>
    public FontResource(
        TextureResource atlasTexture,
        Dictionary<char, GlyphInfo> glyphs,
        int lineHeight,
        int ascender,
        int descender,
        int fontSize)
    {
        AtlasTexture = atlasTexture;
        Glyphs = glyphs;
        LineHeight = lineHeight;
        Ascender = ascender;
        Descender = descender;
        FontSize = fontSize;
    }
}
