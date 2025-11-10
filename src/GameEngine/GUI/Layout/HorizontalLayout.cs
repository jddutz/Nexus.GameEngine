namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children horizontally.
/// Child components are positioned side by side with configurable spacing and alignment.
/// </summary>
public partial class HorizontalLayout : Container
{
    public HorizontalLayout(IDescriptorManager descriptorManager) : base(descriptorManager) { }
    /// <summary>
    /// Vertical alignment of child components within the layout.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private float _alignment = VerticalAlignment.Center;

    // If true, children will be stretched vertically to fill the content height.
    // This avoids checking for a non-existent VerticalAlignment.Stretch value.
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

            // Calculate Y based on alignment - use content area height for centering
            var y = Alignment switch
            {
                VerticalAlignment.Top => contentArea.Origin.Y,
                VerticalAlignment.Center => contentArea.Origin.Y + (contentArea.Size.Y - measured.Y) / 2,
                VerticalAlignment.Bottom => contentArea.Origin.Y + contentArea.Size.Y - measured.Y,
                _ => contentArea.Origin.Y
            };

            var w = measured.X;
            var h = _stretchChildren ? contentArea.Size.Y : measured.Y;

            // Set child constraints
            var newConstraints = new Rectangle<int>(x, y, w, h);
            child.SetSizeConstraints(newConstraints);

            // Move to next position
            pX += measured.X + (int)Spacing.X;
        }
    }
}
