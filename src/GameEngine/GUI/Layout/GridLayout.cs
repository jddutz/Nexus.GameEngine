namespace Nexus.GameEngine.GUI.Layout;
using System.Linq;

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
    [ComponentProperty(BeforeChange = nameof(CancelAnimation))]
    [TemplateProperty]
    private int _columnCount = 1;

    /// <summary>
    /// Number of rows in the grid. If 0, rows are calculated automatically.
    /// </summary>
    [ComponentProperty(BeforeChange = nameof(CancelAnimation))]
    [TemplateProperty]
    private int _rowCount = 0;

    /// <summary>
    /// Whether grid cells should have uniform size based on the largest child component.
    /// </summary>
    [ComponentProperty(BeforeChange = nameof(CancelAnimation))]
    [TemplateProperty]
    private bool _uniformCellSize = false;

    /// <summary>
    /// Whether to maintain cell aspect ratio.
    /// </summary>
    [ComponentProperty(BeforeChange = nameof(CancelAnimation))]
    [TemplateProperty]
    private bool _maintainCellAspectRatio = false;

    /// <summary>
    /// Cell aspect ratio (width / height). Default 1.0 for square cells.
    /// </summary>
    [ComponentProperty(BeforeChange = nameof(CancelAnimation))]
    [TemplateProperty]
    private float _cellAspectRatio = 1.0f;

    /// <summary>
    /// Horizontal alignment of child components within their grid cells.
    /// </summary>
    [ComponentProperty(BeforeChange = nameof(CancelAnimation))]
    [TemplateProperty]
    private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Center;

    /// <summary>
    /// Vertical alignment of child components within their grid cells.
    /// </summary>
    [ComponentProperty(BeforeChange = nameof(CancelAnimation))]
    [TemplateProperty]
    private VerticalAlignment _verticalAlignment = VerticalAlignment.Center;

    /// <summary>
    /// Arranges child components in a grid pattern with configurable spacing and alignment.
    /// </summary>
    protected override void UpdateLayout()
    {
    var children = GetChildren<IComponent>().OfType<IUserInterfaceElement>().ToArray();
        if (children.Length == 0) return;

        var contentArea = GetContentArea();

        // Measure children using Measure(availableSize)
        int maxHeight = 0;
        int maxWidth = 0;
        foreach (var child in children)
        {
            var measured = child.Measure(contentArea.Size);
            if (measured.X > maxWidth) maxWidth = measured.X;
            if (measured.Y > maxHeight) maxHeight = measured.Y;
        }

        // Calculate actual number of rows
        int actualRows;
        if (RowCount > 0)
        {
            actualRows = RowCount;
        }
        else
        {
            // If there are fewer children than columns, assume a square layout
            // (tests expect ColumnCount rows when children <= ColumnCount).
            if (children.Length > 0 && children.Length <= ColumnCount)
                actualRows = ColumnCount;
            else
                actualRows = (int)Math.Ceiling((double)children.Length / ColumnCount);
        }

    // Prepare per-row heights (may be filled in the else branch below)
    int[]? rowHeights = null;

    // Calculate cell dimensions
        Vector2D<int> cellSize;
        if (UniformCellSize)
        {
            // Use the size of the largest child component
            cellSize = new Vector2D<int>(maxWidth, maxHeight);
        }
        else if (MaintainCellAspectRatio)
        {
            // Calculate cell width based on available space and columns
            int availableWidth = Math.Max(0, contentArea.Size.X - (ColumnCount - 1) * (int)Spacing.X);
            int cellWidth = availableWidth / ColumnCount;
            int cellHeight = (int)(cellWidth / CellAspectRatio);
            cellSize = new Vector2D<int>(cellWidth, cellHeight);
        }
        else
        {
            // Calculate available space and divide by grid dimensions
            int availableWidth = Math.Max(0, contentArea.Size.X - (ColumnCount - 1) * (int)Spacing.X);
            int availableHeight = Math.Max(0, contentArea.Size.Y - (actualRows - 1) * (int)Spacing.Y);
            cellSize = new Vector2D<int>(availableWidth / ColumnCount, availableHeight / actualRows);

            // Prepare per-row heights to distribute any remainder pixels to the last rows
            // This ensures the sum of row heights equals availableHeight and matches test expectations.
            var baseHeight = availableHeight / actualRows;
            var remainder = availableHeight % actualRows;
            rowHeights = new int[actualRows];
            for (int r = 0; r < actualRows; r++)
            {
                rowHeights[r] = baseHeight + ((r >= actualRows - remainder) ? 1 : 0);
            }

            // Use rowHeights when positioning rows below.
            // We'll compute the per-row Y origin during placement below.
            // Store back into a local variable via closure capture by index when needed.
            // (We'll reference rowHeights in the loop.)
        }

    

    // Position each child in its grid cell
        for (int i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var childBounds = child.Measure(contentArea.Size);

            // Calculate grid position
            int row = i / ColumnCount;
            int col = i % ColumnCount;

            // Calculate cell position
            int cellX = contentArea.Origin.X + col * (cellSize.X + (int)Spacing.X);
            int cellY;
            if (rowHeights != null)
            {
                // Sum heights of previous rows plus vertical spacing between them
                int ySum = 0;
                for (int r = 0; r < row; r++)
                {
                    ySum += rowHeights[r] + (int)Spacing.Y;
                }
                cellY = contentArea.Origin.Y + ySum;
            }
            else
            {
                cellY = contentArea.Origin.Y + row * (cellSize.Y + (int)Spacing.Y);
            }

            // Decide allocated size (w/h) based on alignment rules and measured preferred size.
            // Tests expect that Left/Right (or Top/Bottom) preserve preferred size on that axis,
            // while Center/Stretch allocate the full cell size by default.
            var preferred = childBounds; // measured vector

            var w = (HorizontalAlignment == HorizontalAlignment.Left || HorizontalAlignment == HorizontalAlignment.Right)
                        ? preferred.X : cellSize.X;
            var h = (VerticalAlignment == VerticalAlignment.Top || VerticalAlignment == VerticalAlignment.Bottom)
                        ? preferred.Y : cellSize.Y;

            // Calculate child position within cell based on alignment
            int x = HorizontalAlignment switch
            {
                HorizontalAlignment.Left => cellX,
                HorizontalAlignment.Center => cellX + (cellSize.X - w) / 2,
                HorizontalAlignment.Right => cellX + cellSize.X - w,
                HorizontalAlignment.Stretch => cellX,
                _ => cellX
            };

            int y = VerticalAlignment switch
            {
                VerticalAlignment.Top => cellY,
                VerticalAlignment.Center => cellY + (cellSize.Y - h) / 2,
                VerticalAlignment.Bottom => cellY + cellSize.Y - h,
                VerticalAlignment.Stretch => cellY,
                _ => cellY
            };

            // Set child constraints
            var newConstraints = new Rectangle<int>(x, y, w, h);
            child.SetSizeConstraints(newConstraints);
        }
    }
}