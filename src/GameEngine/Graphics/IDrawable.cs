using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Interface for components that issue draw commands.
/// </summary>
public interface IDrawable : IRuntimeComponent
{
    /// <summary>
    /// Gets draw commands for this component.
    /// Called by the renderer during the render phase.
    /// Each DrawCommand specifies which render passes it participates in via RenderMask.
    /// </summary>
    /// <param name="context">Rendering context containing camera, viewport, and pass information</param>
    /// <returns>Collection of Vulkan draw commands describing what to render</returns>
    IEnumerable<DrawCommand> GetDrawCommands(RenderContext context);
}
