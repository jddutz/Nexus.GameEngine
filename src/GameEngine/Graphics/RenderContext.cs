using Nexus.GameEngine.Graphics.Cameras;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Provides rendering context information to components during draw command generation.
/// Contains camera, viewport, and render pass information needed for depth sorting,
/// frustum culling, and render pass filtering.
/// </summary>
public readonly struct RenderContext
{
    /// <summary>
    /// The camera being used for this render operation.
    /// Used for depth calculations, frustum culling, and view/projection matrices.
    /// </summary>
    public required ICamera? Camera { get; init; }
    
    /// <summary>
    /// The viewport being rendered to.
    /// Provides viewport dimensions and configuration.
    /// </summary>
    public required Viewport Viewport { get; init; }
    
    /// <summary>
    /// Bit mask of all available render passes.
    /// Components can use this to determine which passes exist.
    /// Example: If bit 0 is set, render pass 0 exists.
    /// </summary>
    public required uint AvailableRenderPasses { get; init; }
    
    /// <summary>
    /// Names of render passes in order (index 0 = bit 0, etc.).
    /// Used for diagnostic purposes or name-based pass lookups.
    /// </summary>
    public required string[] RenderPassNames { get; init; }
    
    /// <summary>
    /// Frame delta time in seconds.
    /// Can be used for time-based animations or effects during rendering.
    /// </summary>
    public required double DeltaTime { get; init; }
}
