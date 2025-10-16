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

        // Log window events to understand initialization timing
        logger.LogDebug("Window created");
        
        window.Resize += size =>
        {
            logger.LogDebug("Window.Resize event. Size: {Width}x{Height}, FramebufferSize: {FbWidth}x{FbHeight}", 
                size.X, size.Y, window.FramebufferSize.X, window.FramebufferSize.Y);
        };
        
        window.FramebufferResize += size =>
        {
            logger.LogDebug("Window.FramebufferResize event. FramebufferSize: {Width}x{Height}", size.X, size.Y);
        };
        
        window.StateChanged += state =>
        {
            logger.LogDebug("Window.StateChanged event. State: {State}, FramebufferSize: {Width}x{Height}", 
                state, window.FramebufferSize.X, window.FramebufferSize.Y);
        };

        window.Load += () =>
        {
            logger.LogDebug("Window.Load event. FramebufferSize: {Width}x{Height}", window.FramebufferSize.X, window.FramebufferSize.Y);
            
            try
            {
                logger.LogDebug("Creating ContentManager and Renderer...");
                var contentManager = services.GetRequiredService<IContentManager>();
                var renderer = services.GetRequiredService<IRenderer>();
                logger.LogDebug("Renderer created. FramebufferSize: {Width}x{Height}", window.FramebufferSize.X, window.FramebufferSize.Y);

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
            logger.LogDebug("Calling window.Run()...");
            window.Run();
            logger.LogDebug("window.Run() completed.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unhandled exception in Application.Run");
            throw;
        }
    }
}