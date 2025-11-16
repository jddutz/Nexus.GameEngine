namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children horizontally.
/// Child components are positioned side by side with configurable spacing and alignment.
/// </summary>
public partial class HorizontalLayout : Container
{
    public HorizontalLayout(IDescriptorManager descriptorManager) : base(descriptorManager) { }

    // If true, children will be stretched vertically to fill the content height.
    // This avoids checking for a non-existent Align.Stretch value.
    [ComponentProperty]
    [TemplateProperty]
    private bool _stretchChildren = false;

    public void SetStretchChildren(bool value) => _stretchChildren = value;

    /// <summary>
    /// Arranges child components horizontally with spacing and alignment.
    /// </summary>
    protected override void UpdateLayout()
    {
        var children = GetChildren<IComponent>().OfType<IUserInterfaceElement>().ToArray();
        if (children.Length == 0) return;

        var contentArea = GetContentArea();

        var pX = contentArea.Origin.X;

        // Arrange children
        foreach (var child in children)
        {
            var measured = child.Measure(contentArea.Size);

            var x = pX;

            var w = measured.X;
            var h = _stretchChildren ? contentArea.Size.Y : measured.Y;

            // Calculate Y based on alignment.Y (-1..1) using final height
            var align = Alignment.Y; // -1 top, 0 center, 1 bottom
            var alignFrac = (align + 1.0f) * 0.5f; // 0..1 fraction
            var y = contentArea.Origin.Y + (int)((contentArea.Size.Y - h) * alignFrac);

            // Set child constraints
            var newConstraints = new Rectangle<int>(x, y, w, h);
            child.SetSizeConstraints(newConstraints);

            // Move to next position
            pX += measured.X + (int)Spacing.X;
        }
    }
}
