using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI.Abstractions;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Main application class that orchestrates the game loop and manages the lifecycle of core services.
/// Handles the coordination between game state, user interface, and rendering systems.
/// </summary>
/// <param name="windowService">Service for managing the application window</param>
/// <param name="gameState">Manager for game state updates</param>
/// <param name="renderer">Graphics renderer for rendering frames to viewports</param>
/// <param name="logger">Logger for application-level logging</param>
public class Application(
    IWindowService windowService,
    IContentManager contentManager,
    IRenderer renderer,
    ILogger<Application> logger) : IApplication
{
    // <inheritdoc/>
    public IComponentTemplate? StartupTemplate { get; set; }

    // <inheritdoc/>
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        var window = windowService.GetOrCreateWindow();

        window.Load += Window_OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;

        windowService.Run();

        return Task.CompletedTask;
    }

    private void Window_OnLoad()
    {
        if (StartupTemplate == null)
        {
            logger.LogError("Application.StartupTemplate is required.");
            return;
        }

        var content = contentManager.GetOrCreate(StartupTemplate);
        if (content == null)
        {
            logger.LogError("Failed to create content from StartupTemplate {TemplateName}", StartupTemplate.Name);
            return;
        }

        logger.LogDebug("Created startup content from StartupTemplate {TemplateName}", StartupTemplate.Name);
        renderer.Viewport.Content = content;
        renderer.Viewport.Activate();
    }

    /// <summary>
    /// Handles the update phase of the game loop. Called once per frame before rendering.
    /// Updates all active content components. Any exceptions during update
    /// will be logged and cause the application to exit gracefully.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update, in seconds</param>
    private void OnUpdate(double deltaTime)
    {
        try
        {
            var active = contentManager.GetContent().Where(content => content.IsActive);

            foreach (var content in active)
            {
                content.Update(deltaTime);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating content");
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