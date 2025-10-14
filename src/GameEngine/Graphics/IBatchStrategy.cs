namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Provides an abstraction for batching strategies in the rendering pipeline.
/// Implementations define how renderable components are grouped into batches to minimize OpenGL state changes and improve rendering performance.
/// </summary>
public interface IBatchStrategy : IComparer<DrawCommand>
{
    /// <summary>
    /// Computes a stable hash code for the specified <see cref="DrawCommand"/> to facilitate efficient batch grouping.
    /// Render states with the same hash code are considered part of the same batch.
    /// </summary>
    /// <param name="state">The <see cref="DrawCommand"/> to compute the hash code for.</param>
    /// <returns>An integer hash code representing the batchable aspects of the render state.</returns>
    int GetHashCode(DrawCommand state);
}