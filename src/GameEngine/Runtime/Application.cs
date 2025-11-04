using Microsoft.Extensions.DependencyInjection;
using Nexus.GameEngine.GUI;

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
    /// <param name="startupTemplate">The template for the root component to load on startup.</param>
    public void Run(WindowOptions windowOptions, Template startupTemplate)
    {
        var windowService = services.GetRequiredService<IWindowService>();
        var window = windowService.GetOrCreateWindow(windowOptions);

        // Log window events to understand initialization timing
        // Log.Debug("Window created");
        
        window.FramebufferResize += size =>
        {
            // Trigger swapchain recreation on framebuffer resize
            // This handles window resize, minimize/restore, and fullscreen transitions
            var swapChain = services.GetService<ISwapChain>();
            if (swapChain != null)
            {
                swapChain.Recreate();
            }
        };

        window.Load += () =>
        {
            var contentManager = services.GetRequiredService<IContentManager>();
            var renderer = services.GetRequiredService<IRenderer>();

            // Initialize ContentManager (creates default camera for zero-configuration rendering)
            contentManager.Initialize();

            if (startupTemplate == null) return;

            // Load content through ContentManager
            // ContentManager automatically registers cameras found in the content tree
            // Log.Debug($"Loading startup template: {startupTemplate.Name ?? "null"}");
            var startupContent = contentManager.Load(startupTemplate);

            // Push window size constraints to root element if it's a UI element
            // This is optional - not all startup content needs to be IUserInterfaceElement
            if (startupContent is IUserInterfaceElement rootElement)
            {
                var constraints = new Rectangle<int>(0, 0, window.Size.X, window.Size.Y);
                rootElement.SetSizeConstraints(constraints);
                
                // Update constraints when window resizes
                window.Resize += newSize =>
                {
                    rootElement.SetSizeConstraints(new Rectangle<int>(0, 0, newSize.X, newSize.Y));
                };
            }

            window.Update += contentManager.OnUpdate;
            window.Render += renderer.OnRender;
        };

        window.Run();
    }
}
