using System.Numerics;

using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;

using Silk.NET.Input;

namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Runtime component that binds mouse input events to a specific action type.
/// The action is injected via constructor, eliminating the need for service provider dependencies.
/// When activated, subscribes directly to mouse events and executes the injected action
/// when the specified mouse input occurs.
/// </summary>
/// <typeparam name="TAction">The type of action to execute when the mouse binding is triggered</typeparam>
/// <remarks>
/// Initializes a new instance of the MouseBinding class with dependency injection.
/// </remarks>
/// <param name="windowService">The window service to use for getting input context</param>
/// <param name="actionFactory">The action factory for creating and executing actions</param>
public class MouseBinding<TAction>(
    IWindowService windowService,
    IActionFactory actionFactory)
    : InputBinding(windowService, actionFactory)
{

    /// <summary>
    /// Template for configuring mouse input bindings.
    /// </summary>
    public new record Template : InputBinding.Template
    {
        /// <summary>
        /// The mouse button that triggers this binding.
        /// </summary>
        public MouseButton Button { get; init; } = MouseButton.Left;

        /// <summary>
        /// The type of mouse event to listen for.
        /// </summary>
        public MouseEventType EventType { get; init; } = MouseEventType.ButtonDown;

        /// <summary>
        /// Optional modifier keys required for this binding.
        /// </summary>
        public Key[] ModifierKeys { get; init; } = [];

        /// <summary>
        /// The action type to execute when the mouse input is triggered.
        /// </summary>
        public Type ActionType { get; init; } = typeof(object);
    }

    private MouseButton _button = MouseButton.Left;
    private MouseEventType _eventType = MouseEventType.ButtonDown;
    private Key[] _modifierKeys = [];

    /// <summary>
    /// The mouse button that triggers this binding.
    /// </summary>
    public MouseButton Button
    {
        get => _button;
        set => _button = value;
    }

    /// <summary>
    /// The type of mouse event to listen for.
    /// </summary>
    public MouseEventType EventType
    {
        get => _eventType;
        set => _eventType = value;
    }

    /// <summary>
    /// Optional modifier keys required for this binding.
    /// </summary>
    public Key[] ModifierKeys
    {
        get => _modifierKeys;
        set => _modifierKeys = value ?? [];
    }

    /// <summary>
    /// Configure the mouse binding using the provided template.
    /// </summary>
    /// <param name="template">Template containing configuration data</param>
    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            Button = template.Button;
            EventType = template.EventType;
            ModifierKeys = template.ModifierKeys;
        }
    }

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
