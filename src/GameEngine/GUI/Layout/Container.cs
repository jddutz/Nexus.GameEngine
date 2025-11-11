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
    /// Space between children (x=horizontal, y=vertical).
    /// Uses CancelAnimation to prevent layout thrashing during property changes.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _spacing = new(0, 0);

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
    /// Called when size constraints change.
    /// Layout invalidates and recalculates on next update.
    /// </summary>
    protected override void OnSizeConstraintsChanged(Rectangle<int> constraints)
    {
        base.OnSizeConstraintsChanged(constraints);
        // Mark layout invalid and perform an immediate layout pass so child
        // constraints are updated synchronously when SetSizeConstraints is called.
        // This mirrors previous behavior where layout was expected to run
        // immediately in many tests.
        Invalidate();
        UpdateLayout();
        _isLayoutInvalid = false;
    }

    /// <summary>
    /// Called when a child's size changes (for intrinsic sizing support).
    /// </summary>
    public void OnChildSizeChanged(IComponent child, Vector2D<int> oldValue)
    {
        // If this layout uses intrinsic sizing, invalidate when children change size
        if (SizeMode == SizeMode.Intrinsic)
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
        // Ensure the SizeMode is Stretch so that SetSizeConstraints from the parent (e.g. the root viewport)
        // will cause the container to adopt the full available size instead of remaining at the default
        // Fixed/zero size.
    SetSizeMode(SizeMode.Stretch);

    // Also propagate to per-axis size modes and apply updates immediately so that
    // activation-time layout logic (which may run before the usual update loop)
    // sees the container as Stretch.
    SetHorizontalSizeMode(SizeMode.Stretch);
    SetVerticalSizeMode(SizeMode.Stretch);
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

        var contentArea = GetContentArea();

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
    /// Calculates the content area available for child elements.
    /// This is the layout's bounds minus the effective padding.
    /// Uses target position/size if deferred updates are pending to ensure layout calculations
    /// use the intended size rather than waiting for the next frame.
    /// </summary>
    protected Rectangle<int> GetContentArea()
    {
        // Use target position/size so layout calculations operate on the intended
        // values even if deferred updates haven't been applied to the runtime fields yet.
        return new Rectangle<int>(
            (int)TargetPosition.X + Padding.Left,
            (int)TargetPosition.Y + Padding.Top,
            Math.Max(0, TargetSize.X - Padding.Left - Padding.Right),
            Math.Max(0, TargetSize.Y - Padding.Top - Padding.Bottom)
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
}