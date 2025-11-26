using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Interface for components that have 2D spatial properties (Position, Size, Rotation, Scale).
/// Designed for UI elements and 2D game objects.
/// Coordinate system: Top-Left origin (0,0), +X Right, +Y Down.
/// </summary>
public interface IRectTransform
{
    /// <summary>
    /// Position in parent space (or screen space if no parent).
    /// </summary>
    Vector2D<float> Position { get; }

    /// <summary>
    /// Size of the rectangle in pixels.
    /// </summary>
    Vector2D<float> Size { get; }

    /// <summary>
    /// Rotation in radians (clockwise).
    /// </summary>
    float Rotation { get; }

    /// <summary>
    /// Scale factor (default 1,1).
    /// </summary>
    Vector2D<float> Scale { get; }

    /// <summary>
    /// Normalized pivot point (0-1).
    /// (0,0) = Top-Left, (0.5,0.5) = Center, (1,1) = Bottom-Right.
    /// </summary>
    Vector2D<float> Pivot { get; }

    /// <summary>
    /// Local transformation matrix.
    /// </summary>
    Matrix4X4<float> LocalMatrix { get; }

    /// <summary>
    /// World transformation matrix (absolute).
    /// </summary>
    Matrix4X4<float> WorldMatrix { get; }

    void SetPosition(Vector2D<float> position);
    void SetSize(Vector2D<float> size);
    void SetRotation(float radians);
    void SetScale(Vector2D<float> scale);
    void SetPivot(Vector2D<float> pivot);

    /// <summary>
    /// Calculates the screen-space bounding box.
    /// </summary>
    Rectangle<int> GetBounds();
}
