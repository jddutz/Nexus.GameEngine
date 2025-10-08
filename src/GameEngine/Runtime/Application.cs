using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Silk.NET.Windowing;

namespace Nexus.GameEngine.Runtime;

/// <inheritdoc/>
/// <summary>
/// Implements the main application entry point for the Nexus Game Engine runtime.
/// Manages window creation, event loop, and startup content initialization.
/// </summary>
public class Application(IServiceProvider services) : IApplication
{
    private readonly ILogger logger = services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger(nameof(Application));

    private readonly IWindowService windowService = services.GetRequiredService<IWindowService>();
    private readonly IContentManager contentManager = services.GetRequiredService<IContentManager>();
    private readonly IRenderer renderer = services.GetRequiredService<IRenderer>();
    private IWindow? window;

    /// <inheritdoc/>
    public IComponentTemplate? StartupTemplate { get; set; }

    /// <inheritdoc/>
    /// <summary>
    /// Starts the main application event loop with the specified window options.
    /// Creates the window, attaches event handlers, and initializes content from <see cref="StartupTemplate"/>.
    /// This method blocks until the window is closed.
    /// </summary>
    /// <param name="windowOptions">The configuration options for the main application window.</param>
    public void Run(WindowOptions windowOptions)
    {
        window = windowService.GetOrCreateWindow(windowOptions);

        window.Load += Window_OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;

        windowService.Run();
    }

    /// <summary>
    /// Handles the window load event. Called after the window and graphics context are initialized.
    /// Initializes the application content using the configured <see cref="StartupTemplate"/>.
    /// Sets the renderer's viewport content and triggers renderer initialization.
    /// Logs errors if content creation fails.
    /// </summary>
    private void Window_OnLoad()
    {
        if (StartupTemplate == null)
        {
            logger.LogError("Application.StartupTemplate is required.");
            window?.Close();
            return;
        }

        var content = contentManager.Create(StartupTemplate);
        if (content == null)
        {
            logger.LogError("Failed to create content from StartupTemplate {TemplateName}", StartupTemplate.Name);
            return;
        }

        logger.LogDebug("Created startup content from StartupTemplate {ComponentName}", content.Name);

        contentManager.Viewport.Content = content;
    }

    /// <summary>
    /// Handles the update phase of the game loop. Called once per frame before rendering.
    /// Iterates all active content components and updates their state.
    /// Any exceptions during update are logged and cause the application to exit gracefully.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update, in seconds.</param>
    private void OnUpdate(double deltaTime)
    {
        try
        {
            contentManager.OnUpdate(deltaTime);
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
    /// are logged and cause the application to exit gracefully.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last render, in seconds.</param>
    private void OnRender(double deltaTime)
    {
        try
        {
            renderer.OnRender(deltaTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during frame rendering");
            Exit();
        }
    }

    /// <summary>
    /// Gracefully exits the application by closing the window if it exists and is not already closing.
    /// Called when an unrecoverable error occurs during the game loop to ensure proper shutdown.
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