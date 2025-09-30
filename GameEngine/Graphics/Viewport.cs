using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Basic viewport implementation with delayed content assignment capabilities.
/// Supports changing content trees during runtime with proper lifecycle management.
/// </summary>
public class Viewport : IViewport
{
    private readonly ILogger<Viewport>? _logger;
    private IRuntimeComponent? _content;
    private IRuntimeComponent? _pendingContent;
    private bool _hasPendingContentChange;

    public Viewport(Rectangle<int> screenRegion, ICamera camera, ILogger<Viewport>? logger = null)
    {
        ScreenRegion = screenRegion;
        Camera = camera;
        _logger = logger;
    }

    public Rectangle<int> ScreenRegion { get; set; }

    public ICamera Camera { get; set; }

    public uint? FramebufferTarget { get; set; }

    public int ViewportPriority { get; set; } = 0;

    public List<RenderPassConfiguration> RenderPasses { get; set; } = new();

    public bool RequiresFlushAfterRender { get; set; } = false;

    /// <summary>
    /// Gets or sets the content tree. Setting schedules a delayed content change.
    /// </summary>
    public IRuntimeComponent? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _pendingContent = value;
                _hasPendingContentChange = true;
                _logger?.LogDebug("Scheduled content change for next update cycle");
            }
        }
    }

    /// <summary>
    /// Processes any pending content changes. Should be called during update cycle.
    /// </summary>
    public void ProcessPendingContentChanges()
    {
        if (!_hasPendingContentChange) return;

        // Deactivate current content
        if (_content != null)
        {
            _content.Deactivate();
            _logger?.LogDebug("Deactivated previous content");
        }

        // Activate new content
        _content = _pendingContent;
        if (_content != null)
        {
            _content.Activate();
            _logger?.LogDebug("Activated new content");
        }

        // Clear pending change
        _hasPendingContentChange = false;
        _pendingContent = null;
    }

    public IEnumerable<RenderState> OnRender(double deltaTime)
    {
        // Process any pending content changes first
        ProcessPendingContentChanges();

        if (_content == null)
        {
            return Enumerable.Empty<RenderState>();
        }

        // Walk the content tree and collect render states
        return FindRenderableComponents(_content)
            .Where(component => component.IsEnabled && component.IsVisible)
            .SelectMany(c => c.OnRender(this, deltaTime))
            .Select(rs =>
            {
                rs.SourceViewport = this; // Tag render states with their source viewport
                return rs;
            });
    }

    /// <summary>
    /// Recursively finds all IRenderable components in the content tree.
    /// </summary>
    private static IEnumerable<IRenderable> FindRenderableComponents(IRuntimeComponent component)
    {
        if (component is IRenderable renderable)
            yield return renderable;

        foreach (var child in component.Children)
        {
            foreach (var childRenderable in FindRenderableComponents(child))
                yield return childRenderable;
        }
    }
}