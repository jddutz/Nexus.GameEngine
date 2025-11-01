namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children horizontally.
/// Child components are positioned side by side with configurable spacing and alignment.
/// </summary>
public partial class HorizontalLayout(
    IPipelineManager pipelineManager)
    : Layout(pipelineManager)
{

    /// <summary>
    /// Template for configuring HorizontalLayout components.
    /// Defines the properties for arranging child components horizontally.
    /// </summary>
    public new record Template : Layout.Template
    {
        /// <summary>
        /// Vertical alignment of child components within the layout.
        /// </summary>
        public VerticalAlignment Alignment { get; init; } = VerticalAlignment.Center;

        /// <summary>
        /// Spacing between child components in pixels.
        /// </summary>
        public int Spacing { get; init; } = 0;
    }

    /// <summary>
    /// Vertical alignment of child components within the layout.
    /// </summary>
    public VerticalAlignment Alignment { get; set; } = VerticalAlignment.Center;

    /// <summary>
    /// Spacing between child components in pixels.
    /// </summary>
    public int Spacing { get; set; } = 0;

    /// <summary>
    /// Configure this HorizontalLayout using the specified template.
    /// </summary>
    /// <param name="template">Template containing layout configuration</param>
    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        base.OnLoad(componentTemplate);

        if (componentTemplate is Template template)
        {
            Alignment = template.Alignment;
            Spacing = template.Spacing;
        }
    }

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
            // TODO: Calculate X based on alignment and LayoutMode
            var x = pX;

            // TODO: Calculate Y based on alignment and LayoutMode
            var y = Alignment switch
            {
                VerticalAlignment.Top => Padding.Top,
                VerticalAlignment.Center => Padding.Top + (maxHeight - child.Bounds.Size.Y) / 2,
                VerticalAlignment.Bottom => Padding.Top + maxHeight - child.Bounds.Size.Y,
                VerticalAlignment.Stretch => Padding.Top,
                _ => Padding.Top
            };

            // TODO: Calculate W based on LayoutMode
            var w = child.Bounds.Size.X;

            // TODO: Calculate H based on LayoutMode
            var h = child.Bounds.Size.Y;

            // Set child bounds
            var newBounds = new Rectangle<int>(x, y, w, h);
            child.SetBounds(newBounds);

            // Move to next position
            pX += child.Bounds.Size.X + Spacing;
        }
    }
}
