namespace Nexus.GameEngine.GUI;

/// <summary>
/// Horizontal alignment helpers exposing float constants in the range [-1,1].
/// Use these values in layout calculations where -1 = left, 0 = center, 1 = right.
/// </summary>
public static class HorizontalAlignment
{
    public const float Left = -1f;
    public const float Center = 0f;
    public const float Right = 1f;

    public static bool TryParse(string? name, out float value)
    {
        value = Left;
        if (string.IsNullOrEmpty(name)) return false;
        switch (name.Trim().ToLowerInvariant())
        {
            case "left": value = Left; return true;
            case "center": value = Center; return true;
            case "middle": value = Center; return true;
            case "right": value = Right; return true;
            default: return false;
        }
    }

    public static float Parse(string? name)
    {
        if (TryParse(name, out var v)) return v;
        throw new ArgumentException($"Unknown HorizontalAlignment name: {name}");
    }
}