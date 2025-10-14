using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Interface for components that can be rendered.
/// Components implementing this interface provide rendering data to the engine.
/// </summary>
public interface IRenderable : IRuntimeComponent
{
    /// <summary>
    /// Gets draw commands for this component.
    /// Called by the renderer during the render phase.
    /// Each DrawCommand specifies which render passes it participates in via RenderMask.
    /// </summary>
    /// <returns>Collection of Vulkan draw commands describing what to render</returns>
    IEnumerable<DrawCommand> GetDrawCommands();
}
