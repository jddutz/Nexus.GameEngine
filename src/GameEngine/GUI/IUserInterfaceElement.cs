using Nexus.GameEngine.GUI.Layout;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// Interface for user interface components supporting 2D positioning, sizing, and layout.
/// Inherits from IRectTransform for spatial properties.
/// </summary>
public interface IUserInterfaceElement : IRectTransform, IComponent
{
    // Layout Properties
    Vector2D<float> Alignment { get; }
    Vector2D<float> Offset { get; }
    Vector2D<float> MinSize { get; }
    Vector2D<float> MaxSize { get; }
    SizeMode HorizontalSizeMode { get; }
    SizeMode VerticalSizeMode { get; }
    Vector2D<float> RelativeSize { get; }
    Padding Padding { get; }
    SafeArea SafeArea { get; }

    /// <summary>
    /// Marks the layout as invalid, triggering a recalculation on the next update.
    /// </summary>
    void InvalidateLayout();

    /// <summary>
    /// Measures the desired size of the element without an available size.
    /// Layouts call this method to determine the intrinsic size of each child.
    /// </summary>
    /// <returns>The desired size (width, height) in pixels.</returns>
    Vector2D<float> Measure();

    /// <summary>
    /// Measures the desired size of the element given an available size.
    /// Layouts call this to ask children how large they would like to be when
    /// constrained by available space.
    /// </summary>
    /// <param name="availableSize">The available space in pixels (width, height).</param>
    /// <returns>The desired size (width, height) in pixels.</returns>
    Vector2D<float> Measure(Vector2D<float> availableSize);

    /// <summary>
    /// Updates the layout of the element and its children.
    /// This is called during the layout update phase before validation.
    /// </summary>
    void UpdateLayout();

    /// <summary>
    /// Sets the size constraints for this element (the available space it can occupy).
    /// The element uses these constraints to determine its actual size and position.
    /// </summary>
    /// <param name="constraints">The rectangle defining available space (position + size in pixels).</param>
    void UpdateLayout(Rectangle<float> constraints);
}