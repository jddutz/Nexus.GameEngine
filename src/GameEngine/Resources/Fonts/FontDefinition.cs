namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Defines a font resource for atlas generation and rendering.
/// This is a recipe that tells FontResourceManager how to create the font resource.
/// </summary>
public record FontDefinition
{
    /// <summary>
    /// Unique name for this font resource.
    /// Used for caching and identification.
    /// Defaults to "DefaultFont".
    /// </summary>
    public string Name { get; init; } = "DefaultFont";
    
    /// <summary>
    /// Source that loads the raw font file data.
    /// Defaults to embedded Roboto Regular font.
    /// </summary>
    public IFontSource Source { get; init; } = new EmbeddedTrueTypeFontSource(
        "EmbeddedResources/Fonts/Roboto-Regular.ttf",
        typeof(FontDefinition).Assembly);

    /// <summary>
    /// Font size in pixels for atlas rasterization.
    /// Defaults to 16pt.
    /// </summary>
    public int FontSize { get; init; } = 16;

    /// <summary>
    /// Range of characters to include in the font atlas.
    /// Defaults to ASCII printable characters.
    /// </summary>
    public CharacterRange CharacterRange { get; init; } = CharacterRange.AsciiPrintable;

    /// <summary>
    /// Whether to use signed distance field rendering for scalable high-quality text.
    /// Defaults to false.
    /// </summary>
    public bool UseSignedDistanceField { get; init; } = false;
}
