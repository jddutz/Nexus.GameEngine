using Microsoft.Extensions.Logging;

using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;
using Silk.NET.Input;

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
    : RuntimeComponent, IInputBindingController
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
                _inputContext = windowService.GetInputContext();
                return _inputContext;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("view was not initialized"))
            {
                // Window view not ready yet - this is expected during early component lifecycle
                Logger?.LogDebug("Input context not available yet, window view not initialized");
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

    // Private field for deferred updates
    private ActionId _actionId = ActionId.None;

    /// <summary>
    /// Gets the action that will be executed when input is triggered.
    /// </summary>
    public ActionId ActionId
    {
        get => _actionId;
        private set
        {
            if (_actionId != value)
            {
                _actionId = value;
                NotifyPropertyChanged();
            }
        }
    }

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            // Direct field assignment during configuration - no deferred updates needed
            _actionId = template.ActionId;
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
                Logger?.LogDebug("Input context not available, cannot activate {ComponentType}", GetType().Name);
                return;
            }

            SubscribeToInputEvents();
            Logger?.LogDebug("{ComponentType} activated and listening for input", GetType().Name);
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
            if (!IsEnabled)
                return;

            if (ActionId == ActionId.None)
            {
                Logger?.LogDebug("No action configured for {InputDescription} on {ComponentType}", inputDescription, GetType().Name);
                return;
            }

            if (_actionFactory == null)
            {
                Logger?.LogDebug("ActionFactory not available for {InputDescription} on {ComponentType}", inputDescription, GetType().Name);
                return;
            }

            Logger?.LogDebug("Executing action for {InputDescription} - ActionId: {ActionIdentifier}", inputDescription, ActionId.Identifier);

            var result = await _actionFactory.ExecuteAsync(ActionId, this);

            if (result.Success)
            {
                Logger?.LogDebug("Action executed successfully for {InputDescription}: {ResultMessage}", inputDescription, result.Message);
            }
            else
            {
                Logger?.LogDebug("Action execution failed for {InputDescription}: {ResultMessage}", inputDescription, result.Message);
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
        Logger?.LogDebug("{ComponentType} deactivated", GetType().Name);
    }

    /// <summary>
    /// Subscribe to the appropriate input events. Must be implemented by derived classes.
    /// </summary>
    protected abstract void SubscribeToInputEvents();

    /// <summary>
    /// Unsubscribe from input events. Must be implemented by derived classes.
    /// </summary>
    protected abstract void UnsubscribeFromInputEvents();

    // IInputBindingController implementation - all methods use deferred updates
    public void SetActionId(ActionId actionId)
    {
        QueueUpdate(() => ActionId = actionId);
    }
}
