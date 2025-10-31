namespace Nexus.GameEngine.Actions;

/// <summary>
/// Interface for executable actions in the game.
/// Actions are discovered via reflection and executed by components or input handlers.
/// 
/// Convention: All action implementations should provide a static GetActionId() method
/// that returns their unique ActionId for use in templates and bindings.
/// Example: public static ActionId GetActionId() => ActionId.FromType&lt;MyAction&gt;();
/// </summary>
public interface IAction
{
    /// <summary>
    /// Executes the action with the provided component context.
    /// </summary>
    /// <param name="context">The component that triggered this action. 
    /// Can be used to navigate the component tree or access component-specific data.</param>
    /// <returns>Result indicating success/failure and any additional data</returns>
    Task<ActionResult> ExecuteAsync(IRuntimeComponent? context = null);
}