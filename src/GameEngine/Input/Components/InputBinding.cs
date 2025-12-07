using Nexus.GameEngine.Runtime.Extensions;

namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Abstract base class for input binding components that provides shared behavior
/// for binding input events to actions. Handles common functionality like
/// action execution, error handling, and lifecycle management.
/// Uses lazy initialization to obtain input context from window service when ready.
/// </summary>
/// <remarks>
/// Initializes a new instance of the InputBinding class.
/// The input context is obtained lazily from the input system to ensure proper
/// initialization order during component lifecycle.
/// </remarks>
public abstract partial class InputBinding()
    : Component
{
    /// <summary>
    /// Direct access to Silk.NET Input interface for component input handling.
    /// Components use this to register for input events in their Subscribe/Unsubscribe methods.
    /// Lazily initialized when first accessed, with graceful handling if window isn't ready.
    /// </summary>
    private IInputContext? _inputContext;
    public IInputContext? InputContext
    {
        get
        {
            if (_inputContext != null)
                return _inputContext;

            _inputContext = Input.GetInputContext();
            return _inputContext;
        }
    }

    /// <summary>
    /// Gets the action that will be executed when input is triggered.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected ActionId _actionId = ActionId.None;

    /// <summary>
    /// Validate the input binding configuration. Override to add specific validation logic.
    /// </summary>
    /// <returns>Collection of validation errors</returns>
    protected override IEnumerable<ValidationError> OnValidate()
    {
        if (ActionId == ActionId.None)
        {
            yield return new ValidationError(
                this,
                "ActionId is required for input binding",
                ValidationSeverityEnum.Error
            );
        }
    }

    /// <summary>
    /// Called when the input binding is activated. Override to subscribe to input events.
    /// </summary>
    protected override void OnActivate()
    {
        if (InputContext == null) return;

        SubscribeToInputEvents();
    }

    /// <summary>
    /// Execute the bound action safely with error handling and debugging output.
    /// </summary>
    /// <param name="inputDescription">Description of the input event for debugging</param>
    /// <returns>Task representing the async action execution</returns>
    protected async Task ExecuteActionAsync(string inputDescription)
    {
        if (!IsActive())
            return;

        if (ActionId == ActionId.None)
        {
            return;
        }

        var result = await Input.ExecuteActionAsync(ActionId, this);
    }

    /// <summary>
    /// Called when the input binding is deactivated. Override to unsubscribe from input events.
    /// </summary>
    protected override void OnDeactivate()
    {
        UnsubscribeFromInputEvents();
    }

    /// <summary>
    /// Subscribe to the appropriate input events. Must be implemented by derived classes.
    /// </summary>
    protected abstract void SubscribeToInputEvents();

    /// <summary>
    /// Unsubscribe from input events. Must be implemented by derived classes.
    /// </summary>
    protected abstract void UnsubscribeFromInputEvents();
}
