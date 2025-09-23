using Microsoft.Extensions.Logging;

using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;

using Silk.NET.Input;

namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Generic RuntimeComponent that binds a keyboard key to a specific action type.
/// The action is injected via constructor, eliminating the need for service provider dependencies.
/// When loaded, subscribes directly to window keyboard events and executes the injected action
/// when the specified key combination is pressed.
/// </summary>
/// <typeparam name="TAction">The type of action to execute when the key binding is triggered. Must implement IAction.</typeparam>
/// <remarks>
/// Initializes a new instance of the KeyBinding class with dependency injection.
/// </remarks>
/// <param name="input">The input context to use for registering keyboard events</param>
/// <param name="action">The action instance to execute when the key is pressed. This action is injected and ready to use.</param>
/// <param name="logger">Logger for debugging and diagnostics</param>
public class KeyBinding(
    IInputContext inputContext,
    IActionFactory actionFactory)
    : InputBinding(inputContext, actionFactory)
{
    public new record Template : InputBinding.Template
    {
        /// <summary>
        /// The key that triggers this binding
        /// </summary>
        public Key Key { get; set; }

        /// <summary>
        /// Optional modifier keys required for this binding
        /// </summary>
        public Key[] ModifierKeys { get; set; } = [];
    }

    /// <summary>
    /// The key that triggers this binding
    /// </summary>
    public Key Key { get; set; }

    /// <summary>
    /// Optional modifier keys required for this binding
    /// </summary>
    public Key[] ModifierKeys { get; set; } = [];

    /// <summary>
    /// Subscribe to the appropriate keyboard events based on configuration.
    /// </summary>
    protected override void SubscribeToInputEvents()
    {
        if (_inputContext == null) return;

        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyChar += OnKeyChar;
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }
    }

    /// <summary>
    /// Configure the gamepad binding using the provided template.
    /// </summary>
    /// <param name="template">Template containing configuration data</param>
    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            Key = template.Key;
            ModifierKeys = template.ModifierKeys;
        }
    }

    /// <summary>
    /// Unsubscribe from keyboard events.
    /// </summary>
    protected override void UnsubscribeFromInputEvents()
    {
        if (_inputContext?.Keyboards == null) return;

        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyChar -= OnKeyChar;
            keyboard.KeyDown -= OnKeyDown;
            keyboard.KeyUp -= OnKeyUp;
        }
    }

    /// <summary>
    /// Activate the key binding by subscribing to input events.
    /// </summary>
    protected override void OnActivate()
    {
        try
        {
            if (_inputContext == null)
            {
                Logger?.LogDebug("Input context not available, cannot activate KeyBinding");
                return;
            }

            // Subscribe to keyboard events
            foreach (var keyboard in _inputContext.Keyboards)
            {
                keyboard.KeyDown += OnKeyDown;
            }

            Logger?.LogDebug($"KeyBinding for {Key} (modifiers: {string.Join(",", ModifierKeys)}) activated and listening");
        }
        catch (Exception ex)
        {
            Logger?.LogDebug($"Error activating KeyBinding: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate the KeyBinding configuration.
    /// </summary>
    protected override IEnumerable<ValidationError> OnValidate()
    {
        // Get base validation from InputBinding
        foreach (var error in base.OnValidate())
            yield return error;
    }

    private void OnKeyChar(IKeyboard keyboard, char arg2)
    {
        throw new NotImplementedException();
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int arg3)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Handle the key down event directly from Silk.NET.
    /// </summary>
    private void OnKeyDown(IKeyboard keyboard, Key key, int scanCode)
    {
        // Check if this is the key we're looking for
        if (key != Key) return;

        // Check modifier keys if specified
        if (ModifierKeys.Length > 0)
        {
            if (!AreModifierKeysPressed(keyboard))
                return;
        }

        // Fire and forget the action execution
        _ = ExecuteActionAsync($"{key} pressed");
    }

    /// <summary>
    /// Check if all required modifier keys are currently pressed.
    /// </summary>
    private bool AreModifierKeysPressed(IKeyboard keyboard)
    {
        foreach (var modifierKey in ModifierKeys)
        {
            if (!keyboard.IsKeyPressed(modifierKey))
                return false;
        }
        return true;
    }
}