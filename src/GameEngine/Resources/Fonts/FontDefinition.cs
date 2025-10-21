using Nexus.GameEngine.Resources.Fonts;

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
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Source that loads the raw font file data.
    /// </summary>
    public required IFontSource Source { get; init; }

    /// <summary>
    /// Font size in pixels for atlas rasterization.
    /// </summary>
    public required int FontSize { get; init; }

    /// <summary>
    /// Range of characters to include in the font atlas.
    /// </summary>
    public required CharacterRange CharacterRange { get; init; }

    /// <summary>
    /// Whether to use signed distance field rendering for scalable high-quality text.
    /// </summary>
    public required bool UseSignedDistanceField { get; init; }
}
