namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children vertically.
/// Child components are positioned one above the other with configurable spacing and alignment.
/// </summary>
public partial class VerticalLayout(IDescriptorManager descriptorManager)
    : Layout(descriptorManager)
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
            var childBounds = child.GetBounds();
            
            // TODO: Calculate X based on alignment and LayoutMode
            var x = Alignment switch
            {
                HorizontalAlignment.Left => Padding.Left,
                HorizontalAlignment.Center => Padding.Left + (maxWidth - childBounds.Size.X) / 2,
                HorizontalAlignment.Right => Padding.Left + maxWidth - childBounds.Size.X,
                HorizontalAlignment.Stretch => Padding.Left,
                _ => Padding.Left
            };

            // TODO: Calculate Y based on alignment and LayoutMode
            var y = pY;

            // TODO: Calculate W based on LayoutMode
            var w = childBounds.Size.X;

            // TODO: Calculate H based on LayoutMode
            var h = childBounds.Size.Y;

            // Set child constraints
            var newConstraints = new Rectangle<int>(x, y, w, h);
            child.SetSizeConstraints(newConstraints);

            // Move to next position
            pY += childBounds.Size.Y + Spacing;
        }
    }
}
