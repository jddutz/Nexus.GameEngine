namespace Nexus.GameEngine.GUI;

/// <summary>
/// Defines the visual styling for text rendering.
/// Encapsulates font configuration and text appearance properties.
/// </summary>
public record TextStyle
{
    /// <summary>
    /// The font definition specifying which font to use, size, and character range.
    /// Defaults to a 16pt Roboto Regular font.
    /// </summary>
    public FontDefinition Font { get; init; } = new();
    
    /// <summary>
    /// The color of the text (RGBA).
    /// Default is white (1, 1, 1, 1).
    /// </summary>
    public Vector4D<float> Color { get; init; } = new(1, 1, 1, 1);
    
    /// <summary>
    /// Text alignment within the text element bounds.
    /// Default is Left.
    /// </summary>
    public TextAlignment Alignment { get; init; } = TextAlignment.Left;
    
    /// <summary>
    /// Creates a text style with default white color and left alignment.
    /// </summary>
    /// <param name="font">The font definition to use</param>
    public static TextStyle Default(FontDefinition font) => new()
    {
        Font = font,
        Color = new Vector4D<float>(1, 1, 1, 1),
        Alignment = TextAlignment.Left
    };
    
    /// <summary>
    /// Creates a text style with specified color.
    /// </summary>
    /// <param name="font">The font definition to use</param>
    /// <param name="color">The text color</param>
    public static TextStyle WithColor(FontDefinition font, Vector4D<float> color) => new()
    {
        Font = font,
        Color = color,
        Alignment = TextAlignment.Left
    };
    
    /// <summary>
    /// Creates a text style with specified alignment.
    /// </summary>
    /// <param name="font">The font definition to use</param>
    /// <param name="alignment">The text alignment</param>
    public static TextStyle WithAlignment(FontDefinition font, TextAlignment alignment) => new()
    {
        Font = font,
        Color = new Vector4D<float>(1, 1, 1, 1),
        Alignment = alignment
    };
}
