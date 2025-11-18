namespace Nexus.GameEngine.GUI;

/// <summary>
/// Interface for user interface components supporting 2D positioning, sizing, and anchor-based layout.
/// Uses Position/AnchorPoint/Size model where Position defines where the AnchorPoint is located in screen space.
/// </summary>
public interface IUserInterfaceElement : ITransformable, IComponent
{
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
    /// Measures the desired size of the element without an available size.
    /// Layouts call this method to determine the intrinsic size of each child.
    /// </summary>
    /// <returns>The desired size (width, height) in pixels.</returns>
    Vector2D<int> Measure();

    /// <summary>
    /// Measures the desired size of the element given an available size.
    /// Layouts call this to ask children how large they would like to be when
    /// constrained by available space.
    /// </summary>
    /// <param name="availableSize">The available space in pixels (width, height).</param>
    /// <returns>The desired size (width, height) in pixels.</returns>
    Vector2D<int> Measure(Vector2D<int> availableSize);

    /// <summary>
    /// Sets the size constraints for this element (the available space it can occupy).
    /// The element uses these constraints to determine its actual size and position.
    /// </summary>
    /// <param name="constraints">The rectangle defining available space (position + size in pixels).</param>
    void SetSizeConstraints(Rectangle<int> constraints);
}