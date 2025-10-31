namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children in a grid pattern.
/// Child components are positioned in rows and columns with configurable spacing and alignment.
/// </summary>
public partial class GridLayout(
    IPipelineManager pipelineManager,
    IResourceManager resourceManager)
    : Layout(pipelineManager, resourceManager)
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
        public int ColumnSpacing { get; init; } = 0;

        /// <summary>
        /// Vertical spacing between grid cells in pixels.
        /// </summary>
        public int RowSpacing { get; init; } = 0;

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
    public int ColumnSpacing { get; set; } = 0;

    /// <summary>
    /// Vertical spacing between grid cells in pixels.
    /// </summary>
    public int RowSpacing { get; set; } = 0;

    /// <summary>
    /// Horizontal alignment of child components within their grid cells.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Center;

    /// <summary>
    /// Vertical alignment of child components within their grid cells.
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;

    /// <summary>
    /// Whether grid cells should have uniform size based on the largest child component.
    /// </summary>
    public bool UniformCellSize { get; set; } = false;

    /// <summary>
    /// Configure this GridLayout using the specified template.
    /// </summary>
    /// <param name="template">Template containing grid layout configuration</param>
    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        base.OnLoad(componentTemplate);

        if (componentTemplate is Template template)
        {
            Columns = template.Columns;
            Rows = template.Rows;
            ColumnSpacing = template.ColumnSpacing;
            RowSpacing = template.RowSpacing;
            HorizontalAlignment = template.HorizontalAlignment;
            VerticalAlignment = template.VerticalAlignment;
            UniformCellSize = template.UniformCellSize;
        }
    }

    /// <summary>
    /// Arranges child components in a grid pattern with configurable spacing and alignment.
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

        // Calculate actual number of rows
        int actualRows = Rows > 0 ? Rows : (int)Math.Ceiling((double)children.Length / Columns);

        // Calculate cell dimensions
        Vector2D<int> cellSize;
        if (UniformCellSize)
        {
            // Use the size of the largest child component
            cellSize = new Vector2D<int>(maxWidth, maxHeight);
        }
        else
        {
            // Calculate available space and divide by grid dimensions
            int availableWidth = Math.Max(0, Bounds.Size.X - Padding.Left - Padding.Right - (Columns - 1) * ColumnSpacing);
            int availableHeight = Math.Max(0, Bounds.Size.Y - Padding.Top - Padding.Bottom - (actualRows - 1) * RowSpacing);
            cellSize = new Vector2D<int>(availableWidth / Columns, availableHeight / actualRows);
        }

        // Position each child in its grid cell
        for (int i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var childBounds = child.Bounds;

            // Calculate grid position
            int row = i / Columns;
            int col = i % Columns;

            // Calculate cell position
            int cellX = Padding.Left + col * (cellSize.X + ColumnSpacing);
            int cellY = Padding.Top + row * (cellSize.Y + RowSpacing);

            // Calculate child position within cell based on alignment
            int x = HorizontalAlignment switch
            {
                HorizontalAlignment.Left => cellX,
                HorizontalAlignment.Center => cellX + (cellSize.X - childBounds.Size.X) / 2,
                HorizontalAlignment.Right => cellX + cellSize.X - childBounds.Size.X,
                HorizontalAlignment.Stretch => cellX, // TODO: Resize child to fit cell
                _ => cellX
            };

            int y = VerticalAlignment switch
            {
                VerticalAlignment.Top => cellY,
                VerticalAlignment.Center => cellY + (cellSize.Y - childBounds.Size.Y) / 2,
                VerticalAlignment.Bottom => cellY + cellSize.Y - childBounds.Size.Y,
                VerticalAlignment.Stretch => cellY, // TODO: Resize child to fit cell
                _ => cellY
            };

            var w = HorizontalAlignment == HorizontalAlignment.Stretch
                ? child.Bounds.Size.X
                : cellSize.X;
            
            var h = VerticalAlignment == VerticalAlignment.Stretch
                ? child.Bounds.Size.Y
                : cellSize.Y;

            // Set child bounds
            var newBounds = new Rectangle<int>(x, y, w, h);
            child.SetBounds(newBounds);
        }
    }
}