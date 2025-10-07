namespace Nexus.GameEngine.Components;

/// <summary>
/// Provides data for property animation lifecycle events.
/// </summary>
public sealed class PropertyAnimationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the name of the property being animated.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyAnimationEventArgs"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property being animated.</param>
    public PropertyAnimationEventArgs(string propertyName)
    {
        PropertyName = propertyName;
    }
}
