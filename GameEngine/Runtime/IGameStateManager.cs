using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Manages the overall game state and progression
/// </summary>
public interface IGameStateManager : IComponentCollection
{
    /// <summary>
    /// Updates game logic: physics, actions, AI, and game object behaviors.
    /// Called once per frame during the update phase.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update.</param>
    void Update(double deltaTime);
}