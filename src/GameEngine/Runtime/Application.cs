using Microsoft.Extensions.DependencyInjection;
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
        var windowService = services.GetRequiredService<IWindowService>();

        var window = windowService.GetOrCreateWindow(windowOptions);

        window.Load += () =>
        {
            if (startupTemplate == null)
            {
                Console.WriteLine("Application.StartupTemplate is required.");
                return;
            }

            var contentManager = services.GetRequiredService<IContentManager>();
            var renderer = services.GetRequiredService<IRenderer>();

            window.Update += contentManager.OnUpdate;
            window.Render += renderer.OnRender;

            var content = contentManager.Create(startupTemplate);
            if (content == null)
            {
                Console.WriteLine("Failed to create startup template {StartupTemplate.Name}");
                return;
            }
            
            contentManager.Viewport.Content = content;
        };

        window.Run();
    }
}