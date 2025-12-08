using Nexus.GameEngine.Components;
using Nexus.GameEngine.Data.Binding;
using Nexus.GameEngine.GUI.Layout;

namespace Nexus.GameEngine.GUI;

public partial class UserInterfaceElement : RectTransform, IUserInterfaceElement
{
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _alignment = Align.MiddleCenter;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _offset = Vector2D<float>.Zero;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _minSize = Vector2D<float>.Zero;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _maxSize = Vector2D<float>.Zero;

    [ComponentProperty]
    [TemplateProperty]
    protected SizeMode _horizontalSizeMode = SizeMode.Relative;

    [ComponentProperty]
    [TemplateProperty]
    protected SizeMode _verticalSizeMode = SizeMode.Relative;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _relativeSize = Vector2D<float>.One;

    [ComponentProperty]
    [TemplateProperty]
    protected Padding _padding = new(0);

    [ComponentProperty]
    [TemplateProperty]
    protected SafeArea _safeArea = SafeArea.Zero;

    [ComponentProperty]
    [TemplateProperty]
    protected bool _visible = true;

#pragma warning disable CS0414 // Field is assigned but its value is never used
    private bool _isLayoutInvalid = true;
#pragma warning restore CS0414

    public void InvalidateLayout()
    {
        _isLayoutInvalid = true;
    }

    public Vector2D<float> Measure() => CalculateIntrinsicSize();

    public Vector2D<float> Measure(Vector2D<float> availableSize)
    {
        // Compute desired size per-axis using the independent size modes and relative modifiers
        float desiredWidth;
        switch (HorizontalSizeMode)
        {
            case SizeMode.Fixed:
                desiredWidth = Size.X;
                break;
            case SizeMode.Relative:
                if (Math.Abs(RelativeSize.X) <= float.Epsilon)
                    desiredWidth = availableSize.X;
                else
                    desiredWidth = availableSize.X * RelativeSize.X;
                break;
            case SizeMode.Absolute:
                desiredWidth = availableSize.X;
                if (Math.Abs(RelativeSize.X) > float.Epsilon)
                    desiredWidth = Math.Max(0, desiredWidth + RelativeSize.X);
                break;
            case SizeMode.FitContent:
                desiredWidth = CalculateIntrinsicSize().X;
                break;
            default:
                desiredWidth = Size.X;
                break;
        }

        float desiredHeight;
        switch (VerticalSizeMode)
        {
            case SizeMode.Fixed:
                desiredHeight = Size.Y;
                break;
            case SizeMode.Relative:
                if (Math.Abs(RelativeSize.Y) <= float.Epsilon)
                    desiredHeight = availableSize.Y;
                else
                    desiredHeight = availableSize.Y * RelativeSize.Y;
                break;
            case SizeMode.Absolute:
                desiredHeight = availableSize.Y;
                if (Math.Abs(RelativeSize.Y) > float.Epsilon)
                    desiredHeight = Math.Max(0, desiredHeight + RelativeSize.Y);
                break;
            case SizeMode.FitContent:
                desiredHeight = CalculateIntrinsicSize().Y;
                break;
            default:
                desiredHeight = Size.Y;
                break;
        }

        var desired = new Vector2D<float>(desiredWidth, desiredHeight);

        // Apply min/max constraints
        if (MinSize.X > 0 && desired.X < MinSize.X) desired.X = MinSize.X;
        if (MinSize.Y > 0 && desired.Y < MinSize.Y) desired.Y = MinSize.Y;
        if (MaxSize.X > 0 && desired.X > MaxSize.X) desired.X = MaxSize.X;
        if (MaxSize.Y > 0 && desired.Y > MaxSize.Y) desired.Y = MaxSize.Y;

        return desired;
    }

    public void UpdateLayout()
    {
        UpdateLayout(new Rectangle<float>(0, 0, Size.X, Size.Y));
    }

