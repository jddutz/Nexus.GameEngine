namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Defines the range of characters to include in a font atlas.
/// </summary>
public enum CharacterRange
{
    /// <summary>
    /// ASCII printable characters (32-126), total of 95 characters.
    /// </summary>
    AsciiPrintable,

    /// <summary>
    /// Extended ASCII characters (32-255), total of 224 characters.
    /// </summary>
    Extended,

    /// <summary>
    /// User-defined custom character range.
    /// </summary>
    Custom
}
