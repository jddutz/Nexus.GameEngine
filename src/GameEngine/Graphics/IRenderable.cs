using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Interface for components that can be rendered.
/// Components implementing this interface provide rendering data to the engine.
/// </summary>
public interface IRenderable : IRuntimeComponent
{
    /// <summary>
    /// Render priority. Lower values render first (e.g., backgrounds=0, UI=1000)
    /// </summary>
    uint RenderPriority { get; }

    /// <summary>
    /// Gets elements for this component.
    /// Called by the renderer during the render phase.
    /// </summary>
    /// <param name="viewport">The viewport being rendered to</param>
    /// <returns>Collection of render elements describing what to draw</returns>
    IEnumerable<ElementData> GetElements();
}
