namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Default font definitions for embedded font resources.
/// These are static instances of FontDefinition that can be used throughout the application.
/// </summary>
public static class FontDefinitions
{
    /// <summary>
    /// Roboto Bold 32pt - suitable for titles and headings with heavier weight.
    /// Uses a bolder font face for better pixel coverage in tests.
    /// </summary>
    public static readonly FontDefinition RobotoTitle = new()
    {
        Name = "Roboto-Bold-32pt",
        Source = new EmbeddedTrueTypeFontSource(
            "EmbeddedResources/Fonts/Roboto-Bold.ttf",
            typeof(FontDefinitions).Assembly),
        FontSize = 32,
        CharacterRange = CharacterRange.AsciiPrintable,
        UseSignedDistanceField = false
    };
    
    /// <summary>
    /// Roboto Regular 16pt - suitable for body text and normal UI elements.
    /// </summary>
    public static readonly FontDefinition RobotoNormal = new()
    {
        Name = "Roboto-Regular-16pt",
        Source = new EmbeddedTrueTypeFontSource(
            "EmbeddedResources/Fonts/Roboto-Regular.ttf",
            typeof(FontDefinitions).Assembly),
        FontSize = 16,
        CharacterRange = CharacterRange.AsciiPrintable,
        UseSignedDistanceField = false
    };
    
    /// <summary>
    /// Roboto Regular 12pt - suitable for captions and small text.
    /// </summary>
    public static readonly FontDefinition RobotoCaption = new()
    {
        Name = "Roboto-Regular-12pt",
        Source = new EmbeddedTrueTypeFontSource(
            "EmbeddedResources/Fonts/Roboto-Regular.ttf",
            typeof(FontDefinitions).Assembly),
        FontSize = 12,
        CharacterRange = CharacterRange.AsciiPrintable,
        UseSignedDistanceField = false
    };
}
