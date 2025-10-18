using Silk.NET.Windowing;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;

namespace Nexus.GameEngine.Actions;

/// <summary>
/// Action to toggle between fullscreen and windowed mode using WindowService
/// </summary>
public class ToggleFullscreenAction(IWindowService windowService) : IAction
{
    private readonly IWindowService _windowService = windowService;

    /// <summary>
    /// Gets the unique ActionId for this action type.
    /// Use this in templates and bindings to reference this action.
    /// </summary>
    /// <returns>ActionId for ToggleFullscreenAction</returns>
    public static ActionId GetActionId() => ActionId.FromType<ToggleFullscreenAction>();

    public Task<ActionResult> ExecuteAsync(IRuntimeComponent? context = null)
    {
        try
        {
            _windowService.GetWindow().ToggleFullscreen();

            // Get the current fullscreen state from the window directly
            var window = _windowService.GetWindow();
            var newState = window.WindowState == WindowState.Fullscreen ? "Fullscreen" : "Windowed";
            return Task.FromResult(ActionResult.Successful(message: $"Window state changed to {newState}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.Failed(ex));
        }
    }
}
