using Nexus.GameEngine.GUI.Layout;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// Base class for UI components with anchor-based positioning.
/// 
/// POSITIONING SYSTEM:
/// - Position: Where the element IS in screen space (pixels) - inherited from Transformable
/// - AnchorPoint: Which point of the element aligns with Position (normalized -1 to 1)
/// - Size: Pixel dimensions of the element
/// - Scale: Multiplier applied to Size for effects - inherited from Transformable
/// 
/// LAYOUT INTEGRATION:
/// - Parents call SetSizeConstraints() to define available space
/// - Element positions itself within constraints via OnSizeConstraintsChanged()
/// - Default: Fill constraints with top-left anchor
/// - Override OnSizeConstraintsChanged() for custom layout behavior
/// 
/// RENDERING:
/// - WorldMatrix (from Transformable) used as model matrix
/// - Transforms from local geometry space to screen space
/// - Combined with camera view-projection matrix in shader
/// </summary>
public partial class Element(IDescriptorManager descriptorManager) : Transformable, IUserInterfaceElement
{
    protected IDescriptorManager DescriptorManager { get; } = descriptorManager;
    
    // Element is layout/transform only and must not depend on graphics services.
    // Graphics-related dependencies belong to drawable components (Drawable / DrawableElement).

    /// <summary>
    /// Overrides UpdateLocalMatrix to account for Size and AnchorPoint.
    /// Computes the position of the quad's center based on where the anchor point should be.
    /// </summary>
    protected override void UpdateLocalMatrix()
    {
        // Compute quad center position from anchor point
        // Position is where the AnchorPoint is located in screen space
        // We offset backwards to find the center of the quad
        var centerX = Position.X - (AnchorPoint.X * Size.X * 0.5f * Scale.X);
        var centerY = Position.Y - (AnchorPoint.Y * Size.Y * 0.5f * Scale.Y);
        var centerZ = Position.Z;

        // Scale: Geometry is 2x2 (±1), so scale by Size/2 to get pixel dimensions
        // Then multiply by Scale for effects
        var scaleX = Size.X * 0.5f * Scale.X;
        var scaleY = Size.Y * 0.5f * Scale.Y;
        var scaleZ = Scale.Z;

        // Build transform: Scale then Translate (no rotation for basic UI)
        // Store directly in the _localMatrix field from base class
        _localMatrix = Matrix4X4.CreateScale(scaleX, scaleY, scaleZ)
                     * Matrix4X4.CreateTranslation(centerX, centerY, centerZ);
    }

    /// <summary>
    /// Anchor point in normalized element space (-1 to 1).
    /// Defines which point of the element aligns with Position in screen space.
    /// (-1, -1) = top-left, (0, 0) = center, (1, 1) = bottom-right.
    /// Default: (-1, -1) for top-left alignment (standard UI behavior).
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _anchorPoint = new(-1, -1);

    partial void OnAnchorPointChanged(Vector2D<float> oldValue)
    {
        // When AnchorPoint changes, regenerate the cached local matrix
        UpdateLocalMatrix();
    }

    /// <summary>
    /// Pixel dimensions of the element (width, height).
    /// Combined with Scale (inherited from Transformable) to determine final rendered size.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<int> _size = new(0, 0);

    partial void OnSizeChanged(Vector2D<int> oldValue)
    {
        // When Size changes, regenerate the cached local matrix
        UpdateLocalMatrix();
        
        // Notify parent layout if it exists (for intrinsic sizing)
        var parentLayout = FindParent<ILayout>();
        parentLayout?.OnChildSizeChanged(this, oldValue);
    }

    /// <summary>
    /// UV rectangle for texture atlas/sprite sheet support.
    /// MinUV = top-left corner of sprite in texture (default 0,0)
    /// MaxUV = bottom-right corner of sprite in texture (default 1,1)
    /// For full texture: MinUV=(0,0), MaxUV=(1,1)
    /// For sprite in atlas: set to sub-rectangle coordinates
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private Vector2D<float> _minUV = new(0f, 0f);

    [ComponentProperty]
    [TemplateProperty]
    private Vector2D<float> _maxUV = new(1f, 1f);

