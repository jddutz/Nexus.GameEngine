namespace Nexus.GameEngine.GUI;

/// <summary>
/// Vertical alignment helpers exposing float constants in the range [-1,1].
/// Use these values in layout calculations where -1 = top, 0 = center, 1 = bottom.
/// </summary>
public static class VerticalAlignment
{
    public const float Top = -1f;
    public const float Center = 0f;
    public const float Bottom = 1f;

    public static bool TryParse(string? name, out float value)
    {
        value = Top;
        if (string.IsNullOrEmpty(name)) return false;
        switch (name.Trim().ToLowerInvariant())
        {
            case "top": value = Top; return true;
            case "center": value = Center; return true;
            case "middle": value = Center; return true;
            case "bottom": value = Bottom; return true;
            default: return false;
        }
    }

    public static float Parse(string? name)
    {
        if (TryParse(name, out var v)) return v;
        throw new ArgumentException($"Unknown VerticalAlignment name: {name}");
    }
}