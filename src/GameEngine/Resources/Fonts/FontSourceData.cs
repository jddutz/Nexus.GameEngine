namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Raw font data returned by font sources.
/// Contains font file data needed for font atlas generation.
/// </summary>
public record FontSourceData
{
    /// <summary>
    /// Raw font file data (TTF, OTF, etc.).
    /// </summary>
    public required byte[] FontFileData { get; init; }
    
    /// <summary>
    /// Font name for identification.
    /// </summary>
    public required string FontName { get; init; }
}
