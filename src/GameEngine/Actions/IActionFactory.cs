using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Actions;

/// <summary>
/// Factory for creating and executing actions with dependency injection
/// </summary>
public interface IActionFactory
{
    Task<ActionResult> ExecuteAsync(ActionId actionId, IRuntimeComponent context);
}