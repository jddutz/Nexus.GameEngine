namespace Nexus.GameEngine.GUI.Layout;
using System.Linq;

/// <summary>
/// A layout component that arranges its children vertically.
/// Child components are positioned one above the other with configurable spacing and alignment.
/// </summary>
public partial class VerticalLayout : Container
{
    public VerticalLayout(IDescriptorManager descriptorManager) : base(descriptorManager) { }
    /// <summary>
    /// Horizontal alignment of child components within the layout.
    /// </summary>
    [ComponentProperty(BeforeChange = nameof(CancelAnimation))]
    [TemplateProperty]
    private HorizontalAlignment _alignment = HorizontalAlignment.Center;

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

            // Calculate X based on alignment - use content area width for centering
            var x = Alignment switch
            {
                HorizontalAlignment.Left => contentArea.Origin.X,
                HorizontalAlignment.Center => contentArea.Origin.X + (contentArea.Size.X - measured.X) / 2,
                HorizontalAlignment.Right => contentArea.Origin.X + contentArea.Size.X - measured.X,
                HorizontalAlignment.Stretch => contentArea.Origin.X,
                _ => contentArea.Origin.X
            };

            var y = pY;
            var w = Alignment == HorizontalAlignment.Stretch ? contentArea.Size.X : measured.X;
            var h = measured.Y;

            // Set child constraints
            var newConstraints = new Rectangle<int>(x, y, w, h);
            child.SetSizeConstraints(newConstraints);

            // Move to next position
            pY += measured.Y + (int)Spacing.Y;
        }
    }
}
