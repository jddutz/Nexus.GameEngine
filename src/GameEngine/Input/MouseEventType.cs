namespace Nexus.GameEngine.Input;


/// <summary>
/// Types of mouse events that can be bound to actions.
/// </summary>
public enum MouseEventType
{
    /// <summary>
    /// Mouse button pressed down.
    /// </summary>
    ButtonDown,

    /// <summary>
    /// Mouse button released.
    /// </summary>
    ButtonUp,

    /// <summary>
    /// Mouse moved.
    /// </summary>
    Move,

    /// <summary>
    /// Mouse wheel scrolled.
    /// </summary>
    Scroll
}