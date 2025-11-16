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
    /// Overrides UpdateLocalMatrix to account for AnchorPoint.
    /// Computes the element's position based on where the anchor point should be.
    /// Note: Does NOT include element Size scaling or AnchorPoint offset - those are handled 
    /// in the shader via push constants to avoid double-application.
    /// DOES include Position, Rotation, and Scale from Transformable.
    /// </summary>
    protected override void UpdateLocalMatrix()
    {
        // The shader handles both size scaling AND anchor offset via push constants:
        // vec2 xy = (inPos - anchor) * size * 0.5;
        // So the world matrix should contain SRT (Scale-Rotation-Translation) from Transformable,
        // but NOT the element size scaling or anchor offset (those are in the shader).
        
        // Build transform: Scale (from Transformable) -> Rotation -> Translation
        // Standard SRT (Scale-Rotation-Translation) matrix composition
        _localMatrix = Matrix4X4.CreateScale(_scale) *
                       Matrix4X4.CreateFromQuaternion(_rotation) *
                       Matrix4X4.CreateTranslation(_position);
    }

    /// <summary>
    /// Anchor point in normalized element space (-1 to 1).
    /// Defines which point of the element aligns with Position in screen space.
    /// (-1, -1) = top-left, (0, 0) = center, (1, 1) = bottom-right.
    /// Default: (-1, -1) for top-left alignment (standard UI behavior).
    /// </summary>
    [ComponentProperty]
    protected Vector2D<float> _anchorPoint = new(-1, -1);

    /// <summary>
    /// Pixel dimensions of the element (width, height).
    /// Combined with Scale (inherited from Transformable) to determine final rendered size.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<int> _size = new(0, 0);

    partial void OnSizeChanged(Vector2D<int> oldValue)
    {
        // Size is handled in the shader via push constants, so no need to update
        // the local matrix. The matrix only contains Position translation.
        
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
    [TemplateProperty]
    [ComponentProperty]
    private Vector2D<float> _alignment = Align.TopLeft;

    // Note: RelativeWidth/RelativeHeight are always treated as raw multipliers
    // (e.g. 0.5 = 50% of the available space for display, 250 = 250x). Do not convert values to percentages.
    // Displaying values as percentages is a view/formatting concern and should not be performed here.

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
        Log.Debug($"OnSizeConstraintsChanged: {Name} {constraints.Origin} {constraints.Size}");

        if (constraints.Size.X <= 0 || constraints.Size.Y <= 0)
            return;

        // Calculate preferred width/height independently based on per-axis size modes
        var preferredWidth = HorizontalSizeMode switch
        {
            SizeMode.Fixed => Size.X,
            SizeMode.Percent => Math.Max(0, (int)(constraints.Size.X * RelativeWidth)),
            SizeMode.Stretch => Math.Max(0, constraints.Size.X + (int)RelativeWidth),
            SizeMode.Intrinsic => CalculateIntrinsicSize().X + (int)RelativeWidth,
            _ => Size.X,
        };

        Log.Debug($"preferredWidth: {preferredWidth}");

        var preferredHeight = VerticalSizeMode switch
        {
            SizeMode.Fixed => Size.Y,
            SizeMode.Percent => Math.Max(0, (int)(constraints.Size.Y * RelativeHeight)),
            SizeMode.Stretch => Math.Max(0, constraints.Size.Y + (int)RelativeHeight),
            SizeMode.Intrinsic => CalculateIntrinsicSize().Y + (int)RelativeHeight,
            _ => Size.Y,
        };

        Log.Debug($"preferredHeight: {preferredHeight}");

        // Combine and apply size constraints (min/max)
        var preferredSize = new Vector2D<int>(preferredWidth, preferredHeight);
        var finalSize = ApplySizeConstraints(preferredSize);

        Log.Debug($"finalSize: {finalSize}");

        // Update size first
        SetSize(finalSize);

        // If AnchorPoint wasn't explicitly set by template or API, seed it from layout
        // LayoutHorizontal/LayoutVertical are template-only convenience values in [-1,1].
        _anchorPoint = new Vector2D<float>(Alignment.X, Alignment.Y);
        UpdateLocalMatrix();

        Log.Debug($"WorldMatrix: {WorldMatrix}");

        var posX = constraints.Center.X + Alignment.X * constraints.HalfSize.X;
        var posY = constraints.Center.Y + Alignment.Y * constraints.HalfSize.Y;

        SetPosition(new Vector3D<float>(posX, posY, Position.Z));

        Log.Debug($"Position: {Position}");
        Log.Debug($"AnchorPoint: {AnchorPoint}");
    }

    // When the single SizeMode property is changed, propagate the value to per-axis
    // size modes for backward compatibility when those axes still matched the old value.
    partial void OnSizeModeChanged(SizeMode oldValue)
    {
        var newValue = _sizeMode;
        if (_horizontalSizeMode == oldValue)
        {
            var prev = _horizontalSizeMode;
            _horizontalSizeMode = newValue;
            OnHorizontalSizeModeChanged(prev);
        }

        if (_verticalSizeMode == oldValue)
        {
            var prev = _verticalSizeMode;
            _verticalSizeMode = newValue;
            OnVerticalSizeModeChanged(prev);
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
                if (Math.Abs(RelativeWidth) <= float.Epsilon)
                    desiredWidth = availableSize.X;
                else
                {
                    // RelativeWidth is a raw multiplier (no percent conversion).
                    var mult = RelativeWidth;
                    desiredWidth = (int)(availableSize.X * mult);
                }
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
                if (Math.Abs(RelativeHeight) <= float.Epsilon)
                    desiredHeight = availableSize.Y;
                else
                {
                    // RelativeHeight is a raw multiplier (no percent conversion).
                    var mult = RelativeHeight;
                    desiredHeight = (int)(availableSize.Y * mult);
                }
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
