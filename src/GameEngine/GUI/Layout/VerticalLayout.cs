namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children vertically.
/// Child components are positioned one above the other with configurable spacing and alignment.
/// </summary>
public partial class VerticalLayout
    : Layout
{
    /// <summary>
    /// Horizontal alignment of child components within the layout.
    /// </summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Center;

    /// <summary>
    /// Spacing between child components in pixels.
    /// </summary>
    public int Spacing { get; set; } = 0;

    /// <summary>
    /// Arranges child components vertically with spacing and alignment.
    /// </summary>
    /// <param name="children">Collection of UI child components to arrange</param>
    protected override void UpdateLayout()
    {
        var children = GetChildren<Element>().ToArray();

        // Measure children
        int maxHeight = 0;
        int maxWidth = 0;
        foreach (var child in children)
        {
            if (child.Size.X > maxWidth) maxWidth = child.Size.X;
            if (child.Size.Y > maxHeight) maxHeight = child.Size.Y;
        }

        var pY = 0;

        // Arrange children
        foreach (var child in children)
        {
            // TODO: Calculate X based on alignment and LayoutMode
            var x = Alignment switch
            {
                HorizontalAlignment.Left => Padding.Left,
                HorizontalAlignment.Center => Padding.Left + (maxWidth - child.Bounds.Size.X) / 2,
                HorizontalAlignment.Right => Padding.Left + maxWidth - child.Bounds.Size.X,
                HorizontalAlignment.Stretch => Padding.Left,
                _ => Padding.Left
            };

            // TODO: Calculate Y based on alignment and LayoutMode
            var y = pY;

            // TODO: Calculate W based on LayoutMode
            var w = child.Bounds.Size.X;

            // TODO: Calculate H based on LayoutMode
            var h = child.Bounds.Size.Y;

            // Set child bounds
            var newBounds = new Rectangle<int>(x, y, w, h);
            child.SetBounds(newBounds);

            // Move to next position
            pY += child.Bounds.Size.Y + Spacing;
        }
    }
}
