using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics;

/// <summary>
/// Behavior interface for components that have spatial transformation properties.
/// Implement this interface for components that can be positioned, rotated, and scaled.
/// </summary>
public interface ITransformable
{
    /// <summary>
    /// The position of the component in world space or local space.
    /// For 2D components, typically uses X and Y. For 3D, consider using Vector3D<float>.
    /// </summary>
    Vector2D<float> Position { get; set; }

    /// <summary>
    /// The size or scale of the component.
    /// For 2D components, represents width and height. For 3D, consider using Vector3D<float>.
    /// </summary>
    Vector2D<float> Size { get; set; }

    /// <summary>
    /// The rotation of the component in radians.
    /// For 2D rotation around the Z-axis. For 3D, consider using Quaternion or Vector3D<float> for Euler angles.
    /// </summary>
    float Rotation { get; set; }

    /// <summary>
    /// The scale factor applied to the component.
    /// Values greater than 1 make the component larger, less than 1 make it smaller.
    /// </summary>
    Vector2D<float> Scale { get; set; }

    /// <summary>
    /// The origin point for rotation and scaling operations.
    /// Typically a value from (0,0) to (1,1) representing the anchor point.
    /// (0,0) = top-left, (0.5,0.5) = center, (1,1) = bottom-right
    /// </summary>
    Vector2D<float> Origin { get; set; }
}
