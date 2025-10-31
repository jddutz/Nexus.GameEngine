using Microsoft.Extensions.Options;

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
    /// <param name="camera">The camera to use for rendering this viewport</param>
    /// <param name="x">X position (normalized 0-1)</param>
    /// <param name="y">Y position (normalized 0-1)</param>
    /// <param name="width">Width (normalized 0-1)</param>
    /// <param name="height">Height (normalized 0-1)</param>
    /// <param name="backgroundColor">Optional background color</param>
    /// <param name="content">Optional root component to render in this viewport</param>
    /// <returns>The created viewport</returns>
    public Viewport CreateViewport(
        ICamera camera,
        float x = 0f,
        float y = 0f,
        float width = 1f,
        float height = 1f,
        Vector4D<float>? backgroundColor = null,
        IComponent? content = null)
    {
        var bgColor = backgroundColor ?? graphicsSettings.Value.BackgroundColor ?? Colors.DarkBlue;

        var viewport = new Viewport(logger, window)
        {
            Camera = camera,
            Content = content,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            BackgroundColor = bgColor
        };

        Log.Debug($"Created viewport at ({x},{y}) with size ({width},{height}) with camera {camera?.GetType().Name ?? "null"}");

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
        Log.Debug($"Added viewport with priority {viewport.RenderPriority}");
    }

    /// <summary>
    /// Removes a viewport from management.
    /// </summary>
    public void RemoveViewport(Viewport viewport)
    {
        viewports.Remove(viewport);
        Log.Debug("Removed viewport");
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

        Log.Debug("Invalidated all viewport Vulkan state");
    }

    /// <summary>
    /// Disposes all managed viewports.
    /// </summary>
    public virtual void Dispose()
    {
        viewports.Clear();
    }
}
