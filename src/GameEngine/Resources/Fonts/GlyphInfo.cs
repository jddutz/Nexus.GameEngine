using Silk.NET.Maths;

namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Contains metrics and texture coordinates for a single character glyph.
/// </summary>
public record GlyphInfo
{
    /// <summary>
    /// The character this glyph represents.
    /// </summary>
    public required char Character { get; init; }

    /// <summary>
    /// Minimum texture coordinate (top-left corner) in the font atlas.
    /// </summary>
    public required Vector2D<float> TexCoordMin { get; init; }

    /// <summary>
    /// Maximum texture coordinate (bottom-right corner) in the font atlas.
    /// </summary>
    public required Vector2D<float> TexCoordMax { get; init; }

    /// <summary>
    /// Width of the glyph in pixels.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Height of the glyph in pixels.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// Horizontal offset from cursor position to left edge of glyph.
    /// </summary>
    public required int BearingX { get; init; }

    /// <summary>
    /// Vertical offset from baseline to top edge of glyph.
    /// </summary>
    public required int BearingY { get; init; }

    /// <summary>
    /// Horizontal distance to advance cursor after rendering this glyph.
    /// </summary>
    public required int Advance { get; init; }
}
