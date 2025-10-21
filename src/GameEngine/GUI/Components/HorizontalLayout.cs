using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI.Abstractions;
using Nexus.GameEngine.Runtime;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// A layout component that arranges its children horizontally.
/// Child components are positioned side by side with configurable spacing and alignment.
/// </summary>
public partial class HorizontalLayout(IWindowService windowService)
    : LayoutBase(windowService)
{

    /// <summary>
    /// Template for configuring HorizontalLayout components.
    /// Defines the properties for arranging child components horizontally.
    /// </summary>
    public new record Template : LayoutBase.Template
    {
        /// <summary>
        /// Vertical alignment of child components within the layout.
        /// </summary>
        public VerticalAlignment Alignment { get; init; } = VerticalAlignment.Center;

        /// <summary>
        /// Spacing between child components in pixels.
        /// </summary>
        public float Spacing { get; init; } = 0f;
    }

    /// <summary>
    /// Vertical alignment of child components within the layout.
    /// </summary>
    public VerticalAlignment Alignment { get; set; } = VerticalAlignment.Center;

    /// <summary>
    /// Spacing between child components in pixels.
    /// </summary>
    public float Spacing { get; set; } = 0f;

    /// <summary>
    /// Padding around the layout container.
    /// </summary>
    public Padding Padding { get; set; } = Padding.Zero;

    /// <summary>
    /// Configure this HorizontalLayout using the specified template.
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
    /// Arranges child components horizontally with spacing and alignment.
    /// </summary>
    /// <param name="children">Collection of UI child components to arrange</param>
    protected override void OnLayout(IReadOnlyList<UserInterfaceComponent> children)
    {
        if (children.Count == 0)
            return;

        float currentX = Padding.Left;
        float maxHeight = children.Max(c => c.GetBounds().Size.Y);

        foreach (var child in children)
        {
            var childBounds = child.GetBounds();

            // Calculate Y position based on alignment
            float y = Alignment switch
            {
                VerticalAlignment.Top => Padding.Top,
                VerticalAlignment.Center => Padding.Top + (maxHeight - childBounds.Size.Y) / 2,
                VerticalAlignment.Bottom => Padding.Top + maxHeight - childBounds.Size.Y,
                _ => Padding.Top
            };

            // Set child bounds
            child.SetBounds(new Rectangle<float>(currentX, y, childBounds.Size.X, childBounds.Size.Y));

            // Move to next position
            currentX += childBounds.Size.X + Spacing;
        }
    }
}
