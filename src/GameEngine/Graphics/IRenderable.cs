using Nexus.GameEngine.Components;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Interface for components that declare their rendering requirements to the engine.
/// Components implementing this interface provide collections of <see cref="ElementData"/> describing how they should be rendered.
/// This enables declarative, batch-friendly rendering without direct OpenGL calls in component code.
/// </summary>
public interface IRenderable : IRuntimeComponent
{
    /// <summary>
    /// Returns a collection of <see cref="ElementData"/> objects describing the rendering requirements for this component.
    /// Each <see cref="ElementData"/> specifies geometry, shader, textures, and other state needed for a single draw call.
    /// Components do not perform OpenGL calls directly; instead, they declare what is needed and the renderer executes the draw logic.
    /// </summary>
    /// <param name="gl">The OpenGL context for resource creation and queries</param>
    /// <param name="vp">The current viewport context</param>
    /// <returns>An enumerable collection of <see cref="ElementData"/> objects for this component</returns>
    IEnumerable<ElementData> GetElements(GL gl, IViewport vp);

    /// <summary>
    /// Indicates whether the component is currently visible and should be rendered.
    /// This property is read-only; use <see cref="SetVisible(bool)"/> to change visibility.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Sets the visibility of the renderable component. The change is applied at the next frame boundary for consistency.
    /// </summary>
    /// <param name="visible">True to show the component, false to hide it.</param>
    void SetVisible(bool visible);

    /// <summary>
    /// Render priority for sorting components during batching. Lower values render first.
    /// Recommended values: Background=0, 3D Objects=100-299, Transparent=300-399, UI=400+.
    /// </summary>
    uint RenderPriority { get; }

    /// <summary>
    /// Bounding box for frustum culling. Components outside the camera view are automatically culled.
    /// Return <see cref="Box3D{float}.Empty"/> if the component should never be culled (e.g., UI elements, skybox).
    /// </summary>
    Box3D<float> BoundingBox { get; }

    /// <summary>
    /// Bit flags indicating which render passes this component participates in.
    /// Uses <see cref="RenderPassConfiguration.Id"/> as bit positions (1 &lt;&lt; passId).
    /// Default: All passes (0xFFFFFFFF). Set to 0 to exclude from all passes.
    /// </summary>
    /// <example>
    /// // Include in passes 0, 1, and 3
    /// RenderPassFlags = (1 << 0) | (1 << 1) | (1 << 3);
    /// // Exclude from shadow pass (pass 2) but include others
    /// RenderPassFlags = 0xFFFFFFFF & ~(1 << 2);
    /// </example>
    uint RenderPassFlags { get; }
}