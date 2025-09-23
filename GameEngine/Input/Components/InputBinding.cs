using Microsoft.Extensions.Logging;

using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;

using Silk.NET.Input;

namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Abstract base class for input binding components that provides shared behavior
/// for binding input events to actions. Handles common functionality like
/// action execution, error handling, and lifecycle management.
/// </summary>
/// <typeparam name="TAction">The type of action to execute when input is triggered</typeparam>
/// <typeparam name="TTemplate">The template type used to configure this binding</typeparam>
/// <remarks>
/// Initializes a new instance of the InputBinding class.
/// </remarks>
/// <param name="inputContext">The input context for registering events</param>
/// <param name="action">The action to execute when input is triggered</param>
/// <param name="logger">Logger for debugging and diagnostics</param>
public abstract class InputBinding(
    IInputContext inputContext,
    IActionFactory actionFactory)
    : RuntimeComponent
{

    public new record Template : RuntimeComponent.Template
    {
        public ActionId ActionId { get; set; } = ActionId.None;
    }


    protected readonly IInputContext _inputContext = inputContext;
    protected readonly IActionFactory _actionFactory = actionFactory;

    /// <summary>
    /// Gets the action that will be executed when input is triggered.
    /// </summary>
    public ActionId ActionId { get; set; } = ActionId.None;

    /// <summary>
    /// Gets the input context used for event registration.
    /// </summary>
    public IInputContext? InputContext => _inputContext;

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            ActionId = template.ActionId;
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
        if (_inputContext == null)
        {
            Logger?.LogDebug($"Input context not available, cannot activate {GetType().Name}");
            return;
        }

        SubscribeToInputEvents();
        Logger?.LogDebug($"{GetType().Name} activated and listening for input");
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
            // Check if this is the key we're looking for
            if (!IsEnabled)
                return;

            if (ActionId == ActionId.None)
            {
                Logger?.LogDebug($"No action configured for {inputDescription} on {GetType().Name}");
                return;
            }

            if (_actionFactory == null)
            {
                Logger?.LogDebug($"ActionFactory not available for {inputDescription} on {GetType().Name}");
                return;
            }

            Logger?.LogDebug($"Executing action for {inputDescription} - ActionId: {ActionId.Identifier}");

            var result = await _actionFactory.ExecuteAsync(ActionId, this);

            if (result.Success)
            {
                Logger?.LogDebug($"Action executed successfully for {inputDescription}: {result.Message}");
            }
            else
            {
                Logger?.LogDebug($"Action execution failed for {inputDescription}: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogDebug($"Error executing action for {inputDescription}: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when the input binding is deactivated. Override to unsubscribe from input events.
    /// </summary>
    protected override void OnDeactivate()
    {
        UnsubscribeFromInputEvents();
        Logger?.LogDebug($"{GetType().Name} deactivated");
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
