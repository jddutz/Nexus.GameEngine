using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Abstractions;

/// <summary>
/// Interface for components that have a preferred size for layout calculations.
/// </summary>
public interface ISizable
{
    /// <summary>
    /// Gets the preferred size of this component.
    /// Used by layout systems to determine optimal sizing.
    /// </summary>
    /// <returns>The preferred size as a Vector2D</returns>
    Vector2D<float> GetPreferredSize();

    /// <summary>
    /// Gets the minimum size this component can be rendered at.
    /// </summary>
    /// <returns>The minimum size as a Vector2D</returns>
    Vector2D<float> GetMinimumSize();
}