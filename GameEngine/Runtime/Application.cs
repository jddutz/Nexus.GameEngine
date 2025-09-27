using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics;

namespace Nexus.GameEngine.Runtime;

// Engine.Runtime.Application - Just orchestrates the game
public class Application(
    IWindowService windowService,
    IGameStateManager gameState,
    IRenderer renderer,
    ILogger<Application> logger) : IApplication
{
    private readonly IWindowService _windowService = windowService;
    private readonly IGameStateManager _gameState = gameState;
    private readonly IRenderer _renderer = renderer;
    private readonly ILogger<Application> _logger = logger;

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        var window = _windowService.GetOrCreateWindow();

        // Update game state: physics, actions, AI, game logic
        window.Update += (deltaTime) =>
        {
            try
            {
                _gameState.Update(deltaTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during game state update");
                throw; // Re-throw to ensure the application terminates properly
            }
        };

        window.Update += (deltaTime) =>
        {
            try
            {
                // Check if renderer has a root component (UI) set
                if (_renderer.RootComponent == null)
                {
                    _logger.LogInformation("No root component set on renderer - exiting");
                    _windowService.Close();
                    return;
                }

                // Update the root component (which will recursively update all children)
                _renderer.RootComponent.Update(deltaTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during component update");
                throw; // Re-throw to ensure the application terminates properly
            }
        };

        // Renderer handles all rendering including UI components
        window.Render += (deltaTime) =>
        {
            try
            {
                _renderer.RenderFrame(deltaTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during frame rendering");
                throw; // Re-throw to ensure the application terminates properly
            }
        };

        // Run the window - Silk.NET handles everything
        _windowService.Run();

        return Task.CompletedTask;
    }
}