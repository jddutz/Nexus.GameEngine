namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children vertically.
/// Child components are positioned one above the other with configurable spacing and alignment.
/// </summary>
public partial class VerticalLayout : Container
{
    public VerticalLayout(IDescriptorManager descriptorManager) : base(descriptorManager) { }

    /// <summary>
    /// Fixed height for each child item in the vertical layout.
    /// If 0, children use their measured height.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private int _itemHeight = 0;

    /// <summary>
    /// Arranges child components vertically with fixed height and full width.
    /// Each child receives constraints with ItemHeight (or measured height if ItemHeight is 0)
    /// and the full content area width. Children position themselves within these constraints
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

        var y = contentArea.Origin.Y;

        // Arrange children vertically
        foreach (var child in children)
        {
            // Determine child height: use ItemHeight if set, otherwise measure
            var h = ItemHeight > 0 ? ItemHeight : child.Measure(contentArea.Size).Y;
            
            // Give child full width of content area
            var w = contentArea.Size.X;

            // Set child constraints - child will position itself using its AnchorPoint
            var childConstraints = new Rectangle<int>(contentArea.Origin.X, y, w, h);
            child.SetSizeConstraints(childConstraints);

            // Move to next position
            y += h + (int)Spacing.Y;
        }
    }
}
