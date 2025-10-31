namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Abstract base class for input binding components that provides shared behavior
/// for binding input events to actions. Handles common functionality like
/// action execution, error handling, and lifecycle management.
/// Uses lazy initialization to obtain input context from window service when ready.
/// </summary>
/// <remarks>
/// Initializes a new instance of the InputBinding class with dependency injection.
/// The input context is obtained lazily from the window service to ensure proper
/// initialization order during component lifecycle.
/// </remarks>
/// <param name="windowService">The window service used to obtain input context when ready</param>
/// <param name="actionFactory">The action factory for creating and executing actions</param>
public abstract partial class InputBinding(
    IWindowService windowService,
    IActionFactory actionFactory)
    : RuntimeComponent
{

    public new record Template : RuntimeComponent.Template
    {
        public ActionId ActionId { get; set; } = ActionId.None;
    }
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

            try
            {
                _inputContext = windowService.InputContext;
                return _inputContext;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("view was not initialized"))
            {
                // Window view not ready yet - this is expected during early component lifecycle
                Log.Debug("Input context not available yet, window view not initialized");
                return null;
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Failed to get input context from window service");
                return null;
            }
        }
    }

    protected readonly IActionFactory _actionFactory = actionFactory;

    /// <summary>
    /// Gets the action that will be executed when input is triggered.
    /// </summary>
    [ComponentProperty]
    private ActionId _actionId = ActionId.None;

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        base.OnLoad(componentTemplate);

        if (componentTemplate is Template template)
        {
            SetActionId(template.ActionId);
        }
    }

    /// <summary>
    /// Validate the input binding configuration. Override to add specific validation logic.
    /// </summary>
    /// <returns>Collection of validation errors</returns>
    protected override IEnumerable<ValidationError> OnValidate()
    {
        if (_actionFactory == null)
        {
            yield return new ValidationError(
                this,
                "ActionFactory is required for input binding",
                ValidationSeverityEnum.Error
            );
        }

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
        try
        {
            if (InputContext == null)
            {
                Log.Debug($"Input context not available, cannot activate {GetType().Name}");
                return;
            }

            SubscribeToInputEvents();
            Log.Debug($"{GetType().Name} activated and listening for input");
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, "Failed to activate input binding {ComponentType}, input context may not be ready yet", GetType().Name);
        }
    }

    /// <summary>
    /// Execute the bound action safely with error handling and debugging output.
    /// </summary>
    /// <param name="inputDescription">Description of the input event for debugging</param>
    /// <returns>Task representing the async action execution</returns>
    protected async Task ExecuteActionAsync(string inputDescription)
    {
        try
        {
            if (!IsActive())
                return;

            if (ActionId == ActionId.None)
            {
                Log.Debug($"No action configured for {inputDescription} on {GetType().Name}");
                return;
            }

            if (_actionFactory == null)
            {
                Log.Debug($"ActionFactory not available for {inputDescription} on {GetType().Name}");
                return;
            }

            Log.Debug($"Executing action for {inputDescription} - ActionId: {ActionId.Identifier}");

            var result = await _actionFactory.ExecuteAsync(ActionId, this);

            if (result.Success)
            {
                Log.Debug($"Action executed successfully for {inputDescription}: {result.Message ?? string.Empty}");
            }
            else
            {
                Log.Debug($"Action execution failed for {inputDescription}: {result.Message ?? string.Empty}");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error executing action for {InputDescription}", inputDescription);
        }
    }

    /// <summary>
    /// Called when the input binding is deactivated. Override to unsubscribe from input events.
    /// </summary>
    protected override void OnDeactivate()
    {
        UnsubscribeFromInputEvents();
        Log.Debug($"{GetType().Name} deactivated");
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
