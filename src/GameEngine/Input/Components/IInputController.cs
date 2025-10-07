using Nexus.GameEngine.Actions;
using Silk.NET.Input;

namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Interface for input mapping components that support runtime control operations.
/// Used for runtime discovery and polymorphic control of input mapping properties.
/// </summary>
public interface IInputMapController
{
    /// <summary>
    /// Sets the input mapping description. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="description">New description text</param>
    void SetDescription(string description);

    /// <summary>
    /// Sets the input mapping priority level. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="priority">New priority level (higher values have higher priority)</param>
    void SetPriority(int priority);

    /// <summary>
    /// Sets whether this input mapping should be enabled by default. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="enabledByDefault">Whether to enable by default</param>
    void SetEnabledByDefault(bool enabledByDefault);
}

/// <summary>
/// Interface for input binding components that support runtime control operations.
/// Used for runtime discovery and polymorphic control of input binding properties.
/// </summary>
public interface IInputBindingController
{
    /// <summary>
    /// Sets the action ID for this input binding. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="actionId">New action ID to bind to</param>
    void SetActionId(ActionId actionId);
}

/// <summary>
/// Interface for keyboard input binding components that support runtime control operations.
/// Used for runtime discovery and polymorphic control of key binding properties.
/// </summary>
public interface IKeyBindingController : IInputBindingController
{
    /// <summary>
    /// Sets the key that triggers this binding. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="key">New key to bind to</param>
    void SetKey(Key key);

    /// <summary>
    /// Sets the modifier keys required for this binding. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="modifierKeys">Array of modifier keys required</param>
    void SetModifierKeys(params Key[] modifierKeys);

    /// <summary>
    /// Adds a modifier key to the current set of required modifiers. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="modifierKey">Modifier key to add</param>
    void AddModifierKey(Key modifierKey);

    /// <summary>
    /// Removes a modifier key from the current set of required modifiers. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="modifierKey">Modifier key to remove</param>
    void RemoveModifierKey(Key modifierKey);

    /// <summary>
    /// Clears all modifier keys. Change is applied at next frame boundary.
    /// </summary>
    void ClearModifierKeys();
}

/// <summary>
/// Interface for mouse input binding components that support runtime control operations.
/// Used for runtime discovery and polymorphic control of mouse binding properties.
/// </summary>
public interface IMouseBindingController : IInputBindingController
{
    /// <summary>
    /// Sets the mouse button that triggers this binding. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="button">New mouse button to bind to</param>
    void SetButton(MouseButton button);

    /// <summary>
    /// Sets the type of mouse event to listen for. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="eventType">New mouse event type</param>
    void SetEventType(MouseEventType eventType);

    /// <summary>
    /// Sets the modifier keys required for this binding. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="modifierKeys">Array of modifier keys required</param>
    void SetModifierKeys(params Key[] modifierKeys);

    /// <summary>
    /// Adds a modifier key to the current set of required modifiers. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="modifierKey">Modifier key to add</param>
    void AddModifierKey(Key modifierKey);

    /// <summary>
    /// Removes a modifier key from the current set of required modifiers. Change is applied at next frame boundary.
    /// </summary>
    /// <param name="modifierKey">Modifier key to remove</param>
    void RemoveModifierKey(Key modifierKey);

    /// <summary>
    /// Clears all modifier keys. Change is applied at next frame boundary.
    /// </summary>
    void ClearModifierKeys();
}