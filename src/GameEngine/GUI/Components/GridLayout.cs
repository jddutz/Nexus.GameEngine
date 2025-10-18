using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI.Abstractions;
using Nexus.GameEngine.Runtime;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Components;

/// <summary>
/// A layout component that arranges its children in a grid pattern.
/// Child components are positioned in rows and columns with configurable spacing and alignment.
/// </summary>
public partial class GridLayout(IWindowService windowService)
    : LayoutBase(windowService)
{
    /// <summary>
    /// Template for configuring GridLayout components.
    /// Defines the properties for arranging child components in a grid pattern.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Number of columns in the grid.
        /// </summary>
        public int Columns { get; init; } = 1;

        /// <summary>
        /// Number of rows in the grid. If 0, rows are calculated automatically based on child count and columns.
        /// </summary>
        public int Rows { get; init; } = 0;

        /// <summary>
        /// Horizontal spacing between grid cells in pixels.
        /// </summary>
        public float ColumnSpacing { get; init; } = 0f;

        /// <summary>
        /// Vertical spacing between grid cells in pixels.
        /// </summary>
        public float RowSpacing { get; init; } = 0f;

        /// <summary>
        /// Horizontal alignment of child components within their grid cells.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment { get; init; } = HorizontalAlignment.Center;

        /// <summary>
        /// Vertical alignment of child components within their grid cells.
        /// </summary>
        public VerticalAlignment VerticalAlignment { get; init; } = VerticalAlignment.Center;

        /// <summary>
        /// Padding around the entire grid container.
        /// </summary>
        public Padding Padding { get; init; } = Padding.Zero;

        /// <summary>
        /// Whether grid cells should have uniform size based on the largest child component.
        /// </summary>
        public bool UniformCellSize { get; init; } = false;
    }

    /// <summary>
    /// Number of columns in the grid.
    /// </summary>
    public int Columns { get; set; } = 1;

    /// <summary>
    /// Number of rows in the grid. If 0, rows are calculated automatically.
    /// </summary>
    public int Rows { get; set; } = 0;

    /// <summary>
    /// Horizontal spacing between grid cells in pixels.
    /// </summary>
    public float ColumnSpacing { get; set; } = 0f;

    /// <summary>
    /// Vertical spacing between grid cells in pixels.
    /// </summary>
    public float RowSpacing { get; set; } = 0f;

    /// <summary>
    /// Horizontal alignment of child components within their grid cells.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;

    /// <summary>
    /// Vertical alignment of child components within their grid cells.
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;

    /// <summary>
    /// Padding around the entire grid container.
    /// </summary>
    public Padding Padding { get; set; } = Padding.Zero;

    /// <summary>
    /// Whether grid cells should have uniform size based on the largest child component.
    /// </summary>
    public bool UniformCellSize { get; set; } = false;

    /// <summary>
    /// Configure this GridLayout using the specified template.
    /// </summary>
    /// <param name="template">Template containing grid layout configuration</param>
    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            Columns = template.Columns;
            Rows = template.Rows;
            ColumnSpacing = template.ColumnSpacing;
            RowSpacing = template.RowSpacing;
            HorizontalAlignment = template.HorizontalAlignment;
            VerticalAlignment = template.VerticalAlignment;
            Padding = template.Padding;
            UniformCellSize = template.UniformCellSize;
            InvalidateLayout();
        }
    }

    /// <summary>
    /// Arranges child components in a grid pattern with configurable spacing and alignment.
    /// </summary>
    /// <param name="children">Collection of layoutable child components to arrange</param>
    protected override void OnLayout(IReadOnlyList<ILayoutable> children)
    {
        if (children.Count == 0 || Columns <= 0)
            return;

        // Calculate actual number of rows
        int actualRows = Rows > 0 ? Rows : (int)Math.Ceiling((double)children.Count / Columns);

        // Calculate cell dimensions
        Vector2D<float> cellSize;
        if (UniformCellSize)
        {
            // Use the size of the largest child component
            var maxChildBounds = children.Select(c => c.GetBounds()).ToList();
            float maxWidth = maxChildBounds.Max(b => b.Size.X);
            float maxHeight = maxChildBounds.Max(b => b.Size.Y);
            cellSize = new Vector2D<float>(maxWidth, maxHeight);
        }
        else
        {
            // Calculate available space and divide by grid dimensions
            var availableSize = GetAvailableSize();
            float availableWidth = Math.Max(0, availableSize.X - Padding.Left - Padding.Right - (Columns - 1) * ColumnSpacing);
            float availableHeight = Math.Max(0, availableSize.Y - Padding.Top - Padding.Bottom - (actualRows - 1) * RowSpacing);
            cellSize = new Vector2D<float>(availableWidth / Columns, availableHeight / actualRows);
        }

        // Position each child in its grid cell
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var childBounds = child.GetBounds();

            // Calculate grid position
            int row = i / Columns;
            int col = i % Columns;

            // Calculate cell position
            float cellX = Padding.Left + col * (cellSize.X + ColumnSpacing);
            float cellY = Padding.Top + row * (cellSize.Y + RowSpacing);

            // Calculate child position within cell based on alignment
            float childX = HorizontalAlignment switch
            {
                HorizontalAlignment.Left => cellX,
                HorizontalAlignment.Center => cellX + (cellSize.X - childBounds.Size.X) / 2,
                HorizontalAlignment.Right => cellX + cellSize.X - childBounds.Size.X,
                HorizontalAlignment.Stretch => cellX, // TODO: Resize child to fit cell
                _ => cellX
            };

            float childY = VerticalAlignment switch
            {
                VerticalAlignment.Top => cellY,
                VerticalAlignment.Center => cellY + (cellSize.Y - childBounds.Size.Y) / 2,
                VerticalAlignment.Bottom => cellY + cellSize.Y - childBounds.Size.Y,
                VerticalAlignment.Stretch => cellY, // TODO: Resize child to fit cell
                _ => cellY
            };

            // Handle stretch alignment by resizing the child
            Vector2D<float> childSize = childBounds.Size;
            if (HorizontalAlignment == HorizontalAlignment.Stretch)
                childSize = childSize with { X = cellSize.X };
            if (VerticalAlignment == VerticalAlignment.Stretch)
                childSize = childSize with { Y = cellSize.Y };

            // Set child bounds
            child.SetBounds(new Rectangle<float>(childX, childY, childSize.X, childSize.Y));
        }
    }
}