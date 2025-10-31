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
    public void Run(WindowOptions windowOptions, Element.Template startupTemplate)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Application));
        var windowService = services.GetRequiredService<IWindowService>();
        var window = windowService.GetOrCreateWindow(windowOptions);

        // Log window events to understand initialization timing
        Log.Debug("Window created");
        
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
            var viewportManager = services.GetRequiredService<IViewportManager>();
            var renderer = services.GetRequiredService<IRenderer>();

            if (startupTemplate == null) return;

            var mainViewport = viewportManager.CreateViewport(new StaticCamera());
            
            // Load content through ContentManager, then assign to Renderer's viewport
            Log.Debug("Loading startup template: {TemplateName}", startupTemplate.Name ?? "null");

            mainViewport.Content = contentManager.Load(startupTemplate);

            // Push window size constraints to main viewport's root element
            // Element.Template parameter ensures this cast should always succeed
            if (mainViewport.Content is IUserInterfaceElement rootElement)
            {
                var constraints = new Rectangle<int>(0, 0, window.Size.X, window.Size.Y);
                rootElement.SetSizeConstraints(constraints);
                
                // Update constraints when window resizes
                window.Resize += newSize =>
                {
                    rootElement.SetSizeConstraints(new Rectangle<int>(0, 0, newSize.X, newSize.Y));
                };
            }
            else
            {
                var typeName = mainViewport.Content?.GetType().Name ?? "null";
                throw new InvalidOperationException(
                    $"Invalid startup content template, '{typeName}'."
                    + " Startup content must be IUserInterfaceElement."
                    + " Ensure the template was properly registered with the service provider.");
            }

            if (!mainViewport.Activate())
            {
                Log.Error("Viewport activation failed.");
                window.Close();
            }

            window.Update += contentManager.OnUpdate;
            window.Render += renderer.OnRender;
        };

        window.Run();
    }
}
