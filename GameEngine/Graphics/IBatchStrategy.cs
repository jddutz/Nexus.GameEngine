using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Provides an abstraction for batching strategies in the rendering pipeline.
/// Implementations define how renderable components are grouped into batches to minimize OpenGL state changes and improve rendering performance.
/// </summary>
public interface IBatchStrategy : IComparer<RenderState>
{
    /// <summary>
    /// Computes a stable hash code for the specified <see cref="RenderState"/> to facilitate efficient batch grouping.
    /// Render states with the same hash code are considered part of the same batch.
    /// </summary>
    /// <param name="state">The <see cref="RenderState"/> to compute the hash code for.</param>
    /// <returns>An integer hash code representing the batchable aspects of the render state.</returns>
    int GetHashCode(RenderState state);

    /// <summary>
    /// Computes a stable hash code for the specified <see cref="GL"/> to facilitate efficient batch grouping.
    /// Render states with the same hash code are considered part of the same batch.
    /// </summary>
    /// <param name="gl">The <see cref="GL"/> to compute the hash code for.</param>
    /// <returns>An integer hash code representing the batchable aspects of the GL state.</returns>
    int GetHashCode(GL gl);
}