    /// <summary>
    /// Size constraints available to this element (set by parent or viewport).
    /// Defines the maximum space this element can occupy.
    /// </summary>
    private Rectangle<int> _sizeConstraints = new(0, 0, 0, 0);

    /// <summary>
    /// Gets the current size constraints for this element.
    /// </summary>
    protected Rectangle<int> SizeConstraints => _sizeConstraints;

    /// <summary>
    /// How this element determines its size.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private SizeMode _sizeMode = SizeMode.Fixed;

    /// <summary>
    /// Independent horizontal size mode (width).
    /// If not set explicitly, defaults to Fixed to preserve existing behaviour.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private SizeMode _horizontalSizeMode = SizeMode.Fixed;

    /// <summary>
    /// Independent vertical size mode (height).
    /// If not set explicitly, defaults to Fixed to preserve existing behaviour.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private SizeMode _verticalSizeMode = SizeMode.Fixed;

    /// <summary>
    /// Horizontal alignment used by parent layouts when positioning this element
    /// inside the provided constraints rectangle. Default: Left.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private float _layoutHorizontal = HorizontalAlignment.Left;

    /// <summary>
    /// Vertical alignment used by parent layouts when positioning this element
    /// inside the provided constraints rectangle. Default: Top.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private float _layoutVertical = VerticalAlignment.Top;

    // Note: Percentage values are represented via RelativeWidth/RelativeHeight as fractional multipliers
    // (e.g. 0.5 = 50%). We intentionally do not store separate WidthPercentage/HeightPercentage fields.

    /// <summary>
    /// Relative width modifier. Interpretation depends on HorizontalSizeMode:
    /// - Fixed: ignored
    /// - Percentage: if non-zero, treated as a fractional multiplier (e.g. 0.5 = 50%); otherwise uses WidthPercentage
    /// - Stretch: treated as pixel offset added to the available width (negative to shrink by pixels)
    /// - Intrinsic: treated as additional pixels to add to intrinsic size
    /// Default: 0 (no modification)
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private float _relativeWidth = 0f;

    /// <summary>
    /// Relative height modifier. Interpretation depends on VerticalSizeMode:
    /// - Fixed: ignored
    /// - Percentage: if non-zero, treated as a fractional multiplier (e.g. 0.5 = 50%); otherwise uses HeightPercentage
    /// - Stretch: treated as pixel offset added to the available height (negative to shrink by pixels)
    /// - Intrinsic: treated as additional pixels to add to intrinsic size
    /// Default: 0 (no modification)
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private float _relativeHeight = 0f;

    /// <summary>
    /// Minimum size constraints (width, height).
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private Vector2D<int> _minSize = new(0, 0);

    /// <summary>
    /// Maximum size constraints (width, height).
    /// Use 0 for no limit.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private Vector2D<int> _maxSize = new(0, 0);

    /// <summary>
    /// Sets the size constraints for this element.
    /// Called by parent layout or root elements.
    /// Only triggers recalculation if constraints actually changed.
    /// </summary>
    public virtual void SetSizeConstraints(Rectangle<int> constraints)
    {
        // Only trigger recalculation if constraints changed
        if (_sizeConstraints.Equals(constraints))
            return;
            
        _sizeConstraints = constraints;
        // When constraints change, element may need to recalculate its bounds
        OnSizeConstraintsChanged(constraints);
    }

    /// <summary>
    /// Called when size constraints change.
    /// Override to implement custom sizing behavior.
    /// Applies SizeMode logic and enforces MinSize/MaxSize constraints.
    /// </summary>
    protected virtual void OnSizeConstraintsChanged(Rectangle<int> constraints)
    {
        if (constraints.Size.X <= 0 || constraints.Size.Y <= 0)
            return;
        // Calculate preferred width/height independently based on per-axis size modes
        var preferredWidth = CalculatePreferredWidth(constraints);
        var preferredHeight = CalculatePreferredHeight(constraints);

        // Combine and apply size constraints (min/max)
        var preferredSize = new Vector2D<int>(preferredWidth, preferredHeight);
        var finalSize = ApplySizeConstraints(preferredSize);

        // Update size first
        SetSize(finalSize);

        var posX = constraints.Center.X + LayoutHorizontal * constraints.HalfSize.X;
        var posY = constraints.Center.Y + LayoutVertical * constraints.HalfSize.Y;

        SetPosition(new Vector3D<float>(posX, posY, Position.Z));
    }

