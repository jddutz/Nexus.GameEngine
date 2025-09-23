namespace Nexus.GameEngine.Physics.Triggers;

/// <summary>
/// Provides data for trigger events.
/// </summary>
public class TriggerEventArgs(TriggerContact contact, ITrigger trigger, TriggerEventTypeEnum eventType) : EventArgs
{
    /// <summary>
    /// Gets the trigger contact information.
    /// </summary>
    public TriggerContact Contact { get; } = contact;

    /// <summary>
    /// Gets the trigger that detected the object.
    /// </summary>
    public ITrigger Trigger { get; } = trigger;

    /// <summary>
    /// Gets the event type (enter, stay, or exit).
    /// </summary>
    public TriggerEventTypeEnum EventType { get; } = eventType;

    /// <summary>
    /// Gets or sets whether the trigger event should be ignored.
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// Gets the timestamp of the trigger event.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}