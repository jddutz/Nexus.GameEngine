namespace Nexus.GameEngine.GUI.Layout;
using System.Linq;

/// <summary>
/// Abstract base class for layout components that arrange child components.
/// Provides common functionality for collecting and positioning child components.
/// Implements lazy invalidation with automatic recalculation during OnUpdate.
/// </summary>
public partial class Container(IDescriptorManager descriptorManager)
    : Element(descriptorManager), ILayout
{
    /// <summary>
    /// Padding around the layout container.
    /// Uses CancelAnimation to prevent layout thrashing during property changes.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected Padding _padding = new(0);

    /// <summary>
    /// Safe-area margins for avoiding unsafe areas (notches, rounded corners, etc.).
    /// </summary>
    [ComponentProperty()]
    [TemplateProperty]
    protected SafeArea _safeArea = SafeArea.Zero;

    private bool _isLayoutInvalid = true;

    public bool IsLayoutInvalid => _isLayoutInvalid;

    /// <summary>
    /// Marks layout as needing recalculation.
    /// </summary>
    public void Invalidate()
    {
        _isLayoutInvalid = true;
    }

    /// <summary>
    /// Called when a child's size changes (for intrinsic sizing support).
    /// </summary>
    public void OnChildSizeChanged(IComponent child, Vector2D<int> oldValue)
    {
        // If this layout uses FitContent sizing, invalidate when children change size
        if (SizeMode == SizeMode.FitContent)
        {
            Invalidate();
        }
    }

    /// <summary>
    /// Called during activation. Ensures layout is performed and subscribes to child changes.
    /// Skips Element's rendering setup (textures/geometry) since layouts don't render themselves.
    /// </summary>
    protected override void OnActivate()
    {
        // Call base activation (Element/Transformable) - Element no longer performs rendering setup
        base.OnActivate();

        // By default, layout containers should fill the available space provided by their parent/viewport.
        // Ensure the SizeMode is Absolute so that SetSizeConstraints from the parent (e.g. the root viewport)
        // will cause the container to adopt the full available size instead of remaining at the default
        // Fixed/zero size.
        SetSizeMode(SizeMode.Absolute);

        // Also propagate to per-axis size modes and apply updates immediately so that
        // activation-time layout logic (which may run before the usual update loop)
        // sees the container as Absolute (filling parent).
        SetHorizontalSizeMode(SizeMode.Absolute);
        SetVerticalSizeMode(SizeMode.Absolute);
        ApplyUpdates(0);

        UpdateLayout();

        ChildCollectionChanged += OnChildCollectionChanged;
    }

    /// <summary>
    /// Called every frame. Recalculates layout if invalidated.
    /// </summary>
    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_isLayoutInvalid)
        {
            UpdateLayout();
            _isLayoutInvalid = false;
        }
    }

    /// <summary>
    /// Called when child collection changes.
    /// </summary>
    private void OnChildCollectionChanged(object? sender, ChildCollectionChangedEventArgs e)
    {
        Invalidate();
    }

    /// <summary>
    /// Arranges child components within the layout's bounds.
    /// Default implementation: positions all children to fill the area inside padding.
    /// Override in derived classes to implement specific layout algorithms (stacking, grid, etc.).
    /// </summary>
    protected virtual void UpdateLayout()
    {
        var children = GetChildren<IComponent>().OfType<IUserInterfaceElement>().ToArray();
        if (children.Length == 0) return;

        var contentArea = new Rectangle<int>(
            (int)(TargetPosition.X - (1.0f + AnchorPoint.X) * TargetSize.X * 0.5f) + Padding.Left,
            (int)(TargetPosition.Y - (1.0f + AnchorPoint.Y) * TargetSize.Y * 0.5f) + Padding.Top,
            Math.Max(0, TargetSize.X - Padding.Left - Padding.Right),
            Math.Max(0, TargetSize.Y - Padding.Top - Padding.Bottom)
        );

        // Give each child the full content area (they'll overlap if multiple children)
        foreach (var child in children)
        {            
            child.SetSizeConstraints(contentArea);
        }
    }

    /// <summary>
    /// Gets effective padding combining base Padding with SafeArea margins.
    /// Uses target size to handle deferred updates correctly.
    /// </summary>
    protected Padding GetEffectivePadding()
    {
        // Use TargetSize for SafeArea calculations to handle deferred updates
        var size = TargetSize;
        var safeMargins = SafeArea.CalculateMargins(size);
        
        return new Padding(
            Padding.Left + safeMargins.Left,
            Padding.Top + safeMargins.Top,
            Padding.Right + safeMargins.Right,
            Padding.Bottom + safeMargins.Bottom
        );
    }

    /// <summary>
    /// Called when layout is deactivated. Unsubscribes from child changes.
    /// </summary>
    protected override void OnDeactivate()
    {
        ChildCollectionChanged -= OnChildCollectionChanged;
        base.OnDeactivate();
    }

    /// <summary>
    /// Sets size constraints and invalidates layout to ensure recalculation.
    /// </summary>
    public override void SetSizeConstraints(Rectangle<int> constraints)
    {
        base.SetSizeConstraints(constraints);
        Invalidate();
    }
}