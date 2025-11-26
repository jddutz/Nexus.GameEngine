using Nexus.GameEngine.Components;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Defines the contract for a 2D spatial component.
/// </summary>
public interface IRectTransform : IRuntimeComponent
{
    /// <summary>
    /// Gets or sets the position in parent space.
    /// </summary>
    Vector2D<float> Position { get; set; }

    /// <summary>
    /// Gets or sets the size (width, height).
    /// </summary>
    Vector2D<float> Size { get; set; }

    /// <summary>
    /// Gets or sets the rotation in radians (clockwise).
    /// </summary>
    float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the scale factor.
    /// </summary>
    Vector2D<float> Scale { get; set; }

    /// <summary>
    /// Gets or sets the normalized pivot point (0-1).
    /// </summary>
    Vector2D<float> Pivot { get; set; }

    /// <summary>
    /// Calculates the screen-space bounding box.
    /// </summary>
    Rectangle<int> GetBounds();
}
