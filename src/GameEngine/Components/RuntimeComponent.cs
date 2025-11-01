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
    /// Whether this component is currently active and should participate in updates.
    /// Changes to Active are deferred until the next frame boundary to ensure temporal consistency.
    /// Use IsActive() to check if component is both Enabled and Active.
    /// </summary>
    [ComponentProperty]
    protected bool _active = false;

    /// <summary>
    /// Returns whether this component is currently active and enabled.
    /// This is the combined state that determines if the component participates in updates.
    /// </summary>
    public bool IsActive() => IsValid && IsLoaded && Active;

    public event EventHandler<EventArgs>? Activating;
    public event EventHandler<EventArgs>? Activated;

    protected virtual void OnActivate() { }

    public void Activate()
    {
        // Validate before activation (uses cached results if available)
        if (!Validate())
        {
            return;
        }

        Activating?.Invoke(this, EventArgs.Empty);

        // Activate root to leaf
        OnActivate();

        foreach (var child in Children.OfType<IRuntimeComponent>())
        {
            child.Activate();
        }

        // Set active state after successful activation (deferred until next frame)
        SetActive(true);

        Activated?.Invoke(this, EventArgs.Empty);
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

    protected virtual void OnDeactivate() { }

    public void Deactivate()
    {
        if (!IsActive()) return;

        Deactivating?.Invoke(this, EventArgs.Empty);

        // Set inactive state before deactivating children (deferred until next frame)
        SetActive(false);

        // Deactivate leaf to root
        foreach (var child in Children.OfType<IRuntimeComponent>())
        {
            child.Deactivate();
        }

        OnDeactivate();

        Deactivated?.Invoke(this, EventArgs.Empty);

        SetActive(false);
    }
}
