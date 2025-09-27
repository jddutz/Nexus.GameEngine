using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI.Abstractions;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Main application class that orchestrates the game loop and manages the lifecycle of core services.
/// Handles the coordination between game state, user interface, and rendering systems.
/// </summary>
/// <param name="windowService">Service for managing the application window</param>
/// <param name="gameState">Manager for game state updates</param>
/// <param name="uiManager">Manager for user interface components and lifecycle</param>
/// <param name="renderer">Graphics renderer for drawing frames</param>
/// <param name="logger">Logger for application-level logging</param>
public class Application(
    IWindowService windowService,
    IGameStateManager gameState,
    IUserInterfaceManager uiManager,
    IRenderer renderer,
    ILogger<Application> logger) : IApplication
{
    /// <summary>
    /// Runs the application asynchronously, setting up the main game loop and event handlers.
    /// This method initializes the window, subscribes to update and render events, and starts the main loop.
    /// The application will run until the window is closed or an unrecoverable error occurs.
    /// </summary>
    /// <param name="cancellationToken">Token to support cancellation of the operation (currently not used as window lifecycle is managed by the windowing system)</param>
    /// <returns>A task that represents the running application</returns>
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        var window = windowService.GetOrCreateWindow();

        window.Update += OnUpdate;
        window.Render += OnRender;

        // Note: WindowService.Run() is synchronous and blocks until window closes
        // Future enhancement could add cancellation token support
        windowService.Run();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the update phase of the game loop. Called once per frame before rendering.
    /// Updates game state first, then user interface components. Any exceptions during update
    /// will be logged and cause the application to exit gracefully.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update, in seconds</param>
    private void OnUpdate(double deltaTime)
    {
        // Update GameState first - handles core game logic
        try
        {
            gameState.Update(deltaTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Game State");
            Exit();
        }

        // Update UI second - this processes pending activations and component updates
        // UI updates happen after game state to ensure all services are ready
        try
        {
            uiManager.Update(deltaTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during UI component update");
            Exit();
        }
    }

    /// <summary>
    /// Handles the render phase of the game loop. Called once per frame after the update phase.
    /// Renders the current frame using the configured renderer. Any exceptions during rendering
    /// will be logged and cause the application to exit gracefully.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last render, in seconds</param>
    private void OnRender(double deltaTime)
    {
        try
        {
            renderer.RenderFrame(deltaTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during frame rendering");
            Exit();
        }
    }

    /// <summary>
    /// Gracefully exits the application by closing the window if it exists and is not already closing.
    /// This method is called when an unrecoverable error occurs during the game loop.
    /// </summary>
    private void Exit()
    {
        if (windowService.IsWindowCreated)
        {
            var window = windowService.GetWindow();

            if (window != null && !window.IsClosing)
                window.Close();
        }
    }
}