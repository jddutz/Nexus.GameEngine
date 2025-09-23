using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;

namespace Nexus.GameEngine.Actions;

/// <summary>
/// Action to quit the game application
/// </summary>
public class QuitGameAction(IWindowService windowService, ILoggerFactory loggerFactory) : IAction
{
    private readonly IWindowService _windowService = windowService;
    private readonly ILogger _logger = loggerFactory.CreateLogger<QuitGameAction>();

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
            _logger.LogDebug("Quit game action executed - closing window");
            _windowService.Close();
            return Task.FromResult(ActionResult.Successful(null, "Application quit requested"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.Failed(ex));
        }
    }
}
