using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages viewport creation and lifecycle.
/// </summary>
public class ViewportManager(
    ILoggerFactory loggerFactory,
    IWindowService windowService,
    IOptions<GraphicsSettings> graphicsSettings)
    : IViewportManager
{
    private ILogger logger = loggerFactory.CreateLogger<ViewportManager>();
    private IWindow window = windowService.GetWindow();
    private readonly SortedSet<Viewport> viewports = new(new ViewportPriorityComparer());

    /// <summary>
    /// Gets all managed viewports, sorted by render priority.
    /// </summary>
    public IReadOnlyCollection<Viewport> Viewports => viewports;

    /// <summary>
    /// Creates a new viewport with the specified parameters and automatically registers it.
    /// </summary>
    /// <param name="x">X position (normalized 0-1)</param>
    /// <param name="y">Y position (normalized 0-1)</param>
    /// <param name="width">Width (normalized 0-1)</param>
    /// <param name="height">Height (normalized 0-1)</param>
    /// <param name="backgroundColor">Optional background color</param>
    /// <returns>The created viewport</returns>
    public Viewport CreateViewport(
        float x = 0f,
        float y = 0f,
        float width = 1f,
        float height = 1f,
        Vector4D<float>? backgroundColor = null)
    {
        var bgColor = backgroundColor ?? graphicsSettings.Value.BackgroundColor ?? Colors.DarkBlue;

        var viewport = new Viewport(logger, window)
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            BackgroundColor = bgColor
        };

        logger.LogDebug("Created viewport at ({X},{Y}) with size ({Width},{Height})", x, y, width, height);

        // Automatically register the viewport
        AddViewport(viewport);

        return viewport;
    }

    /// <summary>
    /// Adds a viewport to management.
    /// </summary>
    public void AddViewport(Viewport viewport)
    {
        viewports.Add(viewport);
        logger.LogDebug("Added viewport with priority {Priority}", viewport.RenderPriority);
    }

    /// <summary>
    /// Removes a viewport from management.
    /// </summary>
    public void RemoveViewport(Viewport viewport)
    {
        viewports.Remove(viewport);
        logger.LogDebug("Removed viewport");
    }

    /// <summary>
    /// Invalidates all viewport Vulkan state (e.g., after window resize).
    /// </summary>
    public void InvalidateAllViewports()
    {
        foreach (var viewport in viewports)
        {
            viewport.Invalidate();
        }

        logger.LogDebug("Invalidated all viewport Vulkan state");
    }

    /// <summary>
    /// Disposes all managed viewports.
    /// </summary>
    public virtual void Dispose()
    {
        viewports.Clear();
    }
}
