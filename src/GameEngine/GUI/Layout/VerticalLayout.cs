namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children vertically.
/// Child components are positioned one above the other with configurable spacing and alignment.
/// </summary>
public partial class VerticalLayout : Container
{
    public VerticalLayout(IDescriptorManager descriptorManager) : base(descriptorManager) { }

    // If true, children will be stretched horizontally to fill the content width.
    // This replaces the previous use of an Alignment.Stretch enum value so layouts
    // don't need to query child sizing policy directly.
    [ComponentProperty]
    [TemplateProperty]
    private bool _stretchChildren = false;

    /// <summary>
    /// Set whether children should be stretched horizontally.
    /// Provided as a convenience for tests and templates; updated values are applied
    /// during the normal component update cycle.
    /// </summary>
    public void SetStretchChildren(bool value) => _stretchChildren = value;

    /// <summary>
    /// Arranges child components vertically with spacing and alignment.
    /// </summary>
    protected override void UpdateLayout()
    {
        var children = GetChildren<IComponent>().OfType<IUserInterfaceElement>().ToArray();
        if (children.Length == 0) return;

        var contentArea = GetContentArea();

        var pY = contentArea.Origin.Y;

        // Arrange children
        foreach (var child in children)
        {
            var measured = child.Measure(contentArea.Size);

            // Determine final width first (stretch or measured)
            var w = _stretchChildren ? contentArea.Size.X : measured.X;
            var h = measured.Y;

            // Calculate X based on numeric alignment (-1..1) using final width
            var align = Alignment.X; // -1 left, 0 center, 1 right
            var alignFrac = (align + 1.0f) * 0.5f; // 0..1 fraction
            var x = contentArea.Origin.X + (int)((contentArea.Size.X - w) * alignFrac);

            var y = pY;

            // Set child constraints
            var newConstraints = new Rectangle<int>(x, y, w, h);
            child.SetSizeConstraints(newConstraints);

            // Move to next position
            pY += measured.Y + (int)Spacing.Y;
        }
    }
}
