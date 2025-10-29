using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Cameras;
using Nexus.GameEngine.Resources;
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
    public void Run(WindowOptions windowOptions, Configurable.Template startupTemplate)
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
            
            // Trigger swapchain recreation on framebuffer resize
            // This handles window resize, minimize/restore, and fullscreen transitions
            var swapChain = services.GetService<ISwapChain>();
            if (swapChain != null)
            {
                logger.LogDebug("Triggering swapchain recreation due to framebuffer resize");
                swapChain.Recreate();
            }
        };
        
        window.StateChanged += state =>
        {
            logger.LogDebug("Window.StateChanged event. State: {State}, FramebufferSize: {Width}x{Height}", 
                state, window.FramebufferSize.X, window.FramebufferSize.Y);
        };

        window.Load += () =>
        {
            logger.LogDebug("Window.Load event. FramebufferSize: {Width}x{Height}", window.FramebufferSize.X, window.FramebufferSize.Y);
        
            var contentManager = services.GetRequiredService<IContentManager>();
            var viewportManager = services.GetRequiredService<IViewportManager>();
            var renderer = services.GetRequiredService<IRenderer>();

            if (startupTemplate == null) return;

            var mainViewport = viewportManager.CreateViewport();
            
            // Load content through ContentManager, then assign to Renderer's viewport
            logger.LogDebug("Loading startup template: {TemplateName}", startupTemplate.Name);

            mainViewport.Content = contentManager.Load(startupTemplate);

            if (!mainViewport.Activate())
            {
                logger.LogError("Viewport activation failed.");
                window.Close();
            }

            window.Update += contentManager.OnUpdate;
            window.Render += renderer.OnRender;
        };

        window.Run();
    }
}
