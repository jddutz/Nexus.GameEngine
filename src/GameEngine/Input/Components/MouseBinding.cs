using System.Numerics;

namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Runtime component that binds mouse input events to a specific action type.
/// The action is injected via constructor, eliminating the need for service provider dependencies.
/// When activated, subscribes directly to mouse events and executes the injected action
/// when the specified mouse input occurs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the MouseBinding class with dependency injection.
/// </remarks>
/// <param name="windowService">The window service to use for getting input context</param>
/// <param name="actionFactory">The action factory for creating and executing actions</param>
public partial class MouseBinding(
    IWindowService windowService,
    IActionFactory actionFactory)
    : InputBinding(windowService, actionFactory)
{
    /// <summary>
    /// The mouse button that triggers this binding.
    /// </summary>
    [ComponentProperty]
    private MouseButton _button = MouseButton.Left;
    
    /// <summary>
    /// The type of mouse event to listen for.
    /// </summary>
    [ComponentProperty]
    private MouseEventType _eventType = MouseEventType.ButtonDown;
    
    /// <summary>
    /// Optional modifier keys required for this binding.
    /// </summary>
    [ComponentProperty]
    private Key[] _modifierKeys = [];

    /// <summary>
    /// Subscribe to the appropriate mouse events based on configuration.
    /// </summary>
    protected override void SubscribeToInputEvents()
    {
        if (InputContext == null) return;

        foreach (var mouse in InputContext.Mice)
        {
            switch (EventType)
            {
                case MouseEventType.ButtonDown:
                    mouse.MouseDown += OnMouseDown;
                    break;
                case MouseEventType.ButtonUp:
                    mouse.MouseUp += OnMouseUp;
                    break;
                case MouseEventType.Move:
                    mouse.MouseMove += OnMouseMove;
                    break;
                case MouseEventType.Scroll:
                    mouse.Scroll += OnMouseScroll;
                    break;
            }
        }
    }

    /// <summary>
    /// Unsubscribe from mouse events.
    /// </summary>
    protected override void UnsubscribeFromInputEvents()
    {
        if (InputContext == null) return;

        foreach (var mouse in InputContext.Mice)
        {
            mouse.MouseDown -= OnMouseDown;
            mouse.MouseUp -= OnMouseUp;
            mouse.MouseMove -= OnMouseMove;
            mouse.Scroll -= OnMouseScroll;
        }
    }

    /// <summary>
    /// Handle mouse button down events.
    /// </summary>
    private async void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button == Button && AreModifierKeysPressed())
        {
            await ExecuteActionAsync($"Mouse button {button} down");
        }
    }

    /// <summary>
    /// Handle mouse button up events.
    /// </summary>
    private async void OnMouseUp(IMouse mouse, MouseButton button)
    {
        if (button == Button && AreModifierKeysPressed())
        {
            await ExecuteActionAsync($"Mouse button {button} up");
        }
    }

    /// <summary>
    /// Handle mouse move events.
    /// </summary>
    private async void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (AreModifierKeysPressed())
        {
            await ExecuteActionAsync($"Mouse move to {position}");
        }
    }

    /// <summary>
    /// Handle mouse scroll events.
    /// </summary>
    private async void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        if (AreModifierKeysPressed())
        {
            await ExecuteActionAsync($"Mouse scroll {scrollWheel.Y}");
        }
    }

    /// <summary>
    /// Check if all required modifier keys are currently pressed.
    /// </summary>
    private bool AreModifierKeysPressed()
    {
        if (InputContext == null || ModifierKeys.Length == 0)
            return true;

        foreach (var keyboard in InputContext.Keyboards)
        {
            foreach (var modifierKey in ModifierKeys)
            {
                if (!keyboard.IsKeyPressed(modifierKey))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Validate the mouse binding configuration.
    /// </summary>
    protected override IEnumerable<ValidationError> OnValidate()
    {
        var errors = base.OnValidate().ToList();

        // Mouse button validation is handled by the enum type system
        // Event type validation is handled by the enum type system

        return errors;
    }
}
