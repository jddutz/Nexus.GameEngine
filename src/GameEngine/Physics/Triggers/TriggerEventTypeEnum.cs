namespace Nexus.GameEngine.Physics.Triggers;

/// <summary>
/// Specifies the type of trigger event.
/// </summary>
public enum TriggerEventTypeEnum
{
    /// <summary>
    /// Object entered the trigger area.
    /// </summary>
    Enter,

    /// <summary>
    /// Object is staying in the trigger area.
    /// </summary>
    Stay,

    /// <summary>
    /// Object exited the trigger area.
    /// </summary>
    Exit
}