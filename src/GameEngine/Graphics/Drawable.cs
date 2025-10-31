namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Base class for components that render visual content directly to the screen.
/// Provides common functionality like visibility management without transform overhead.
/// Suitable for UI elements, backgrounds, and other non-spatial visual components.
/// 
/// The source generator will create:
/// - public bool IsVisible { get; }
/// - public void SetIsVisible(bool value, float duration = -1f, InterpolationMode interpolation = default)
/// </summary>
public abstract partial class Drawable : Transformable, IDrawable
{
    /// <summary>
    /// Template for configuring Drawable components.
    /// </summary>
    public new record Template : Transformable.Template
    {
        public bool Visible { get; set; } = true;
    }
    
    public required IResourceManager ResourceManager { get; set; }
    public required PipelineHandle Pipeline { get; set; }
    
    /// <summary>
    /// Whether this component should be rendered.
    /// When false, GetDrawCommands will not be called and component is skipped during rendering.
    /// Changes to Visible are deferred until the next frame boundary to ensure temporal consistency.
    /// Use IsVisible() to check if component is both Enabled and Visible.
    /// Source generator will create: public bool Visible { get; } and SetVisible() method.
    /// </summary>
    [ComponentProperty]
    private bool _visible = true;

    /// <summary>
    /// Returns whether this component should be rendered (combines Enabled and Visible states).
    /// This is the combined state that determines if the component participates in rendering.
    /// </summary>
    public bool IsVisible() => IsValid && IsLoaded && Visible;

    /// <summary>
    /// Gets draw commands for this component.
    /// Only called when IsVisible is true.
    /// </summary>
    public abstract IEnumerable<DrawCommand> GetDrawCommands(RenderContext context);
}
