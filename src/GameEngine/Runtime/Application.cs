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
    /// <inheritdoc/>
    /// <summary>
    /// Starts the main application event loop with the specified window options.
    /// Creates the window, attaches event handlers, and initializes content from <see cref="StartupTemplate"/>.
    /// This method blocks until the window is closed.
    /// </summary>
    /// <param name="windowOptions">The configuration options for the main application window.</param>
    public void Run(WindowOptions windowOptions, IComponentTemplate startupTemplate)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Application));
        var windowService = services.GetRequiredService<IWindowService>();
        var window = windowService.GetOrCreateWindow(windowOptions);

        window.Load += () =>
        {
            try
            {
                var contentManager = services.GetRequiredService<IContentManager>();
                var renderer = services.GetRequiredService<IRenderer>();

                if (startupTemplate == null)
                {
                    logger?.LogError("Application.StartupTemplate is required.");
                    return;
                }

                var content = contentManager.Create(startupTemplate);
                if (content == null)
                {
                    logger?.LogError("Failed to create startup template {StartupTemplateName}", startupTemplate.Name);
                    return;
                }

                contentManager.Viewport.Content = content;

                window.Update += dt =>
                {
                    try
                    {
                        contentManager.OnUpdate(dt);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Exception during window.Update event");
                    }
                };

                window.Render += dt =>
                {
                    try
                    {
                        renderer.OnRender(dt);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Exception during window.Render event");
                    }
                };
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception during window.Load event");
            }
        };

        try
        {
            window.Run();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unhandled exception in Application.Run");
            throw;
        }
    }
}