using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Basic implementation of IGameStateManager
/// </summary>
public class GameStateManager : ComponentCollection, IGameStateManager
{
    /// <summary>
    /// Updates game logic: physics, actions, AI, and game object behaviors.
    /// Called once per frame during the update phase.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update.</param>
    public void Update(double deltaTime)
    {
        foreach (var component in GetComponents())
        {
            if (component.IsEnabled)
            {
                // TODO: Implement game state updates
                // Update physics, actions, AI, game logic
            }
        }
    }
}
