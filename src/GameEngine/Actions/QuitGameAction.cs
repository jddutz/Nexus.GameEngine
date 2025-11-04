namespace Nexus.GameEngine.Actions;

/// <summary>
/// Action to quit the game application
/// </summary>
public class QuitGameAction(IWindowService windowService) : IAction
{
    private readonly IWindowService _windowService = windowService;

    /// <summary>
    /// Gets the unique ActionId for this action type.
    /// Use this in templates and bindings to reference this action.
    /// </summary>
    /// <returns>ActionId for QuitGameAction</returns>
    public static ActionId GetActionId() => ActionId.FromType<QuitGameAction>();

    public Task<ActionResult> ExecuteAsync(IRuntimeComponent? context = null)
    {
        try
        {
            _windowService.GetWindow().Close();
            return Task.FromResult(ActionResult.Successful(null, "Application quit requested"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.Failed(ex));
        }
    }
}
