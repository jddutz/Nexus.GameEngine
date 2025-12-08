using Microsoft.Extensions.DependencyInjection;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Performance;

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
    public void Run(WindowOptions windowOptions, ComponentTemplate startupTemplate)
    {
        var windowService = services.GetRequiredService<IWindowService>();
        var window = windowService.GetOrCreateWindow(windowOptions);
        
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
            var profiler = services.GetRequiredService<IProfiler>();

            if (startupTemplate == null)
            {
                Log.Warning("startupTemplate is null, skipping content load");
                return;
            }

            // Load content through ContentManager
            // ContentManager automatically registers cameras found in the content tree
            var startupContent = contentManager.Load(startupTemplate);

            // Push window size constraints to root element if it's a UI element
            // This is optional - not all startup content needs to be IUserInterfaceElement
            if (startupContent is IUserInterfaceElement rootElement)
            {
                // Use centered coordinate system to match StaticCamera's viewport
                // Origin at center: (-width/2, -height/2) to (width/2, height/2)
                var constraints = new Rectangle<float>(0, 0, window.Size.X, window.Size.Y);
                rootElement.UpdateLayout(constraints);
                
                // Update constraints when window resizes
                window.Resize += newSize =>
                {
                    var newConstraints = new Rectangle<float>(0, 0, newSize.X, newSize.Y);
                    rootElement.UpdateLayout(newConstraints);
                };
            }

            if (startupContent is IActivatable rc)
            {
                rc.Activate();
            }

            window.Update += deltaTime =>
            {
                profiler.BeginFrame();
                contentManager.OnUpdate(deltaTime);
            };
            
            window.Render += deltaTime =>
            {
                renderer.OnRender(deltaTime);
                profiler.EndFrame();
            };
        };

        window.Run();
    }
}
