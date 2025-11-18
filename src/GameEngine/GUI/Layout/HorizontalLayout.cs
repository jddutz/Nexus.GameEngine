namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children horizontally.
/// Child components are positioned side by side with configurable spacing and alignment.
/// </summary>
public partial class HorizontalLayout : Container
{
    public HorizontalLayout(IDescriptorManager descriptorManager) : base(descriptorManager) { }

    /// <summary>
    /// Gets or sets the fixed height for each child item in pixels.
    /// When set, overrides child heights. When null, uses child measured heights.
    /// Default: null
    /// </summary>
    [ComponentProperty]
    private int? _itemWidth = null;
    
    [TemplateProperty]
    private void SetItemWidth(uint value)
    {
        if (value > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), "uint value is too large for int.");

        _itemWidth = (int)value;
    }

    partial void OnItemWidthChanged(int? oldValue)
    {
        Invalidate();
    }

    [ComponentProperty]
    private int? _itemSpacing;

    /// <summary>
    /// Gets or sets the fixed spacing between children in pixels.
    /// When set, overrides automatic spacing. When null, uses Spacing mode.
    /// Default: null
    /// </summary>
    [TemplateProperty(Name="ItemSpacing")]
    private void SetItemSpacing(uint value)
    {
        if (value > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), "uint value is too large for int.");

        _itemSpacing = (int)value;
    }

    /// <summary>
    /// Gets or sets the automatic spacing distribution mode.
    /// Used when ItemSpacing is null. Default: Stacked
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private SpacingMode _spacing = SpacingMode.Stacked;

    partial void OnSpacingChanged(SpacingMode oldValue)
    {
        Invalidate();
    }

    /// <summary>
    /// Gets or sets the alignment of the children group within the layout's content area.
    /// Only applies when ItemSpacing is set (fixed spacing mode).
    /// Range: -1.0 (left) to 1.0 (right). Default: 0.0 (center)
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private float _alignContent = 0f;

    partial void OnAlignContentChanged(float oldValue)
    {
        Invalidate();
    }

    /// <summary>
    /// Arranges child components horizontally with fixed width and full height.
    /// Each child receives constraints with ItemWidth (or measured width if ItemWidth is 0)
    /// and the full content area height. Children position themselves within these constraints
    /// using their own AnchorPoint.
    /// </summary>
    protected override void UpdateLayout()
    {
        var contentArea = new Rectangle<int>(
            (int)(TargetPosition.X - (1.0f + AnchorPoint.X) * TargetSize.X * 0.5f) + Padding.Left,
            (int)(TargetPosition.Y - (1.0f + AnchorPoint.Y) * TargetSize.Y * 0.5f) + Padding.Top,
            Math.Max(0, TargetSize.X - Padding.Left - Padding.Right),
            Math.Max(0, TargetSize.Y - Padding.Top - Padding.Bottom)
        );

        if (contentArea.Size.Y <= 0) return;

        Log.Debug($"HorizontalLayout.UpdateLayout called for {Name}");
        var children = new List<IUserInterfaceElement>();
        var measurements = new List<Vector2D<int>>();
        var totalWidth = 0;

        foreach(var child in Children.OfType<IUserInterfaceElement>())
        {
            var m = child.Measure();
            if (m.X > 0)
            {
                children.Add(child);
                measurements.Add(m);
                totalWidth += _itemWidth ?? m.X;
            }
        }

        Log.Debug($"Found {children.Count} children");
        if (children.Count <= 0) return;
        if (totalWidth <= 0) return;

        int[] childWidths = measurements.Select(m => (int)(_itemWidth ?? m.X)).ToArray();

        Log.Debug($"HorizontalLayout contentArea: Origin=({contentArea.Origin.X},{contentArea.Origin.Y}), Size=({contentArea.Size.X},{contentArea.Size.Y}), Alignment: {Alignment}");
        
        // Calculate layout based on spacing mode
        Log.Debug($"ItemSpacing.HasValue: {_itemSpacing.HasValue}, ItemSpacing: {_itemSpacing}, Spacing: {_spacing}");
        if (_itemSpacing.HasValue)
        {
            // Fixed spacing mode
            LayoutWithFixedSpacing(
                [.. children.Cast<IUserInterfaceElement>()], 
                childWidths, 
                _itemSpacing.Value, 
                contentArea
            );
        }
        else
        {
            // Automatic spacing mode
            switch (Spacing)
            {
                case SpacingMode.Stacked:
                    LayoutWithFixedSpacing(children.Cast<IUserInterfaceElement>().ToArray(), childWidths, 0, contentArea);
                    break;
                case SpacingMode.Justified:
                    LayoutWithJustifiedSpacing(children.Cast<IUserInterfaceElement>().ToArray(), childWidths, contentArea);
                    break;
                case SpacingMode.Distributed:
                    LayoutWithDistributedSpacing(children.Cast<IUserInterfaceElement>().ToArray(), childWidths, contentArea);
                    break;
            }
        }
    }

    private void LayoutWithFixedSpacing(IUserInterfaceElement[] children, int[] childWidths, int itemSpacing, Rectangle<int> contentArea)
    {
        var totalWidth = childWidths.Sum() + itemSpacing * (children.Length - 1);
        var remainingSpace = contentArea.Size.X - totalWidth;
        var startX = contentArea.Origin.X + remainingSpace * (AlignContent + 1) / 2.0f;

        Log.Debug($"LayoutWithFixedSpacing: totalWidth={totalWidth}, remainingSpace={remainingSpace}, startX={startX}, AlignContent={AlignContent}");

        var currentX = startX;
        for (int i = 0; i < children.Length; i++)
        {
            var childConstraints = new Rectangle<int>(
                (int)currentX, 
                contentArea.Origin.Y, 
                childWidths[i], 
                contentArea.Size.Y
            );
            Log.Debug($"Child {i} constraints: {childConstraints}");
            children[i].SetSizeConstraints(childConstraints);
            currentX += childWidths[i] + itemSpacing;
        }
    }

    private void LayoutWithJustifiedSpacing(IUserInterfaceElement[] children, int[] childWidths, Rectangle<int> contentArea)
    {
        // Justified spacing requires at least 2 children. For single child, use AlignContent.
        if (children.Length == 1)
        {
            LayoutWithFixedSpacing(children, childWidths, 0, contentArea);
            return;
        }

        var totalChildWidth = childWidths.Sum();
        var availableSpace = contentArea.Size.X - totalChildWidth;
        var spacing = availableSpace / (children.Length - 1);

        var currentX = contentArea.Origin.X;
        for (int i = 0; i < children.Length; i++)
        {
            var childConstraints = new Rectangle<int>(
                (int)currentX,
                contentArea.Origin.Y,
                childWidths[i],
                contentArea.Size.Y
            );
            children[i].SetSizeConstraints(childConstraints);
            currentX += childWidths[i] + spacing;
        }
    }    private void LayoutWithDistributedSpacing(IUserInterfaceElement[] children, int[] childWidths, Rectangle<int> contentArea)
    {
        // Distributed spacing with single child should use AlignContent.
        if (children.Length == 1)
        {
            LayoutWithFixedSpacing(children, childWidths, 0, contentArea);
            return;
        }

        var totalChildWidth = childWidths.Sum();
        var availableSpace = contentArea.Size.X - totalChildWidth;
        var spacing = availableSpace / (children.Length + 1);

        var currentX = contentArea.Origin.X + spacing;
        for (int i = 0; i < children.Length; i++)
        {
            var childConstraints = new Rectangle<int>(
                (int)currentX,
                contentArea.Origin.Y,
                childWidths[i],
                contentArea.Size.Y
            );
            children[i].SetSizeConstraints(childConstraints);
            currentX += childWidths[i] + spacing;
        }
    }
}
