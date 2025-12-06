namespace Nexus.GameEngine.Events;

/// <summary>
/// Generic event arguments for property change notifications.
/// </summary>
/// <typeparam name="T">The type of the property that changed.</typeparam>
public class PropertyChangedEventArgs<T> : EventArgs
{
    /// <summary>
    /// The value before the change.
    /// </summary>
    public T OldValue { get; }

    /// <summary>
    /// The current value after the change.
    /// </summary>
    public T NewValue { get; }

    /// <summary>
    /// Creates event arguments for a property change.
    /// </summary>
    /// <param name="oldValue">The previous value.</param>
    /// <param name="newValue">The new value.</param>
    public PropertyChangedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
