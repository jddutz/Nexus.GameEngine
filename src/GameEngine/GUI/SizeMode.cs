namespace Nexus.GameEngine.GUI;

/// <summary>
/// Defines how an element determines its size.
/// </summary>
public enum SizeMode
{
    /// <summary>
    /// Use explicit FixedSize property, ignoring container constraints, content, and padding.
    /// Formula: FixedSize
    /// Example: FixedSize = (100, 50) → element is always 100×50 pixels
    /// </summary>
    Fixed,

    /// <summary>
    /// Size determined by content (text, texture, children) plus padding.
    /// Formula: CalculateContentSize() + padding
    /// Example: Text "Hello" with Padding = 10 → text bounds + 10px on each side
    /// </summary>
    FitContent,

    /// <summary>
    /// Size as percentage of container constraints (multiplicative).
    /// Formula: containerSize × RelativeSize
    /// Example: RelativeSize = (0.5, 0.75) → 50% container width, 75% container height
    /// </summary>
    Relative,

    /// <summary>
    /// Size relative to container with pixel offset (additive).
    /// Formula: containerSize + RelativeSize
    /// Example: RelativeSize = (-20, -10) → container width - 20px, container height - 10px
    /// Use negative values for margins, positive for overflow.
    /// </summary>
    Absolute
}
