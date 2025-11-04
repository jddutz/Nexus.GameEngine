namespace Nexus.GameEngine.Actions;

/// <summary>
/// Action factory that resolves actions from DI and executes them with proper error handling.
/// Designed for high-performance scenarios with fast ActionId-to-Type resolution.
/// </summary>
public class ActionFactory(IServiceProvider serviceProvider) : IActionFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary>
    /// Executes an action by resolving it from DI and calling its ExecuteAsync method.
    /// </summary>
    /// <param name="actionId">The ActionId containing the action type to execute</param>
    /// <param name="context">The runtime component context for the action</param>
    /// <returns>ActionResult indicating success/failure and any data</returns>
    public async Task<ActionResult> ExecuteAsync(ActionId actionId, IRuntimeComponent context)
    {
        // Handle None/empty ActionId
        if (actionId == ActionId.None || actionId.ActionType == null)
        {
            return ActionResult.Failed("ActionId is None or invalid");
        }

        try
        {
            // Resolve action instance from DI container
            var action = _serviceProvider.GetService(actionId.ActionType) as IAction;

            if (action == null)
            {
                var errorMessage = $"Action type '{actionId.ActionType.Name}' is not registered in the service container";
                return ActionResult.Failed(errorMessage);
            }

            // Execute the action with the provided context
            var result = await action.ExecuteAsync(context);

            return result;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error executing action '{actionId.ActionType.Name}': {ex.Message}";
            return ActionResult.Failed(ex);
        }
    }
}
