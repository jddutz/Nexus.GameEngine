namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children horizontally.
/// Child components are positioned side by side with configurable spacing and alignment.
/// </summary>
public partial class HorizontalLayout(IDescriptorManager descriptorManager)
    : Layout(descriptorManager)
{
    /// <summary>
    /// Vertical alignment of child components within the layout.
    /// </summary>
    public VerticalAlignment Alignment { get; set; } = VerticalAlignment.Center;

    /// <summary>
    /// Spacing between child components in pixels.
    /// </summary>
    public int Spacing { get; set; } = 0;

    /// <summary>
    /// Arranges child components horizontally with spacing and alignment.
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

        var pX = 0;

        // Arrange children
        foreach (var child in children)
        {
            var childBounds = child.GetBounds();
            
            // TODO: Calculate X based on alignment and LayoutMode
            var x = pX;

            // TODO: Calculate Y based on alignment and LayoutMode
            var y = Alignment switch
            {
                VerticalAlignment.Top => Padding.Top,
                VerticalAlignment.Center => Padding.Top + (maxHeight - childBounds.Size.Y) / 2,
                VerticalAlignment.Bottom => Padding.Top + maxHeight - childBounds.Size.Y,
                VerticalAlignment.Stretch => Padding.Top,
                _ => Padding.Top
            };

            // TODO: Calculate W based on LayoutMode
            var w = childBounds.Size.X;

            // TODO: Calculate H based on LayoutMode
            var h = childBounds.Size.Y;

            // Set child constraints
            var newConstraints = new Rectangle<int>(x, y, w, h);
            child.SetSizeConstraints(newConstraints);

            // Move to next position
            pX += childBounds.Size.X + Spacing;
        }
    }
}
