using Nexus.GameEngine.Components;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Interface for components that can be rendered directly using OpenGL calls.
/// Eliminates command pattern complexity in favor of direct GL operations.
/// </summary>
public interface IRenderable : IRuntimeComponent
{
    /// <summary>
    /// Renders this component directly using OpenGL calls
    /// </summary>
    /// <param name="renderer">Provides GL context and helper methods</param>
    /// <param name="deltaTime">Time since last frame for animations</param>
    void OnRender(IRenderer renderer, double deltaTime);

    /// <summary>
    /// Whether this component is visible (legacy compatibility)
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Render priority for sorting (lower values render first)
    /// Background=0, 3D Objects=100-299, Transparent=300-399, UI=400+
    /// </summary>
    int RenderPriority { get; }

    /// <summary>
    /// Bounding box for frustum culling. Components outside camera view are automatically culled.
    /// Return Box3D.Empty if component should never be culled (e.g., UI elements, skybox).
    /// </summary>
    Box3D<float> BoundingBox { get; }

    /// <summary>
    /// Bit flags indicating which render passes this component participates in.
    /// Uses RenderPassConfiguration.Id as bit positions (1 << passId).
    /// Default: All passes (0xFFFFFFFF). Set to 0 to exclude from all passes.
    /// </summary>
    /// <example>
    /// // Include in passes 0, 1, and 3
    /// RenderPassFlags = (1 << 0) | (1 << 1) | (1 << 3);
    /// 
    /// // Exclude from shadow pass (pass 2) but include others
    /// RenderPassFlags = 0xFFFFFFFF & ~(1 << 2);
    /// </example>
    uint RenderPassFlags { get; }
}