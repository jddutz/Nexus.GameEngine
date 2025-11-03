namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Defines a component that has a render priority for ordering rendering operations.
/// Lower priority values render first.
/// </summary>
public interface IRenderPriority
{
    /// <summary>
    /// Gets the render priority. Lower values render first.
    /// </summary>
    int RenderPriority { get; }
}