    /// <summary>
    /// Calculates preferred size based on SizeMode and constraints.
    /// </summary>
    protected virtual Vector2D<int> CalculatePreferredSize(Rectangle<int> constraints)
    {
        // Backwards-compatible: compute both axes via the new per-axis logic
        var w = CalculatePreferredWidth(constraints);
        var h = CalculatePreferredHeight(constraints);
        return new Vector2D<int>(w, h);
    }

    /// <summary>
    /// Calculate preferred width using the HorizontalSizeMode and RelativeWidth semantics.
    /// </summary>
    protected virtual int CalculatePreferredWidth(Rectangle<int> constraints)
    {
        if (constraints.Size.X <= 0)
            return Size.X;

        switch (HorizontalSizeMode)
        {
            case SizeMode.Fixed:
                return Size.X;

            case SizeMode.Percent:
            {
                var multiplier = Math.Abs(RelativeWidth) > float.Epsilon ? RelativeWidth : 1f;
                return (int)(constraints.Size.X * multiplier);
            }

            case SizeMode.Stretch:
            {
                var baseW = constraints.Size.X;
                if (Math.Abs(RelativeWidth) > float.Epsilon)
                    baseW = Math.Max(0, baseW + (int)RelativeWidth);
                return baseW;
            }

            case SizeMode.Intrinsic:
            {
                var intrinsic = CalculateIntrinsicSize().X + (int)RelativeWidth;
                return intrinsic;
            }

            default:
                return Size.X;
        }
    }

    /// <summary>
    /// Calculate preferred height using the VerticalSizeMode and RelativeHeight semantics.
    /// </summary>
    protected virtual int CalculatePreferredHeight(Rectangle<int> constraints)
    {
        if (constraints.Size.Y <= 0)
            return Size.Y;

        switch (VerticalSizeMode)
        {
            case SizeMode.Fixed:
                return Size.Y;

            case SizeMode.Percent:
            {
                var multiplier = Math.Abs(RelativeHeight) > float.Epsilon ? RelativeHeight : 1f;
                return (int)(constraints.Size.Y * multiplier);
            }

            case SizeMode.Stretch:
            {
                var baseH = constraints.Size.Y;
                if (Math.Abs(RelativeHeight) > float.Epsilon)
                    baseH = Math.Max(0, baseH + (int)RelativeHeight);
                return baseH;
            }

            case SizeMode.Intrinsic:
            {
                var intrinsic = CalculateIntrinsicSize().Y + (int)RelativeHeight;
                return intrinsic;
            }

            default:
                return Size.Y;
        }
    }

    /// <summary>
    /// Calculates intrinsic size based on content (children bounds).
    /// Override in derived classes for content-specific sizing (e.g., TextElement).
    /// </summary>
    protected virtual Vector2D<int> CalculateIntrinsicSize()
    {
        // Base implementation: query children bounds
        var childElements = GetChildren<Element>().ToList();
        if (childElements.Count == 0)
            return new Vector2D<int>(100, 100); // Default minimum size
        
        int maxX = 0, maxY = 0;
        foreach (var elem in childElements)
        {
            var childBounds = elem.GetBounds();
            maxX = Math.Max(maxX, childBounds.Origin.X + childBounds.Size.X);
            maxY = Math.Max(maxY, childBounds.Origin.Y + childBounds.Size.Y);
        }
        return new Vector2D<int>(maxX, maxY);
    }

