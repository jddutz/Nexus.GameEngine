using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Base interface for all components in the system.
/// Implements universal lifecycle methods that all components must support.
/// </summary>
public interface IRuntimeComponent : IDisposable
{
    /// <summary>
    /// Factory used to create new components.
    /// </summary>
    public IComponentFactory? ComponentFactory { get; set; }

    /// <summary>
    /// Logger, for logging of course
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Unique identifier for this component instance.
    /// Automatically generated and managed internally.
    /// </summary>
    ComponentId Id { get; }

    /// <summary>
    /// Human-readable name for this component instance.
    /// Used for development-time component lookup and debugging.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Whether this component is currently enabled and should participate in updates.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Whether this component is currently unloaded and can be disposed.
    /// </summary>
    bool IsUnloaded { get; set; }

    /// <summary>
    /// Whether this component is currently active (successfully activated and valid).
    /// Components automatically become inactive when validation fails.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets whether the component is valid (no validation errors).
    /// Triggers validation if not already cached.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets the current validation errors for this component.
    /// Empty collection indicates the component is valid.
    /// </summary>
    IEnumerable<ValidationError> ValidationErrors { get; }

    #region Events

    // Configuration Events
    event EventHandler<ConfigurationEventArgs>? BeforeConfiguration;
    event EventHandler<ConfigurationEventArgs>? AfterConfiguration;

    // Validation Events
    event EventHandler<EventArgs>? Validating;
    event EventHandler<EventArgs>? Validated;
    event EventHandler<EventArgs>? ValidationFailed;

    // Lifecycle Events
    event EventHandler<EventArgs>? Activating;
    event EventHandler<EventArgs>? Activated;
    event EventHandler<EventArgs>? Updating;
    event EventHandler<EventArgs>? Updated;
    event EventHandler<EventArgs>? Deactivating;
    event EventHandler<EventArgs>? Deactivated;

    // Tree Management Events
    event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged;

    #endregion

    /// <summary>
    /// Configure the component using the specified template.
    /// Automatically triggers re-validation of the component.
    /// </summary>
    /// <param name="template">Template used for configuration.</param>
    void Configure(IComponentTemplate template);

    /// <summary>
    /// Validate this component and all its subcomponents.
    /// Stores validation errors internally and returns them.
    /// Subsequent calls return cached results until configuration changes.
    /// </summary>
    /// <returns>Collection of validation errors</returns>
    bool Validate(bool ignoreCached = false);

    /// <summary>
    /// Activate this component and all its subcomponents.
    /// Sets up event subscriptions and prepares for operation in root-to-leaf order.
    /// Parent components must be activated before children to provide proper event contexts.
    /// </summary>
    void Activate();

    /// <summary>
    /// Update this component and all its subcomponents.
    /// </summary>
    /// <param name="deltaTime">Time elapsed in seconds since the previous update.</param>
    void Update(double deltaTime);

    /// <summary>
    /// Applies all queued deferred property updates.
    /// Called by the renderer before rendering each component to ensure temporal consistency.
    /// </summary>
    void ApplyUpdates();

    /// <summary>
    /// Deactivate this component and all its subcomponents, preparing for disposal.
    /// Deactivates all subcomponents then calls OnDeactivate().
    /// To disable an active component and stop handling events, set IsEnabled to false.
    /// </summary>
    void Deactivate();

    /// <summary>
    /// Parent component in the component tree.
    /// Automatically set when AddChild() is called on the parent.
    /// Read-only from outside the component; the tree structure is managed internally.
    /// </summary>
    IRuntimeComponent? Parent { get; }

    /// <summary>
    /// Child components of this component.
    /// Use AddChild() and RemoveChild() methods to modify the collection.
    /// </summary>
    IEnumerable<IRuntimeComponent> Children { get; }

    /// <summary>
    /// Searches the component children recursively, returning all child components
    /// of the specified type <typeparamref name="T"/>.
    /// Optionally filters the children using the provided predicate.
    /// </summary>
    /// <typeparam name="T">The type of child components to return.</typeparam>
    /// <param name="filter">Optional predicate to filter child components.</param>
    /// <returns>Enumerable of child components of type <typeparamref name="T"/>.</returns>
    IEnumerable<T> GetChildren<T>(Func<T, bool>? filter = null)
        where T : IRuntimeComponent;

    /// <summary>
    /// Returns all sibling components of the specified type <typeparamref name="T"/>.
    /// Optionally filters the siblings using the provided predicate.
    /// </summary>
    /// <typeparam name="T">The type of sibling components to return.</typeparam>
    /// <param name="filter">Optional predicate to filter sibling components.</param>
    /// <returns>Enumerable of sibling components of type <typeparamref name="T"/>.</returns>
    IEnumerable<T> GetSiblings<T>(Func<T, bool>? filter = null)
        where T : IRuntimeComponent;

    /// <summary>
    /// Finds the nearest parent component of the specified type <typeparamref name="T"/>.
    /// Optionally filters parent components using the provided predicate.
    /// </summary>
    /// <typeparam name="T">The type of parent component to find.</typeparam>
    /// <param name="filter">Optional predicate to filter parent components.</param>
    /// <returns>The nearest parent component of type <typeparamref name="T"/>, or default if not found.</returns>
    T? FindParent<T>(Func<T, bool>? filter = null)
        where T : IRuntimeComponent;

    /// <summary>
    /// Adds a child component to this tree node.
    /// </summary>
    /// <param name="child">The child component to add</param>
    void AddChild(IRuntimeComponent child);

    /// <summary>
    /// Removes a child component from this tree node.
    /// </summary>
    /// <param name="child">The child component to remove</param>
    void RemoveChild(IRuntimeComponent child);
}