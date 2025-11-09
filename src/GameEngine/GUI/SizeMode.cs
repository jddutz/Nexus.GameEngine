namespace Nexus.GameEngine.GUI;

/// <summary>
/// Defines how an element determines its size.
/// </summary>
public enum SizeMode
{
    /// <summary>
    /// Use explicit Width/Height properties.
    /// </summary>
    Fixed,

    /// <summary>
    /// Size determined by content (text, texture, children).
    /// </summary>
    Intrinsic,

    /// <summary>
    /// Fill available space from parent constraints.
    /// </summary>
    Stretch,

    /// <summary>
    /// Size as percentage of parent dimensions.
    /// </summary>
    Percentage
}
