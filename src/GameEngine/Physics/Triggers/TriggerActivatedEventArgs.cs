namespace Nexus.GameEngine.Physics.Triggers;

/// <summary>
/// Provides data for trigger activation events.
/// </summary>
public class TriggerActivatedEventArgs(ITrigger trigger, IReadOnlyList<TriggerContact> objectsInTrigger,
                                TriggerContact? activatingObject, bool isActivation) : EventArgs
{
    /// <summary>
    /// Gets the trigger that was activated.
    /// </summary>
    public ITrigger Trigger { get; } = trigger;

    /// <summary>
    /// Gets all objects currently in the trigger when activated.
    /// </summary>
    public IReadOnlyList<TriggerContact> ObjectsInTrigger { get; } = objectsInTrigger;

    /// <summary>
    /// Gets the object that caused the activation (if applicable).
    /// </summary>
    public TriggerContact? ActivatingObject { get; } = activatingObject;

    /// <summary>
    /// Gets whether this is an activation (true) or deactivation (false).
    /// </summary>
    public bool IsActivation { get; } = isActivation;

    /// <summary>
    /// Gets the timestamp of the activation event.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}