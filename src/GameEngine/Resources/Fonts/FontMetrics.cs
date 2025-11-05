namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Font-level metrics for text layout and measurement.
/// </summary>
public record FontMetrics
{
    /// <summary>
    /// Distance from baseline to highest point in font (pixels, positive).
    /// </summary>
    public required int Ascender { get; init; }
    
    /// <summary>
    /// Distance from baseline to lowest point in font (pixels, negative).
    /// </summary>
    public required int Descender { get; init; }
    
    /// <summary>
    /// Recommended vertical spacing between lines (pixels).
    /// Typically: Ascender - Descender + LineGap
    /// </summary>
    public required int LineHeight { get; init; }
    
    /// <summary>
    /// Scale factor applied during glyph rasterization.
    /// </summary>
    public required float Scale { get; init; }
}
