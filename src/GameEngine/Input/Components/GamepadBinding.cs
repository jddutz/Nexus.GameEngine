using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;

using Silk.NET.Input;

namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Runtime component that binds gamepad input events to a specific action type.
/// The action is injected via constructor, eliminating the need for service provider dependencies.
/// When activated, subscribes directly to gamepad events and executes the injected action
/// when the specified gamepad input occurs.
/// </summary>
/// <typeparam name="TAction">The type of action to execute when the gamepad binding is triggered</typeparam>
/// <remarks>
/// Initializes a new instance of the GamepadBinding class with dependency injection.
/// </remarks>
/// <param name="windowService">The window service to use for getting input context</param>
/// <param name="actionFactory">The action factory for creating and executing actions</param>
public partial class GamepadBinding(
    IWindowService windowService,
    IActionFactory actionFactory)
    : InputBinding(windowService, actionFactory)
{

    /// <summary>
    /// Template for configuring gamepad input bindings.
    /// </summary>
    public new record Template : InputBinding.Template
    {
        /// <summary>
        /// The gamepad button that triggers this binding.
        /// </summary>
        public ButtonName Button { get; init; } = ButtonName.A;

        /// <summary>
        /// The type of gamepad event to listen for.
        /// </summary>
        public GamepadEventType EventType { get; init; } = GamepadEventType.ButtonDown;

        /// <summary>
        /// For analog inputs (triggers, thumbsticks), the threshold value to trigger the action.
        /// </summary>
        public float Threshold { get; init; } = 0.5f;

        /// <summary>
        /// For thumbstick inputs, the specific thumbstick to monitor.
        /// </summary>
        public ThumbstickType ThumbstickType { get; init; } = ThumbstickType.Left;

        /// <summary>
        /// For trigger inputs, the specific trigger to monitor.
        /// </summary>
        public TriggerType TriggerType { get; init; } = TriggerType.Left;

        /// <summary>
        /// The action type to execute when the gamepad input is triggered.
        /// </summary>
        public Type ActionType { get; init; } = typeof(object);
    }

    /// <summary>
    /// The gamepad button that triggers this binding.
    /// </summary>
    [ComponentProperty]
    private ButtonName _button = ButtonName.A;

    /// <summary>
    /// The type of gamepad event to listen for.
    /// </summary>
    [ComponentProperty]
    private GamepadEventType _eventType = GamepadEventType.ButtonDown;

    /// <summary>
    /// For analog inputs, the threshold value to trigger the action.
    /// </summary>
    [ComponentProperty]
    private float _threshold = 0.5f;

    /// <summary>
    /// For thumbstick inputs, the specific thumbstick to monitor.
    /// </summary>
    [ComponentProperty]
    private ThumbstickType _thumbstickType = ThumbstickType.Left;

    /// <summary>
    /// For trigger inputs, the specific trigger to monitor.
    /// </summary>
    [ComponentProperty]
    private TriggerType _triggerType = TriggerType.Left;

    // Property change callback for Threshold to apply clamping
    partial void OnThresholdChanged(float oldValue)
    {
        // Clamp the value if it's out of range (source generator already updated _threshold)
        if (_threshold < 0f || _threshold > 1f)
        {
            _threshold = Math.Clamp(_threshold, 0f, 1f);
        }
    }

    /// <summary>
    /// Configure the gamepad binding using the provided template.
    /// </summary>
    /// <param name="template">Template containing configuration data</param>
    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            SetButton(template.Button);
            SetEventType(template.EventType);
            SetThreshold(template.Threshold);
            SetThumbstickType(template.ThumbstickType);
            SetTriggerType(template.TriggerType);
        }
    }

    /// <summary>
    /// Subscribe to the appropriate gamepad events based on configuration.
    /// </summary>
    protected override void SubscribeToInputEvents()
    {
        if (InputContext == null) return;

        foreach (var gamepad in InputContext.Gamepads)
        {
            switch (EventType)
            {
                case GamepadEventType.ButtonDown:
                    gamepad.ButtonDown += OnButtonDown;
                    break;
                case GamepadEventType.ButtonUp:
                    gamepad.ButtonUp += OnButtonUp;
                    break;
                case GamepadEventType.ThumbstickMoved:
                    gamepad.ThumbstickMoved += OnThumbstickMoved;
                    break;
                case GamepadEventType.TriggerPressed:
                    gamepad.TriggerMoved += OnTriggerMoved;
                    break;
            }
        }
    }

    /// <summary>
    /// Unsubscribe from gamepad events.
    /// </summary>
    protected override void UnsubscribeFromInputEvents()
    {
        if (InputContext == null) return;

        foreach (var gamepad in InputContext.Gamepads)
        {
            gamepad.ButtonDown -= OnButtonDown;
            gamepad.ButtonUp -= OnButtonUp;
            gamepad.ThumbstickMoved -= OnThumbstickMoved;
            gamepad.TriggerMoved -= OnTriggerMoved;
        }
    }

    /// <summary>
    /// Handle gamepad button down events.
    /// </summary>
    private async void OnButtonDown(IGamepad gamepad, Button button)
    {
        if (button.Name == Button)
        {
            await ExecuteActionAsync($"Gamepad button {button.Name} down");
        }
    }

    /// <summary>
    /// Handle gamepad button up events.
    /// </summary>
    private async void OnButtonUp(IGamepad gamepad, Button button)
    {
        if (button.Name == Button)
        {
            await ExecuteActionAsync($"Gamepad button {button.Name} up");
        }
    }

    /// <summary>
    /// Handle thumbstick movement events.
    /// </summary>
    private async void OnThumbstickMoved(IGamepad gamepad, Thumbstick thumbstick)
    {
        bool isTargetThumbstick = (ThumbstickType == ThumbstickType.Left && thumbstick.Index == 0) ||
                                  (ThumbstickType == ThumbstickType.Right && thumbstick.Index == 1);

        if (isTargetThumbstick)
        {
            var magnitude = Math.Sqrt(thumbstick.X * thumbstick.X + thumbstick.Y * thumbstick.Y);
            if (magnitude >= Threshold)
            {
                await ExecuteActionAsync($"Gamepad {ThumbstickType} thumbstick moved (magnitude: {magnitude:F2})");
            }
        }
    }

    /// <summary>
    /// Handle trigger movement events.
    /// </summary>
    private async void OnTriggerMoved(IGamepad gamepad, Trigger trigger)
    {
        bool isTargetTrigger = (TriggerType == TriggerType.Left && trigger.Index == 0) ||
                               (TriggerType == TriggerType.Right && trigger.Index == 1);

        if (isTargetTrigger && trigger.Position >= Threshold)
        {
            await ExecuteActionAsync($"Gamepad {TriggerType} trigger pressed (value: {trigger.Position:F2})");
        }
    }

    /// <summary>
    /// Validate the gamepad binding configuration.
    /// </summary>
    protected override IEnumerable<ValidationError> OnValidate()
    {
        foreach (var error in base.OnValidate()) yield return error;

        if (Threshold < 0f || Threshold > 1f)
        {
            yield return new ValidationError(
                this,
                "Threshold must be between 0.0 and 1.0",
                ValidationSeverityEnum.Error
            );
        }
    }
}