    /// <summary>
    /// Measures the desired size of this element given the available size.
    /// Default behavior depends on SizeMode:
    /// - Fixed: return TargetSize
    /// - Percentage: compute from availableSize and width/height percentages
    /// - Stretch: return availableSize
    /// - Intrinsic: return CalculateIntrinsicSize()
    /// The result is clamped by MinSize/MaxSize.
    /// </summary>
    public Vector2D<int> Measure(Vector2D<int> availableSize)
    {
        // Compute desired size per-axis using the independent size modes and relative modifiers
        int desiredWidth;
        switch (HorizontalSizeMode)
        {
            case SizeMode.Fixed:
                desiredWidth = TargetSize.X;
                break;
            case SizeMode.Percent:
                desiredWidth = Math.Abs(RelativeWidth) > float.Epsilon
                    ? (int)(availableSize.X * RelativeWidth)
                    : (int)(availableSize.X * 1f);
                break;
            case SizeMode.Stretch:
                desiredWidth = availableSize.X;
                if (Math.Abs(RelativeWidth) > float.Epsilon)
                    desiredWidth = Math.Max(0, desiredWidth + (int)RelativeWidth);
                break;
            case SizeMode.Intrinsic:
                desiredWidth = CalculateIntrinsicSize().X + (int)RelativeWidth;
                break;
            default:
                desiredWidth = TargetSize.X;
                break;
        }

        int desiredHeight;
        switch (VerticalSizeMode)
        {
            case SizeMode.Fixed:
                desiredHeight = TargetSize.Y;
                break;
            case SizeMode.Percent:
                desiredHeight = Math.Abs(RelativeHeight) > float.Epsilon
                    ? (int)(availableSize.Y * RelativeHeight)
                    : (int)(availableSize.Y * 1f);
                break;
            case SizeMode.Stretch:
                desiredHeight = availableSize.Y;
                if (Math.Abs(RelativeHeight) > float.Epsilon)
                    desiredHeight = Math.Max(0, desiredHeight + (int)RelativeHeight);
                break;
            case SizeMode.Intrinsic:
                desiredHeight = CalculateIntrinsicSize().Y + (int)RelativeHeight;
                break;
            default:
                desiredHeight = TargetSize.Y;
                break;
        }

        var desired = new Vector2D<int>(desiredWidth, desiredHeight);

        // Apply min/max constraints (0 means no limit for MaxSize)
        if (MinSize.X > 0 && desired.X < MinSize.X) desired.X = MinSize.X;
        if (MinSize.Y > 0 && desired.Y < MinSize.Y) desired.Y = MinSize.Y;
        if (MaxSize.X > 0 && desired.X > MaxSize.X) desired.X = MaxSize.X;
        if (MaxSize.Y > 0 && desired.Y > MaxSize.Y) desired.Y = MaxSize.Y;

        return desired;
    }

    /// <summary>
    /// Applies min/max size constraints.
    /// </summary>
    protected Vector2D<int> ApplySizeConstraints(Vector2D<int> size)
    {
        var width = size.X;
        var height = size.Y;
        
        // Apply minimum size
        if (MinSize.X > 0 && width < MinSize.X) width = MinSize.X;
        if (MinSize.Y > 0 && height < MinSize.Y) height = MinSize.Y;
        
        // Apply maximum size (0 means no limit)
        if (MaxSize.X > 0 && width > MaxSize.X) width = MaxSize.X;
        if (MaxSize.Y > 0 && height > MaxSize.Y) height = MaxSize.Y;
        
        return new Vector2D<int>(width, height);
    }

    /// <summary>
    /// Computes the bounding rectangle of this element in screen space.
    /// Derived from Position, AnchorPoint, and Size.
    /// </summary>
    public Rectangle<int> GetBounds()
    {
        // AnchorPoint is in normalized space (-1 to 1)
        // Convert to offset: (AnchorPoint + 1) / 2 gives 0 to 1
        // Multiply by Size to get pixel offset from Position
        var anchorOffsetX = (AnchorPoint.X + 1.0f) * 0.5f * Size.X;
        var anchorOffsetY = (AnchorPoint.Y + 1.0f) * 0.5f * Size.Y;
        
        // Top-left corner = Position - anchor offset
        var originX = (int)(Position.X - anchorOffsetX);
        var originY = (int)(Position.Y - anchorOffsetY);
        
        return new Rectangle<int>(originX, originY, Size.X, Size.Y);
    }
}
