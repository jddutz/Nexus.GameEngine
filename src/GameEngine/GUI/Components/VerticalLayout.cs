using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI.Abstractions;
using Nexus.GameEngine.Runtime;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// A layout component that arranges its children vertically.
/// Child components are positioned one above the other with configurable spacing and alignment.
/// </summary>
public partial class VerticalLayout(IWindowService windowService)
    : LayoutBase(windowService)
{

    /// <summary>
    /// Template for configuring VerticalLayout components.
    /// Defines the properties for arranging child components vertically.
    /// </summary>
    public new record Template : LayoutBase.Template
    {
        /// <summary>
        /// Horizontal alignment of child components within the layout.
        /// </summary>
        public HorizontalAlignment Alignment { get; init; } = HorizontalAlignment.Center;

        /// <summary>
        /// Spacing between child components in pixels.
        /// </summary>
        public float Spacing { get; init; } = 0f;
    }

    /// <summary>
    /// Horizontal alignment of child components within the layout.
    /// </summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Center;

    /// <summary>
    /// Spacing between child components in pixels.
    /// </summary>
    public float Spacing { get; set; } = 0f;

    /// <summary>
    /// Padding around the layout container.
    /// </summary>
    public Padding Padding { get; set; } = Padding.Zero;

    /// <summary>
    /// Configure this VerticalLayout using the specified template.
    /// </summary>
    /// <param name="template">Template containing layout configuration</param>
    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            Alignment = template.Alignment;
            Spacing = template.Spacing;
            Padding = template.Padding;
            InvalidateLayout();
        }
    }

    /// <summary>
    /// Arranges child components vertically with spacing and alignment.
    /// </summary>
    /// <param name="children">Collection of UI child components to arrange</param>
    protected override void OnLayout(IReadOnlyList<UserInterfaceComponent> children)
    {
        if (children.Count == 0)
            return;

        float currentY = Padding.Top;
        float maxWidth = children.Max(c => c.GetBounds().Size.X);

        foreach (var child in children)
        {
            var childBounds = child.GetBounds();

            // Calculate X position based on alignment
            float x = Alignment switch
            {
                HorizontalAlignment.Left => Padding.Left,
                HorizontalAlignment.Center => Padding.Left + (maxWidth - childBounds.Size.X) / 2,
                HorizontalAlignment.Right => Padding.Left + maxWidth - childBounds.Size.X,
                _ => Padding.Left
            };

            // Set child bounds
            child.SetBounds(new Rectangle<float>(x, currentY, childBounds.Size.X, childBounds.Size.Y));

            // Move to next position
            currentY += childBounds.Size.Y + Spacing;
        }
    }
}