    public virtual void UpdateLayout(Rectangle<float> constraints)
    {
        if (constraints.Size.X <= 0 || constraints.Size.Y <= 0)
        {
            return;
        }

        var intrinsicSize = CalculateIntrinsicSize();
        
        var preferredWidth = HorizontalSizeMode switch
        {
            SizeMode.Fixed => Size.X > 0 ? Size.X : (intrinsicSize.X > 0 ? intrinsicSize.X : constraints.Size.X),
            SizeMode.Relative => Math.Max(0, constraints.Size.X * RelativeSize.X),
            SizeMode.Absolute => Math.Max(0, constraints.Size.X + RelativeSize.X),
            SizeMode.FitContent => intrinsicSize.X > 0 ? intrinsicSize.X : constraints.Size.X,
            _ => Size.X,
        };

        var preferredHeight = VerticalSizeMode switch
        {
            SizeMode.Fixed => Size.Y > 0 ? Size.Y : (intrinsicSize.Y > 0 ? intrinsicSize.Y : constraints.Size.Y),
            SizeMode.Relative => Math.Max(0, constraints.Size.Y * RelativeSize.Y),
            SizeMode.Absolute => Math.Max(0, constraints.Size.Y + RelativeSize.Y),
            SizeMode.FitContent => intrinsicSize.Y > 0 ? intrinsicSize.Y : constraints.Size.Y,
            _ => Size.Y,
        };

        var preferredSize = new Vector2D<float>(preferredWidth, preferredHeight);
        var finalSize = ApplySizeConstraints(preferredSize);
        SetSize(finalSize);

        // Alignment uses 0-1 range: 0=left/top, 0.5=center, 1=right/bottom
        // Position element within constraints based on alignment
        var alignedX = constraints.Origin.X + Alignment.X * constraints.Size.X + Offset.X;
        var alignedY = constraints.Origin.Y + Alignment.Y * constraints.Size.Y + Offset.Y;
        SetPosition(new Vector2D<float>(alignedX, alignedY));
        
        // Layout children
        LayoutChildren();
        
        _isLayoutInvalid = false;
    }

    protected virtual Vector2D<float> CalculateIntrinsicSize()
    {
        var childElements = GetChildren<IUserInterfaceElement>().ToList();
        if (childElements.Count == 0)
            return Vector2D<float>.Zero;
        
        float maxX = 0, maxY = 0;
        foreach (var elem in childElements)
        {
            var childBounds = elem.Bounds;
            maxX = Math.Max(maxX, childBounds.Origin.X + childBounds.Size.X);
            maxY = Math.Max(maxY, childBounds.Origin.Y + childBounds.Size.Y);
        }
        return new Vector2D<float>(maxX, maxY);
    }

    protected Vector2D<float> ApplySizeConstraints(Vector2D<float> size)
    {
        var width = size.X;
        var height = size.Y;
        
        if (MinSize.X > 0 && width < MinSize.X) width = MinSize.X;
        if (MinSize.Y > 0 && height < MinSize.Y) height = MinSize.Y;
        
        if (MaxSize.X > 0 && width > MaxSize.X) width = MaxSize.X;
        if (MaxSize.Y > 0 && height > MaxSize.Y) height = MaxSize.Y;
        
        return new Vector2D<float>(width, height);
    }

    protected virtual void LayoutChildren()
    {
        // Check for LayoutController components
        var layoutControllers = GetChildren<LayoutController>().ToList();
        
        if (layoutControllers.Count > 0)
        {
            foreach (var controller in layoutControllers)
            {
                controller.UpdateLayout(this);
            }
        }
        else
        {
            // Default layout: fill content area
            var contentArea = GetContentRect();
            var children = GetChildren<IUserInterfaceElement>().ToList();
            
            foreach (var child in children)
            {
                child.UpdateLayout(contentArea);
            }
        }
    }

    public Rectangle<float> GetContentRect()
    {
        // Calculate the content area in local coordinate space
        // This is where children will be positioned
        var size = Size;
        
        // Calculate effective padding including SafeArea
        var safeMargins = SafeArea.CalculateMargins(new Vector2D<int>((int)size.X, (int)size.Y));
        var effectivePaddingLeft = Padding.Left + safeMargins.Left;
        var effectivePaddingTop = Padding.Top + safeMargins.Top;
        var effectivePaddingRight = Padding.Right + safeMargins.Right;
        var effectivePaddingBottom = Padding.Bottom + safeMargins.Bottom;
        
        // Content area size after padding
        var contentWidth = Math.Max(0, size.X - effectivePaddingLeft - effectivePaddingRight);
        var contentHeight = Math.Max(0, size.Y - effectivePaddingTop - effectivePaddingBottom);
        
        // The content rectangle is centered at the element's local origin (0,0)
        // with the content size, so children use alignment relative to this centered area
        var contentLeft = -contentWidth / 2f;
        var contentTop = -contentHeight / 2f;
        
        return new Rectangle<float>(contentLeft, contentTop, contentWidth, contentHeight);
    }
}