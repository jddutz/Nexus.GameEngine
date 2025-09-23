using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics.Rendering;
using Nexus.GameEngine.GUI.Abstractions;

namespace Nexus.GameEngine.Runtime;

// Engine.Runtime.Application - Just orchestrates the game
public class Application(
    IWindowService windowService,
    IGameStateManager gameState,
    IUserInterfaceManager userInterface,
    IRenderer renderer,
    ILogger<Application> logger) : IApplication
{
    private readonly IWindowService _windowService = windowService;
    private readonly IGameStateManager _gameState = gameState;
    private readonly IUserInterfaceManager _ui = userInterface;
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
                _ui.Update(deltaTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user interface update");
                throw; // Re-throw to ensure the application terminates properly
            }
        };

        // Renderer handles all rendering including UI components
        window.Render += (deltaTime) =>
        {
            try
            {
                _renderer.RenderFrame();
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