namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children horizontally.
/// Child components are positioned side by side with configurable spacing and alignment.
/// </summary>
public partial class HorizontalLayout : Container
{
    public HorizontalLayout(IDescriptorManager descriptorManager) : base(descriptorManager) { }

    /// <summary>
    /// Fixed width for each child item in the horizontal layout.
    /// If 0, children use their measured width.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private int _itemWidth = 0;

    /// <summary>
    /// Arranges child components horizontally with fixed width and full height.
    /// Each child receives constraints with ItemWidth (or measured width if ItemWidth is 0)
    /// and the full content area height. Children position themselves within these constraints
    /// using their own AnchorPoint.
    /// </summary>
    protected override void UpdateLayout()
    {
        var children = GetChildren<IComponent>().OfType<IUserInterfaceElement>().ToArray();
        if (children.Length == 0) return;

        var contentArea = new Rectangle<int>(
            (int)(TargetPosition.X - (1.0f + AnchorPoint.X) * TargetSize.X * 0.5f) + Padding.Left,
            (int)(TargetPosition.Y - (1.0f + AnchorPoint.Y) * TargetSize.Y * 0.5f) + Padding.Top,
            Math.Max(0, TargetSize.X - Padding.Left - Padding.Right),
            Math.Max(0, TargetSize.Y - Padding.Top - Padding.Bottom)
        );

        var x = contentArea.Origin.X;

        // Arrange children horizontally
        foreach (var child in children)
        {
            // Determine child width: use ItemWidth if set, otherwise measure
            var w = ItemWidth > 0 ? ItemWidth : child.Measure(contentArea.Size).X;
            
            // Give child full height of content area
            var h = contentArea.Size.Y;

            // Set child constraints - child will position itself using its AnchorPoint
            var childConstraints = new Rectangle<int>(x, contentArea.Origin.Y, w, h);
            child.SetSizeConstraints(childConstraints);

            // Move to next position
            x += w + (int)Spacing.X;
        }
    }
}
