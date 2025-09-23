using Silk.NET.OpenGL;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;

namespace Nexus.GameEngine.Graphics.Rendering;

/// <summary>
/// Minimal renderer interface providing GL access, shared resource management, and render pass orchestration.
/// Component tree walking is handled by the application, not the renderer.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Direct access to Silk.NET OpenGL interface for component rendering.
    /// Use GLRenderingExtensions for common helper methods.
    /// </summary>
    GL GL { get; }

    /// <summary>
    /// Collection of draw batches for lambda-based batching support.
    /// Provides read-only access to all batches for debugging and statistics.
    /// </summary>
    List<DrawBatch> Batches { get; }

    /// <summary>
    /// Gets or creates a draw batch for the specified batch key.
    /// Uses object-based keys for maximum flexibility in batching strategies.
    /// </summary>
    /// <param name="batchKey">Unique key identifying the batch (typically tuple of render state)</param>
    /// <returns>Existing or newly created draw batch for the key</returns>
    DrawBatch GetOrCreateBatch(object batchKey);

    /// <summary>
    /// Root component for the component tree to render.
    /// The renderer will walk this tree during RenderFrame() to collect draw commands.
    /// </summary>
    IRuntimeComponent? RootComponent { get; set; }

    /// <summary>
    /// List of discovered cameras in the component tree.
    /// Automatically populated when RenderFrame() is called if empty.
    /// </summary>
    IReadOnlyList<ICamera> Cameras { get; }

    /// <summary>
    /// Shared resource management for common assets like fullscreen quads, default shaders.
    /// Provides a simple cache that components can use for frequently-used resources.
    /// </summary>
    T GetSharedResource<T>(string name);
    void SetSharedResource<T>(string name, T resource);

    /// <summary>
    /// Orchestrates the actual rendering process through all configured passes.
    /// Called after the application has walked the component tree to update GL state.
    /// This method executes the render passes in dependency order.
    /// </summary>
    void RenderFrame();
}