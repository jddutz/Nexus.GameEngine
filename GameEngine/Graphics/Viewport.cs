using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Basic viewport implementation with delayed content assignment capabilities.
/// Supports changing content trees during runtime with proper lifecycle management.
/// Inherits from RuntimeComponent to participate in the standard component lifecycle.
/// </summary>
public class Viewport(ICamera? camera = null) : RuntimeComponent, IViewport
{
    public new record Template : RuntimeComponent.Template
    {
        public ICamera? Camera { get; set; }
        public Rectangle<int> ScreenRegion { get; set; }
        public uint? FramebufferTarget { get; set; }
        public int ViewportPriority { get; set; } = 0;
        public List<RenderPassConfiguration> RenderPasses { get; set; } = new();
        public bool RequiresFlushAfterRender { get; set; } = false;
    }

    private IRuntimeComponent? _content;
    private IRuntimeComponent? _pendingContent;
    private bool _hasPendingContentChange;

    public Rectangle<int> ScreenRegion { get; set; }

    public ICamera Camera { get; set; } = camera ?? new StaticCamera();

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
                Logger?.LogDebug("Scheduled content change for next update cycle");
            }
        }
    }

    /// <summary>
    /// Processes any pending content changes. Should be called during update cycle.
    /// Content activation/deactivation is now handled by ContentManager, not Viewport.
    /// </summary>
    public void ProcessPendingContentChanges()
    {
        if (!_hasPendingContentChange) return;

        // Simply assign new content - no lifecycle management needed
        // Components are activated when created by ContentManager
        _content = _pendingContent;
        Logger?.LogDebug("Applied pending content change");

        // Clear pending change
        _hasPendingContentChange = false;
        _pendingContent = null;
    }

    /// <summary>
    /// Override OnActivate to immediately apply any pending content changes.
    /// This allows viewports to be activated after content assignment to bypass the deferred update cycle.
    /// </summary>
    protected override void OnActivate()
    {
        if (_content == null && _pendingContent != null)
        {
            _content = _pendingContent;
            _hasPendingContentChange = false;
            _pendingContent = null;
            Logger?.LogDebug("Applied pending content change during activation");
        }
    }

    /// <summary>
    /// Override OnUpdate to handle pending content changes during the component lifecycle.
    /// </summary>
    protected override void OnUpdate(double deltaTime)
    {
        // Process pending content changes during update cycle
        ProcessPendingContentChanges();

        base.OnUpdate(deltaTime);
    }

    /// <summary>
    /// Override OnConfigure to apply template settings to viewport properties.
    /// </summary>
    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            if (template.Camera != null)
                Camera = template.Camera;
            ScreenRegion = template.ScreenRegion;
            FramebufferTarget = template.FramebufferTarget;
            ViewportPriority = template.ViewportPriority;
            RenderPasses = template.RenderPasses;
            RequiresFlushAfterRender = template.RequiresFlushAfterRender;
        }
    }

    public IEnumerable<RenderState> OnRender(double deltaTime)
    {
        if (_content == null)
        {
            return Enumerable.Empty<RenderState>();
        }

        // Apply all deferred updates before rendering
        ApplyUpdatesRecursively(_content);

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
    /// Recursively applies deferred updates to all components in the content tree.
    /// Called before rendering to ensure temporal consistency.
    /// </summary>
    private static void ApplyUpdatesRecursively(IRuntimeComponent component)
    {
        // Apply updates to this component
        component.ApplyUpdates();

        // Recursively apply updates to all children
        foreach (var child in component.Children)
        {
            ApplyUpdatesRecursively(child);
        }
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