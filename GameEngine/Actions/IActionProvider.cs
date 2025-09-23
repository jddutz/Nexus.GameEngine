namespace Nexus.GameEngine.Actions;

/// <summary>
/// Interface for providing access to game actions in the application
/// </summary>
public interface IActionProvider
{
    /// <summary>
    /// Gets all discovered actions.
    /// </summary>
    IEnumerable<ActionInfo> GetAllActions();

    /// <summary>
    /// Gets an action by name.
    /// </summary>
    /// <param name="actionName">The name of the action</param>
    /// <returns>The action info if found, null otherwise</returns>
    ActionInfo? GetActionInfo(string actionName);

    /// <summary>
    /// Gets actions by category.
    /// </summary>
    /// <param name="category">The category to filter by</param>
    /// <returns>Actions in the specified category</returns>
    IEnumerable<ActionInfo> GetActionsByCategory(string category);

    /// <summary>
    /// Invalidates the cached actions and forces re-discovery on next access
    /// This should be called when assemblies are loaded/unloaded or action definitions change
    /// </summary>
    void Invalidate();

    /// <summary>
    /// Forces immediate re-discovery of actions
    /// </summary>
    void Refresh();
}