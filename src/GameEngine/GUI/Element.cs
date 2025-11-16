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

    /// <summary>
    /// Anchor point in normalized element space (-1 to 1).
    /// Defines which point of the element aligns with Position in screen space.
    /// Also used by parent layouts to position this element within size constraints.
    /// (-1, -1) = top-left, (0, 0) = center, (1, 1) = bottom-right.
    /// Default: (-1, -1) for top-left alignment (standard UI behavior).
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _anchorPoint = Align.MiddleCenter;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _alignment = Align.MiddleCenter;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<int> _offset = Vector2D<int>.Zero;

    /// <summary>
    /// Pixel dimensions of the element (width, height).
    /// Combined with Scale (inherited from Transformable) to determine final rendered size.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<int> _size = new(0, 0);

    /// <summary>
    /// Template-only property for setting width.
    /// Sets the X component of Size vector.
    /// </summary>
    [TemplateProperty(Name = "Width")]
    partial void SetWidth(int value);

    /// <summary>
    /// Template-only property for setting height.
    /// Sets the Y component of Size vector.
    /// </summary>
    [TemplateProperty(Name = "Height")]
    partial void SetHeight(int value);

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
    /// Relative size modifier (width, height). Interpretation depends on HorizontalSizeMode/VerticalSizeMode:
    /// - Fixed: ignored
    /// - FitContent: ignored (size calculated from content)
    /// - Relative: fractional multiplier (e.g. 0.5 = 50% of container)
    /// - Absolute: pixel offset added to container size (e.g. -20 = container - 20px)
    /// Default: (0, 0) (no modification)
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private Vector2D<float> _relativeSize = new(0f, 0f);

    /// <summary>
    /// Template-only property for setting relative width.
    /// Sets the X component of RelativeSize.
    /// </summary>
    [TemplateProperty(Name = "RelativeWidth")]
    partial void SetRelativeWidth(float value);

    /// <summary>
    /// Template-only property for setting relative height.
    /// Sets the Y component of RelativeSize.
    /// </summary>
    [TemplateProperty(Name = "RelativeHeight")]
    partial void SetRelativeHeight(float value);

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

        Log.Debug($"{Name} Updating Size Constraints: {Name} {constraints.Origin} {constraints.Size}");
            
        _sizeConstraints = constraints;

        // When constraints change, element may need to recalculate its bounds

        if (constraints.Size.X <= 0 || constraints.Size.Y <= 0)
            return;

        // Calculate preferred width/height independently based on per-axis size modes
        var preferredWidth = HorizontalSizeMode switch
        {
            SizeMode.Fixed => Size.X,
            SizeMode.Relative => Math.Max(0, (int)(constraints.Size.X * RelativeSize.X)),
            SizeMode.Absolute => Math.Max(0, constraints.Size.X + (int)RelativeSize.X),
            SizeMode.FitContent => CalculateIntrinsicSize().X,
            _ => Size.X,
        };

        Log.Debug($"{Name} preferredWidth: {preferredWidth}");

        var preferredHeight = VerticalSizeMode switch
        {
            SizeMode.Fixed => Size.Y,
            SizeMode.Relative => Math.Max(0, (int)(constraints.Size.Y * RelativeSize.Y)),
            SizeMode.Absolute => Math.Max(0, constraints.Size.Y + (int)RelativeSize.Y),
            SizeMode.FitContent => CalculateIntrinsicSize().Y,
            _ => Size.Y,
        };

        Log.Debug($"{Name} preferredHeight: {preferredHeight}");

        // Combine and apply size constraints (min/max)
        var preferredSize = new Vector2D<int>(preferredWidth, preferredHeight);
        var finalSize = ApplySizeConstraints(preferredSize);

        Log.Debug($"{Name} finalSize: {finalSize}");

        // Update size first
        SetSize(finalSize);

        var posX = constraints.Center.X + Alignment.X * constraints.HalfSize.X + Offset.X;
        var posY = constraints.Center.Y + Alignment.Y * constraints.HalfSize.Y + Offset.Y;
        var newPosition = new Vector3D<float>(posX, posY, Position.Z);
        SetPosition(newPosition);

        Log.Debug($"{Name} Alignment: {Alignment}");
        Log.Debug($"{Name} Position: {Position}");
        Log.Debug($"{Name} AnchorPoint: {AnchorPoint}");
        
        UpdateLocalMatrix();
        UpdateWorldMatrix();
        Log.Debug($"{Name} WorldMatrix: {WorldMatrix}");
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
            case SizeMode.Relative:
                if (Math.Abs(RelativeSize.X) <= float.Epsilon)
                    desiredWidth = availableSize.X;
                else
                {
                    // RelativeSize.X is a raw multiplier (no percent conversion).
                    var mult = RelativeSize.X;
                    desiredWidth = (int)(availableSize.X * mult);
                }
                break;
            case SizeMode.Absolute:
                desiredWidth = availableSize.X;
                if (Math.Abs(RelativeSize.X) > float.Epsilon)
                    desiredWidth = Math.Max(0, desiredWidth + (int)RelativeSize.X);
                break;
            case SizeMode.FitContent:
                desiredWidth = CalculateIntrinsicSize().X;
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
            case SizeMode.Relative:
                if (Math.Abs(RelativeSize.Y) <= float.Epsilon)
                    desiredHeight = availableSize.Y;
                else
                {
                    // RelativeSize.Y is a raw multiplier (no percent conversion).
                    var mult = RelativeSize.Y;
                    desiredHeight = (int)(availableSize.Y * mult);
                }
                break;
            case SizeMode.Absolute:
                desiredHeight = availableSize.Y;
                if (Math.Abs(RelativeSize.Y) > float.Epsilon)
                    desiredHeight = Math.Max(0, desiredHeight + (int)RelativeSize.Y);
                break;
            case SizeMode.FitContent:
                desiredHeight = CalculateIntrinsicSize().Y;
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
    /// Template setter for Width.
    /// Sets the X component of Size vector.
    /// </summary>
    partial void SetWidth(int value)
    {
        _size = new Vector2D<int>(value, _size.Y);
    }

    /// <summary>
    /// Template setter for Height.
    /// Sets the Y component of Size vector.
    /// </summary>
    partial void SetHeight(int value)
    {
        _size = new Vector2D<int>(_size.X, value);
    }

    /// <summary>
    /// Template setter for RelativeWidth.
    /// Sets the X component of RelativeSize vector.
    /// </summary>
    partial void SetRelativeWidth(float value)
    {
        _relativeSize = new Vector2D<float>(value, _relativeSize.Y);
    }

    /// <summary>
    /// Template setter for RelativeHeight.
    /// Sets the Y component of RelativeSize vector.
    /// </summary>
    partial void SetRelativeHeight(float value)
    {
        _relativeSize = new Vector2D<float>(_relativeSize.X, value);
    }

    /// <summary>
    /// Applies mode-specific default values after template properties are loaded.
    /// For SizeMode.Relative, RelativeSize defaults to 1.0 (100%) if not explicitly set in template.
    /// For SizeMode.Absolute, RelativeSize defaults to 0.0 (exact container size) if not explicitly set.
    /// Checks template.HasValue to distinguish "not set" from "explicitly set to 0".
    /// </summary>
    partial void OnLoad(ElementTemplate template)
    {
        // Apply defaults for Relative mode only when not explicitly set in template
        // This allows explicit 0.0f values (e.g., for slide-out animations)
        
        if (_horizontalSizeMode == SizeMode.Relative && !template.RelativeWidth.HasValue)
        {
            _relativeSize = new Vector2D<float>(1.0f, _relativeSize.Y);
        }
        
        if (_verticalSizeMode == SizeMode.Relative && !template.RelativeHeight.HasValue)
        {
            _relativeSize = new Vector2D<float>(_relativeSize.X, 1.0f);
        }
        
        // Absolute mode already has the correct default (0.0), so no action needed
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
