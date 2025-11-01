namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Abstract base class for layout components that arrange child components.
/// Provides common functionality for collecting and positioning child components.
/// </summary>
/// <typeparam name="TTemplate">The template type for this layout component</typeparam>
public abstract partial class Layout(
    IPipelineManager pipelineManager)
    : Element(pipelineManager)
{
    /// <summary>
    /// Template for configuring Layout components.
    /// Defines the properties for arranging child components.
    /// </summary>
    public new record Template : Element.Template
    {

        /// <summary>
        /// Padding around the layout container.
        /// </summary>
        public Padding Padding { get; init; } = Padding.Zero;
    }

    /// <summary>
    /// Padding around the layout container.
    /// </summary>
    [ComponentProperty]
    private Padding _padding = new(0);

    private bool _isLayoutInvalid = true;

    public bool IsLayoutInvalid => _isLayoutInvalid;

    public void Invalidate()
    {
        _isLayoutInvalid = true;
    }

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        base.OnLoad(componentTemplate);

        if (componentTemplate is Template template)
        {
            SetPadding(template.Padding);
        }
    }

    /// <summary>
    /// Called when size constraints change.
    /// Layout invalidates and recalculates on next update.
    /// </summary>
    protected override void OnSizeConstraintsChanged(Rectangle<int> constraints)
    {
        base.OnSizeConstraintsChanged(constraints);
        Invalidate();
    }

    /// <summary>
    /// Called during activation. Ensures layout is performed.
    /// </summary>
    protected override void OnActivate()
    {
        base.OnActivate();
        
        UpdateLayout();

        ChildCollectionChanged += OnChildCollectionChanged;
    }    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_isLayoutInvalid) UpdateLayout();
    }

    public override void UpdateGeometry()
    {
        base.UpdateGeometry();
        Invalidate();
    }

    private void OnChildCollectionChanged(object? sender, ChildCollectionChangedEventArgs e)
    {
        Invalidate();
    }

    /// <summary>
    /// Arranges child components within the layout's bounds.
    /// Override in derived classes to implement specific layout algorithms.
    /// </summary>
    protected virtual void UpdateLayout()
    {
        // Base implementation does nothing - child layouts implement specific arrangement logic
    }
}