namespace Nexus.GameEngine.Components;

/// <summary>
/// Base class for all runtime components in the system.
/// Provides component tree management, lifecycle methods, and configuration.
/// Implements IRuntimeComponent, which triggers source generation of animated property implementations.
/// </summary>
public partial class RuntimeComponent
    : Component, IRuntimeComponent
{
    /// <summary>
    /// Parameterless constructor for dependency injection.
    /// Components are instantiated via DI and configured through templates.
    /// </summary>
    public RuntimeComponent() { }

    /// <summary>
    /// Whether this component is currently active and should participate in updates.
    /// Changes to Active are deferred until the next frame boundary to ensure temporal consistency.
    /// Use IsActive() to check if component is both Enabled and Active.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected bool _active = false;

    private PropertyBindings? _bindings;

    protected override void OnLoad(Template? template)
    {
        base.OnLoad(template);
        if (template != null)
        {
            _bindings = template.Bindings;
        }
    }

    /// <summary>
    /// Returns whether this component is currently active and enabled.
    /// This is the combined state that determines if the component participates in updates.
    /// </summary>
    public bool IsActive() => IsValid() && IsLoaded && Active;

    public event EventHandler<EventArgs>? Activating;
    public event EventHandler<EventArgs>? Activated;

    protected virtual void OnActivate() 
    {
        if (_bindings != null)
        {
            foreach (var (propName, binding) in _bindings)
            {
                binding.Activate(this, propName);
            }
        }
    }

    public void Activate()
    {
        if (Parent is IRuntimeComponent parent && !parent.IsActive()) return;

        // Validate before activation (uses cached results if available, validates if cache is null)
        if (!IsValid()) return;

        Activating?.Invoke(this, EventArgs.Empty);

        OnActivate();

        // Set active state immediately (not deferred) BEFORE activating children
        // so that child validation can check parent.IsActive() correctly
        _active = true;

        ActivateChildren();

        Activated?.Invoke(this, EventArgs.Empty);
    }

    public virtual void ActivateChildren()
    {
        // Only activate immediate children, not grandchildren
        // Each child's Activate() will handle its own children
        foreach(var child in Children.OfType<IRuntimeComponent>())
        {
            child.Activate();
        }
    }

    public virtual void ActivateChildren<TChild>()
        where TChild : IRuntimeComponent
    {
        // Only activate immediate children, not grandchildren
        foreach(var child in Children.OfType<TChild>())
        {
            child.Activate();
        }
    }

    /// <summary>
    /// Flag indicating whether or not the component is updating
    /// Used to suppress unwanted event notifications and tree propagation
    /// </summary>
    public bool IsUpdating { get; private set; } = false;

    public event EventHandler<EventArgs>? Updating;
    public event EventHandler<EventArgs>? Updated;

    /// <summary>
    /// Override this in derived classes for custom update logic.
    /// Called after ApplyUpdates() has been processed.
    /// </summary>
    protected virtual void OnUpdate(double deltaTime) { }

    /// <summary>
    /// Updates the component and its children.
    /// This method is sealed to ensure ApplyUpdates is called before OnUpdate.
    /// </summary>
    public void Update(double deltaTime)
    {
        IsUpdating = true;

        Updating?.Invoke(this, EventArgs.Empty);

        // Check if component is active (deferred updates already applied by ContentManager)
        if (!IsActive())
        {
            IsUpdating = false;
            return;
        }

        // Create a snapshot of children to allow collection modifications during update
        foreach (var child in Children.OfType<IRuntimeComponent>().ToArray())
        {
            child.Update(deltaTime);
        }

        // All children should be up to date when OnUpdate is called
        // All updates are deferred to the next frame for temporal consistency
        OnUpdate(deltaTime);

        Updated?.Invoke(this, EventArgs.Empty);
        IsUpdating = false;
    }

    public event EventHandler<EventArgs>? Deactivating;
    public event EventHandler<EventArgs>? Deactivated;

    protected virtual void OnDeactivate() 
    {
        if (_bindings != null)
        {
            foreach (var (_, binding) in _bindings)
            {
                binding.Deactivate();
            }
        }
    }

    public void Deactivate()
    {
        if (!IsActive()) return;

        Deactivating?.Invoke(this, EventArgs.Empty);

        // Set inactive state immediately (not deferred) so children can check IsActive() correctly
        _active = false;

        // Deactivate leaf to root
        foreach (var child in Children.OfType<IRuntimeComponent>())
        {
            child.Deactivate();
        }

        OnDeactivate();

        Deactivated?.Invoke(this, EventArgs.Empty);
    }
}
