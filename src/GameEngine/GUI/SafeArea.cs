namespace Nexus.GameEngine.GUI;

/// <summary>
/// Defines safe-area margins as percentages of viewport dimensions.
/// Used to avoid content in unsafe areas (notches, rounded corners, overscan, etc.).
/// </summary>
public readonly struct SafeArea
{
    public float LeftPercent { get; init; }
    public float TopPercent { get; init; }
    public float RightPercent { get; init; }
    public float BottomPercent { get; init; }

    public int MinPixels { get; init; }
    public int MaxPixels { get; init; }

    public SafeArea(float allPercent, int minPixels = 20, int maxPixels = 100)
    {
        LeftPercent = allPercent;
        TopPercent = allPercent;
        RightPercent = allPercent;
        BottomPercent = allPercent;
        MinPixels = minPixels;
        MaxPixels = maxPixels;
    }

    public SafeArea(float horizontalPercent, float verticalPercent, int minPixels = 20, int maxPixels = 100)
    {
        LeftPercent = horizontalPercent;
        RightPercent = horizontalPercent;
        TopPercent = verticalPercent;
        BottomPercent = verticalPercent;
        MinPixels = minPixels;
        MaxPixels = maxPixels;
    }

    public SafeArea(float leftPercent, float topPercent, float rightPercent, float bottomPercent, int minPixels = 20, int maxPixels = 100)
    {
        LeftPercent = leftPercent;
        TopPercent = topPercent;
        RightPercent = rightPercent;
        BottomPercent = bottomPercent;
        MinPixels = minPixels;
        MaxPixels = maxPixels;
    }

    /// <summary>
    /// Calculates safe-area margins in pixels for the given viewport size.
    /// </summary>
    public Padding CalculateMargins(Vector2D<int> viewportSize)
    {
        int left = Clamp((int)(viewportSize.X * LeftPercent), MinPixels, MaxPixels);
        int top = Clamp((int)(viewportSize.Y * TopPercent), MinPixels, MaxPixels);
        int right = Clamp((int)(viewportSize.X * RightPercent), MinPixels, MaxPixels);
        int bottom = Clamp((int)(viewportSize.Y * BottomPercent), MinPixels, MaxPixels);
        
        return new Padding(left, top, right, bottom);
    }

    private static int Clamp(int value, int min, int max) =>
        Math.Max(min, Math.Min(max, value));

    public static SafeArea Zero => new(0, 0, 0, 0, 0, 0);
}
