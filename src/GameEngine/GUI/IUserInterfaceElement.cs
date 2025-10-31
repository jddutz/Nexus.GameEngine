namespace Nexus.GameEngine.GUI;

/// <summary>
/// Interface for user interface components supporting 2D positioning, sizing, and bounds management in NDC space.
/// </summary>
public interface IUserInterfaceElement : IDrawable
{
    /// <summary>
    /// Gets the origin (top-left corner) of the component in pixel space.
    /// </summary>
    Vector2D<int> Origin { get; }

    /// <summary>
    /// Sets the origin (top-left corner) of the component in pixel space.
    /// </summary>
    /// <param name="origin">The new origin.</param>
    void SetOrigin(Vector2D<int> origin);

    /// <summary>
    /// Called when the origin changes.
    /// </summary>
    /// <param name="oldValue">The previous origin value.</param>
    void OnOriginChanged(Vector2D<int> oldValue);

    /// <summary>
    /// Gets the size of the component in pixels.
    /// </summary>
    Vector2D<int> Size { get; }

    /// <summary>
    /// Sets the size of the component in pixels.
    /// </summary>
    /// <param name="size">The new size.</param>
    void SetSize(Vector2D<int> size);

    /// <summary>
    /// Called when the size changes.
    /// </summary>
    /// <param name="oldValue">The previous size value.</param>
    void OnSizeChanged(Vector2D<int> oldValue);

    /// <summary>
    /// Updates the geometry of the component (e.g., after bounds change).
    /// </summary>
    void UpdateGeometry();

    /// <summary>
    /// Sets the size constraints for this element (the available space it can occupy).
    /// The element uses these constraints to determine its actual size and position.
    /// </summary>
    /// <param name="constraints">The rectangle defining available space (position + size in pixels).</param>
    void SetSizeConstraints(Rectangle<int> constraints);
}