namespace Nexus.GameEngine.GUI;

/// <summary>
/// Interface for user interface components supporting 2D positioning, sizing, and anchor-based layout.
/// Uses Position/AnchorPoint/Size model where Position defines where the AnchorPoint is located in screen space.
/// </summary>
public interface IUserInterfaceElement : IDrawable
{
    /// <summary>
    /// Gets the position of the anchor point in pixel space.
    /// </summary>
    Vector3D<float> Position { get; }

    /// <summary>
    /// Gets the anchor point in normalized space (-1 to 1).
    /// (-1,-1) = top-left, (0,0) = center, (1,1) = bottom-right.
    /// </summary>
    Vector2D<float> AnchorPoint { get; }

    /// <summary>
    /// Gets the size of the component in pixels.
    /// </summary>
    Vector2D<int> Size { get; }

    /// <summary>
    /// Sets the position (where the anchor point is located).
    /// </summary>
    /// <param name="position">The new position in pixel space.</param>
    /// <param name="duration">Optional animation duration.</param>
    /// <param name="interpolation">Optional interpolation mode.</param>
    void SetPosition(Vector3D<float> position, float duration = 0f, InterpolationMode interpolation = InterpolationMode.Step);

    /// <summary>
    /// Sets the anchor point in normalized space (-1 to 1).
    /// </summary>
    /// <param name="anchorPoint">The new anchor point.</param>
    /// <param name="duration">Optional animation duration.</param>
    /// <param name="interpolation">Optional interpolation mode.</param>
    void SetAnchorPoint(Vector2D<float> anchorPoint, float duration = 0f, InterpolationMode interpolation = InterpolationMode.Step);

    /// <summary>
    /// Sets the size of the component in pixels.
    /// </summary>
    /// <param name="size">The new size.</param>
    /// <param name="duration">Optional animation duration.</param>
    /// <param name="interpolation">Optional interpolation mode.</param>
    void SetSize(Vector2D<int> size, float duration = 0f, InterpolationMode interpolation = InterpolationMode.Step);

    /// <summary>
    /// Computes the bounding rectangle from Position, AnchorPoint, and Size.
    /// </summary>
    /// <returns>The bounding rectangle in pixel space.</returns>
    Rectangle<int> GetBounds();

    /// <summary>
    /// Sets the size constraints for this element (the available space it can occupy).
    /// The element uses these constraints to determine its actual size and position.
    /// </summary>
    /// <param name="constraints">The rectangle defining available space (position + size in pixels).</param>
    void SetSizeConstraints(Rectangle<int> constraints);
}