namespace Nexus.GameEngine.Input;

/// <summary>
/// Types of gamepad events that can be bound to actions.
/// </summary>
public enum GamepadEventType
{
    /// <summary>
    /// Gamepad button pressed down.
    /// </summary>
    ButtonDown,

    /// <summary>
    /// Gamepad button released.
    /// </summary>
    ButtonUp,

    /// <summary>
    /// Thumbstick moved beyond threshold.
    /// </summary>
    ThumbstickMoved,

    /// <summary>
    /// Trigger pressed beyond threshold.
    /// </summary>
    TriggerPressed
}