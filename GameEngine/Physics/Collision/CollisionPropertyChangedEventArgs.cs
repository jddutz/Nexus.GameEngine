namespace Nexus.GameEngine.Physics.Collision;

/// <summary>
/// Provides data for collision property changed events.
/// </summary>
public class CollisionPropertyChangedEventArgs(string propertyName, object oldValue, object newValue) : EventArgs
{
    public string PropertyName { get; } = propertyName;
    public object OldValue { get; } = oldValue;
    public object NewValue { get; } = newValue;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
