namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children in a grid pattern.
/// Child components are positioned in rows and columns with configurable spacing and alignment.
/// </summary>
public partial class GridLayout : Container
{
    public GridLayout(IDescriptorManager descriptorManager) : base(descriptorManager) { }
    /// <summary>
    /// Number of columns in the grid.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private int _columnCount = 1;

    /// <summary>
    /// Number of rows in the grid. If 0, rows are calculated automatically.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private int _rowCount = 0;

    /// <summary>
    /// Number of rows in the grid. If 0, rows are calculated automatically.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private Vector2D<int> _spacing = new Vector2D<int>(0, 0);

    /// <summary>
    /// Whether grid cells should have uniform size based on the largest child component.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private bool _uniformCellSize = false;

    /// <summary>
    /// Whether to maintain cell aspect ratio.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private bool _maintainCellAspectRatio = false;

    /// <summary>
    /// Cell aspect ratio (width / height). Default 1.0 for square cells.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private float _cellAspectRatio = 1.0f;

    /// <summary>
    /// Arranges child components in a grid pattern with configurable spacing and alignment.
    /// Minimal implementation: evenly divide the content area into cells and assign each
    /// child the full cell size. More advanced behavior will be added later.
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

        var cols = Math.Max(1, ColumnCount);
        var rows = RowCount > 0 ? RowCount : (int)Math.Ceiling((double)children.Length / cols);

        var availableWidth = Math.Max(0, contentArea.Size.X - (cols - 1) * (int)Spacing.X);
        var availableHeight = Math.Max(0, contentArea.Size.Y - (rows - 1) * (int)Spacing.Y);

        var cellWidth = availableWidth / cols;
        var cellHeight = rows > 0 ? (availableHeight / rows) : availableHeight;

        for (int i = 0; i < children.Length; i++)
        {
            var child = children[i];
            int row = i / cols;
            int col = i % cols;

            int x = contentArea.Origin.X + col * (cellWidth + (int)Spacing.X);
            int y = contentArea.Origin.Y + row * (cellHeight + (int)Spacing.Y);

            var newConstraints = new Rectangle<int>(x, y, cellWidth, cellHeight);
            child.SetSizeConstraints(newConstraints);
        }
    }
}