using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Animation;

namespace Nexus.GameEngine.GUI.Abstractions;

/// <summary>
/// Abstract base class for layout components that arrange child components.
/// Provides common functionality for collecting and positioning child components.
/// </summary>
/// <typeparam name="TTemplate">The template type for this layout component</typeparam>
public abstract partial class LayoutBase : RuntimeComponent, IDrawable
{

    /// <summary>
    /// Template for configuring Layout components.
    /// Defines the properties for arranging child components.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        Vector2D<float> PreferredSize { get; set; }

        /// <summary>
        /// Padding around the layout container.
        /// </summary>
        public Padding Padding { get; init; } = Padding.Zero;
    }

    IWindow? _window;
    private bool _needsLayout = true;
    private Vector2D<float> _size = new(0, 0);

    protected LayoutBase(IWindowService windowService)
        : base()
    {
        // Subscribe to window resize events if we have a window service
        _window = windowService.GetWindow();
        if (_window != null)
        {
            _window.Resize += OnWindowResize;
        }
    }
    
    /// <summary>
    /// Whether this component should be rendered.
    /// When false, GetDrawCommands will not be called and component is skipped during rendering.
    /// Generated property: IsVisible (read-only), SetIsVisible(...) method for updates.
    /// </summary>
    [ComponentProperty(Duration = AnimationDuration.Fast, Interpolation = InterpolationMode.Step)]
    private bool _isVisible = true;


    public Vector2D<float> PreferredSize { get; set; }

    /// <summary>
    /// Size of this layout container.
    /// </summary>
    public Vector2D<float> Size
    {
        get => _size;
        set
        {
            if (_size != value)
            {
                _size = value;
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Whether this layout needs to be recalculated.
    /// </summary>
    public bool NeedsLayout => _needsLayout;

    /// <summary>
    /// Marks the layout as needing recalculation.
    /// </summary>
    public void InvalidateLayout()
    {
        _needsLayout = true;
    }

    /// <summary>
    /// Gets the available size for layout calculations.
    /// First tries to get size from parent layout, then falls back to window size.
    /// </summary>
    /// <returns>Available size for this layout</returns>
    protected Vector2D<float> GetAvailableSize()
    {
        // If this layout has an explicit size set, use it
        if (_size.X > 0 && _size.Y > 0)
        {
            return _size;
        }

        // Try to get size from parent layout component
        if (Parent is UserInterfaceComponent parentUi)
        {
            var parentBounds = parentUi.GetBounds();
            return new Vector2D<float>(parentBounds.Size.X, parentBounds.Size.Y);
        }

        // Fall back to window size if available
        if (_window != null)
        {
            return new Vector2D<float>(_window.Size.X, _window.Size.Y);
        }

        // Default fallback size if no other options available
        return new Vector2D<float>(800, 600);
    }

    /// <summary>
    /// Handles window resize events to invalidate layout.
    /// </summary>
    private void OnWindowResize(Vector2D<int> newSize)
    {
        InvalidateLayout();
    }

    /// <summary>
    /// Performs layout of child components if needed.
    /// Collects all UserInterfaceComponent immediate child components and calls OnLayout.
    /// </summary>
    public void Layout()
    {
        if (!_needsLayout || !IsEnabled)
            return;

        // Collect all immediate child components that are UI components
        var uiChildren = Children
            .Where(c => c.IsEnabled)
            .OfType<UserInterfaceComponent>()
            .ToList();

        // Call the derived class implementation
        OnLayout(uiChildren);

        _needsLayout = false;
    }

    /// <summary>
    /// Override in derived classes to implement specific layout logic.
    /// </summary>
    /// <param name="children">Collection of UI child components to arrange</param>
    protected abstract void OnLayout(IReadOnlyList<UserInterfaceComponent> children);

    /// <summary>
    /// Called when a child component is added. Invalidates layout.
    /// </summary>
    public override void AddChild(IRuntimeComponent child)
    {
        base.AddChild(child);
        InvalidateLayout();
    }

    /// <summary>
    /// Called when a child component is removed. Invalidates layout.
    /// </summary>
    public override void RemoveChild(IRuntimeComponent child)
    {
        base.RemoveChild(child);
        InvalidateLayout();
    }

    /// <summary>
    /// Called during activation. Ensures layout is performed.
    /// </summary>
    protected override void OnActivate()
    {
        base.OnActivate();
        InvalidateLayout();
    }

    public uint RenderPriority => 420; // UI layout layer

    /// <summary>
    /// Bounding box for layout components. Returns minimal box since these are UI elements.
    /// </summary>
    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero);

    /// <summary>
    /// Layout components participate in UI render pass (pass 1).
    /// </summary>
    public uint RenderPassFlags => 1u << 1; // UI pass

    /// <summary>
    /// Layout components should render their children.
    /// </summary>
    public bool ShouldRenderChildren => true;

    /// <summary>
    /// Layout components don't issue their own draw commands - they arrange children.
    /// </summary>
    public virtual IEnumerable<DrawCommand> GetDrawCommands(RenderContext context) => [];

    protected override void OnDeactivate()
    {
        if (_window != null)
        {
            _window.Resize -= OnWindowResize;
        }

        base.OnDeactivate();
    }
}