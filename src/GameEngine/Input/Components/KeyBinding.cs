namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Generic RuntimeComponent that binds a keyboard key to a specific action type.
/// The action is injected via constructor, eliminating the need for service provider dependencies.
/// When loaded, subscribes directly to window keyboard events and executes the injected action
/// when the specified key combination is pressed.
/// </summary>
/// <typeparam name="TAction">The type of action to execute when the key binding is triggered. Must implement IAction.</typeparam>
/// <remarks>
/// Initializes a new instance of the KeyBinding class.
/// </remarks>
public partial class KeyBinding()
    : InputBinding
{
    /// <summary>
    /// The key that triggers this binding
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private Key _key;

    /// <summary>
    /// Optional modifier keys required for this binding
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private Key[] _modifierKeys = [];

    /// <summary>
    /// Subscribe to the appropriate keyboard events based on configuration.
    /// </summary>
    protected override void SubscribeToInputEvents()
    {
        if (InputContext == null) return;

        foreach (var keyboard in InputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
        }
    }

    /// <summary>
    /// Unsubscribe from keyboard events.
    /// </summary>
    protected override void UnsubscribeFromInputEvents()
    {
        if (InputContext?.Keyboards == null) return;

        foreach (var keyboard in InputContext.Keyboards)
        {
            keyboard.KeyDown -= OnKeyDown;
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

    public void AddModifierKey(Key modifierKey)
    {
        SetModifierKeys(ModifierKeys.Concat([modifierKey]).ToArray());
    }

    public void RemoveModifierKey(Key modifierKey)
    {
        SetModifierKeys(ModifierKeys.Where(k => k != modifierKey).ToArray());
    }

    public void ClearModifierKeys()
    {
        SetModifierKeys([]);
    }
}