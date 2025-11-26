namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Interface for components that issue draw commands.
/// </summary>
public interface IDrawable : IRuntimeComponent
{    
    /// <summary>
    /// Gets whether this drawable component should be rendered.
    /// The renderer will skip calling GetDrawCommands() if this is false.
    /// Should be implemented as a [TemplateProperty] for deferred updates and animation support.
    /// </summary>
    bool IsVisible();

    /// <summary>
    /// Gets draw commands for this component.
    /// Called by the renderer during the render phase only if IsVisible is true.
    /// Each DrawCommand specifies which render passes it participates in via RenderMask.
    /// </summary>
    /// <param name="context">Rendering context containing camera, viewport, and pass information</param>
    /// <returns>Collection of Vulkan draw commands describing what to render</returns>
    IEnumerable<DrawCommand> GetDrawCommands(RenderContext context);
}
