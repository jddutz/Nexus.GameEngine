using System.Formats.Asn1;

namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children vertically using
/// property-based configuration for flexible spacing and sizing.
/// </summary>
public partial class VerticalLayout : Container
{
    public VerticalLayout(IDescriptorManager descriptorManager) : base(descriptorManager) { }

    /// <summary>
    /// Gets or sets the fixed height for each child item in pixels.
    /// When set, overrides child heights. When null, uses child measured heights.
    /// Default: null
    /// </summary>
    [ComponentProperty]
    private int? _itemHeight = null;
    
    [TemplateProperty]
    private void SetItemHeight(uint value)
    {
        if (value > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), "uint value is too large for int.");

        _itemHeight = (int)value;
    }

    partial void OnItemHeightChanged(int? oldValue)
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
    /// Range: -1.0 (top) to 1.0 (bottom). Default: 0.0 (center)
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private float _alignContent = 0f;

    /// <summary>
    /// Arranges child components vertically using property-based configuration.
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

        Log.Debug($"VerticalLayout.UpdateLayout called for {Name}");
        var children = new List<IUserInterfaceElement>();
        var measurements = new List<Vector2D<int>>();
        var totalHeight = 0;

        foreach(var child in Children.OfType<IUserInterfaceElement>())
        {
            var m = child.Measure();
            if (m.Y > 0)
            {
                children.Add(child);
                measurements.Add(m);
                totalHeight += _itemHeight ?? m.Y;
            }
        }

        Log.Debug($"Found {children.Count} children");
        if (children.Count <= 0) return;
        if (totalHeight <= 0) return;

        int[] childHeights = measurements.Select(m => (int)(ItemHeight ?? m.Y)).ToArray();

        Log.Debug($"VerticalLayout contentArea: Origin=({contentArea.Origin.X},{contentArea.Origin.Y}), Size=({contentArea.Size.X},{contentArea.Size.Y}), Alignment: {Alignment}");
        
        // Calculate layout based on spacing mode
        Log.Debug($"ItemSpacing.HasValue: {ItemSpacing.HasValue}, ItemSpacing: {ItemSpacing}, Spacing: {Spacing}");
        if (ItemSpacing.HasValue)
        {
            // Fixed spacing mode
            LayoutWithFixedSpacing(
                [.. children.Cast<IUserInterfaceElement>()], 
                childHeights, 
                ItemSpacing.Value, 
                contentArea
            );
        }
        else
        {
            // Automatic spacing mode
            switch (Spacing)
            {
                case SpacingMode.Stacked:
                    LayoutWithFixedSpacing(children.Cast<IUserInterfaceElement>().ToArray(), childHeights, 0, contentArea);
                    break;
                case SpacingMode.Justified:
                    LayoutWithJustifiedSpacing(children.Cast<IUserInterfaceElement>().ToArray(), childHeights, contentArea);
                    break;
                case SpacingMode.Distributed:
                    LayoutWithDistributedSpacing(children.Cast<IUserInterfaceElement>().ToArray(), childHeights, contentArea);
                    break;
            }
        }
    }

    private void LayoutWithFixedSpacing(IUserInterfaceElement[] children, int[] childHeights, int itemSpacing, Rectangle<int> contentArea)
    {
        var totalHeight = childHeights.Sum() + itemSpacing * (children.Length - 1);
        var remainingSpace = contentArea.Size.Y - totalHeight;
        var startY = contentArea.Origin.Y + remainingSpace * (AlignContent + 1) / 2.0f;

        Log.Debug($"LayoutWithFixedSpacing: totalHeight={totalHeight}, remainingSpace={remainingSpace}, startY={startY}, AlignContent={AlignContent}");

        var currentY = startY;
        for (int i = 0; i < children.Length; i++)
        {
            var childConstraints = new Rectangle<int>(
                contentArea.Origin.X, 
                (int)currentY, 
                contentArea.Size.X, 
                childHeights[i]
            );
            Log.Debug($"Child {i} constraints: {childConstraints}");
            children[i].SetSizeConstraints(childConstraints);
            currentY += childHeights[i] + itemSpacing;
        }
    }

    private void LayoutWithJustifiedSpacing(IUserInterfaceElement[] children, int[] childHeights, Rectangle<int> contentArea)
    {
        // Justified spacing requires at least 2 children. For single child, use AlignContent.
        if (children.Length == 1)
        {
            LayoutWithFixedSpacing(children, childHeights, 0, contentArea);
            return;
        }

        var totalChildHeight = childHeights.Sum();
        var availableSpace = contentArea.Size.Y - totalChildHeight;
        var spacing = availableSpace / (children.Length - 1);

        var currentY = contentArea.Origin.Y;
        for (int i = 0; i < children.Length; i++)
        {
            var childConstraints = new Rectangle<int>(
                contentArea.Origin.X, 
                (int)currentY, 
                contentArea.Size.X, 
                childHeights[i]
            );
            children[i].SetSizeConstraints(childConstraints);
            currentY += childHeights[i] + spacing;
        }
    }

    private void LayoutWithDistributedSpacing(IUserInterfaceElement[] children, int[] childHeights, Rectangle<int> contentArea)
    {
        // Distributed spacing with single child should use AlignContent.
        if (children.Length == 1)
        {
            LayoutWithFixedSpacing(children, childHeights, 0, contentArea);
            return;
        }

        var totalChildHeight = childHeights.Sum();
        var availableSpace = contentArea.Size.Y - totalChildHeight;
        var spacing = availableSpace / (children.Length + 1);

        var currentY = contentArea.Origin.Y + spacing;
        for (int i = 0; i < children.Length; i++)
        {
            var childConstraints = new Rectangle<int>(
                contentArea.Origin.X, 
                (int)currentY, 
                contentArea.Size.X, 
                childHeights[i]
            );
            children[i].SetSizeConstraints(childConstraints);
            currentY += childHeights[i] + spacing;
        }
    }

    partial void OnItemSpacingChanged(int? oldValue)
    {
        Log.Debug($"OnItemSpacingChanged: {oldValue} -> {ItemSpacing}");
        Invalidate();
    }

    partial void OnAlignContentChanged(float oldValue)
    {
        Invalidate();
    }
}
