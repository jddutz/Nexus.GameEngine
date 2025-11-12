namespace Nexus.GameEngine.GUI;

/// <summary>
/// Alignment constants for UI element positioning and text alignment.
/// Provides both individual axis values (-1 to 1) and common 2D alignment presets.
/// </summary>
public static class Align
{
    // Horizontal alignment values
    public const float Left = -1f;
    public const float Center = 0f;
    public const float Right = 1f;

    // Vertical alignment values
    public const float Top = -1f;
    public const float Middle = 0f;
    public const float Bottom = 1f;

    // Common 2D alignment presets (9 standard positions)
    public static readonly Vector2D<float> TopLeft = new(Left, Top);
    public static readonly Vector2D<float> TopCenter = new(Center, Top);
    public static readonly Vector2D<float> TopRight = new(Right, Top);
    
    public static readonly Vector2D<float> MiddleLeft = new(Left, Middle);
    public static readonly Vector2D<float> MiddleCenter = new(Center, Middle);
    public static readonly Vector2D<float> MiddleRight = new(Right, Middle);
    
    public static readonly Vector2D<float> BottomLeft = new(Left, Bottom);
    public static readonly Vector2D<float> BottomCenter = new(Center, Bottom);
    public static readonly Vector2D<float> BottomRight = new(Right, Bottom);

    /// <summary>
    /// Parses alignment string into a 2D vector.
    /// Supports formats like "TopLeft", "MiddleCenter", "BottomRight", etc.
    /// Also supports single-axis values like "Left", "Center", "Top", "Middle", "Bottom".
    /// </summary>
    public static bool TryParse(string? name, out Vector2D<float> value)
    {
        value = TopLeft;
        if (string.IsNullOrEmpty(name)) return false;

        var normalized = name.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");

        // Try 2D alignment presets first
        switch (normalized)
        {
            case "topleft": value = TopLeft; return true;
            case "topcenter": case "topmiddle": value = TopCenter; return true;
            case "topright": value = TopRight; return true;
            
            case "middleleft": case "centerleft": value = MiddleLeft; return true;
            case "middlecenter": case "centercenter": case "center": value = MiddleCenter; return true;
            case "middleright": case "centerright": value = MiddleRight; return true;
            
            case "bottomleft": value = BottomLeft; return true;
            case "bottomcenter": case "bottommiddle": value = BottomCenter; return true;
            case "bottomright": value = BottomRight; return true;
        }

        // Try single-axis values (horizontal only, vertical stays at default)
        switch (normalized)
        {
            case "left": value = new(Left, Top); return true;
            case "right": value = new(Right, Top); return true;
        }

        // Try single-axis values (vertical only, horizontal stays at default)
        switch (normalized)
        {
            case "top": value = new(Left, Top); return true;
            case "middle": value = new(Left, Middle); return true;
            case "bottom": value = new(Left, Bottom); return true;
        }

        return false;
    }

    /// <summary>
    /// Parses alignment string into a 2D vector.
    /// Throws ArgumentException if the string is not recognized.
    /// </summary>
    public static Vector2D<float> Parse(string? name)
    {
        if (TryParse(name, out var value)) return value;
        throw new ArgumentException($"Unknown alignment name: {name}");
    }
}
