using Nexus.GameEngine.Animation;
using Nexus.GameEngine.Components;

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
public abstract partial class DrawableComponent : RuntimeComponent, IDrawable
{
    /// <summary>
    /// Whether this component should be rendered.
    /// When false, GetDrawCommands will not be called and component is skipped during rendering.
    /// Generated property: IsVisible (read-only), SetIsVisible(...) method for updates.
    /// </summary>
    [ComponentProperty(Duration = AnimationDuration.Fast, Interpolation = InterpolationMode.Step)]
    private bool _isVisible = true;

    /// <summary>
    /// Gets draw commands for this component.
    /// Only called when IsVisible is true.
    /// </summary>
    public abstract IEnumerable<DrawCommand> GetDrawCommands(RenderContext context);
}
