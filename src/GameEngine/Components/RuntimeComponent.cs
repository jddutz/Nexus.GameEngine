using Microsoft.Extensions.Logging;

namespace Nexus.GameEngine.Components;


/// <summary>
/// Base class for all runtime components in the system.
/// Provides component tree management, lifecycle methods, and configuration.
/// Implements IRuntimeComponent, which triggers source generation of animated property implementations.
/// </summary>
public partial class RuntimeComponent : ComponentBase, IRuntimeComponent, IDisposable
{
    /// <summary>
    /// Interface for component templates that define design-time component structure.
    /// Templates are instantiated into runtime RuntimeComponent instances.
    /// </summary>
    public record Template : IComponentTemplate
    {
        // <inheritdoc/>
        public ComponentId Id { get; set; } = ComponentId.None;

        // <inheritdoc/>
        public string Name { get; set; } = string.Empty;

        // <inheritdoc/>
        public bool Enabled { get; set; } = true;

        // <inheritdoc/>
        public IComponentTemplate[] Subcomponents { get; set; } = [];
    }

    #region Deferred Updates

    /// <summary>
    /// Queue of deferred property updates to be applied before rendering.
    /// </summary>
    private readonly List<Action> _deferredUpdates = [];

    /// <summary>
    /// Queues an update to be executed during the next ApplyUpdates() call.
    /// Used by derived classes to defer property changes until frame boundaries.
    /// </summary>
    /// <param name="update">The update action to queue</param>
    protected void QueueUpdate(Action update)
    {
        if (update == null) throw new ArgumentNullException(nameof(update));

        _deferredUpdates.Add(update);

        Logger?.LogTrace("Queued deferred update. Total pending: {PendingCount}", _deferredUpdates.Count);
    }

    /// <summary>
    /// Applies all queued deferred updates.
    /// Called by the renderer before rendering each component.
    /// </summary>
    public void ApplyUpdates()
    {
        if (_deferredUpdates.Count == 0) return;

        Logger?.LogTrace("Applying {UpdateCount} deferred updates", _deferredUpdates.Count);

        foreach (var update in _deferredUpdates)
        {
            try
            {
                update();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error applying deferred update");
            }
        }

        _deferredUpdates.Clear();

        // Trigger validation after all updates have been applied
        Validate();

        Logger?.LogTrace("Finished applying deferred updates");
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Whether this component is currently enabled and should participate in updates.
    /// </summary>
    private bool _configured = true;
    public bool IsConfigured
    {
        get => _configured;
        set
        {
            if (_configured == value) return;

            _configured = value;
            NotifyPropertyChanged();
        }
    }

    // Configuration Events
    public event EventHandler<ConfigurationEventArgs>? BeforeConfiguration;
    public event EventHandler<ConfigurationEventArgs>? AfterConfiguration;

    /// <summary>
    /// Override in derived classes to implement component-specific configuration.
    /// Parent is configured before calling this method.
    /// </summary>
    /// <param name="template">Template used for configuration</param>
    protected virtual void OnConfigure(IComponentTemplate componentTemplate) { }

    /// <summary>
    /// Configure the component using the specified template.
    /// Base implementation calls OnConfigure() for component-specific configuration,
    /// then applies initial property animations via UpdateAnimations(0).
    /// This method is sealed to ensure UpdateAnimations is called after OnConfigure.
    /// </summary>
    /// <param name="componentTemplate">The template to configure from.</param>
    public void Configure(IComponentTemplate componentTemplate)
    {
        if (componentTemplate == null)
        {
            Logger?.LogDebug("Configure called on {TypeName} with null template", GetType().Name);
            return;
        }

        Logger?.LogDebug("Configure called on {TypeName} with template: {ComponentTypeName}", GetType().Name, componentTemplate.GetType().Name);

        Name = componentTemplate.Name;
        IsEnabled = componentTemplate.Enabled;

        Logger?.LogDebug("Component name set to: '{Name}', enabled: {IsEnabled}", Name, IsEnabled);

        BeforeConfiguration?.Invoke(this, new(componentTemplate));

        // Configure root to leaf
        OnConfigure(componentTemplate);

        // Apply any pending property changes after configuration (instant update with deltaTime = 0)
        UpdateAnimations(0);

        if (componentTemplate is Template template)
        {
            Logger?.LogDebug("Template is RuntimeComponent.Template with {SubcomponentCount} subcomponents", template.Subcomponents?.Length ?? 0);

            if (template.Subcomponents != null)
            {
                foreach (var subcomponentTemplate in template.Subcomponents)
                {
                    if (subcomponentTemplate != null)
                    {
                        Logger?.LogDebug("Creating child from subcomponent template: {SubComponentName} with name '{SubComponentTemplateName}'", subcomponentTemplate.GetType().Name, subcomponentTemplate.Name ?? "null");
                        var child = CreateChild(subcomponentTemplate);
                        Logger?.LogDebug("Child creation result: {Result}", child != null ? $"SUCCESS - {child.GetType().Name}" : "FAILED");
                    }
                    else
                    {
                        Logger?.LogDebug("Skipping null subcomponent template");
                    }
                }
            }
        }
        else
        {
            Logger?.LogDebug("Template is not RuntimeComponent.Template, it's: {TypeName}", componentTemplate.GetType().Name);
        }

        AfterConfiguration?.Invoke(this, new(componentTemplate));

        Logger?.LogDebug("Configuration completed");
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validation state: null means needs to be validated
    /// </summary>
    private bool? _validationState;

    /// <summary>
    /// Indicates whether the component is in a valid state
    /// </summary>
    public bool IsValid => _validationState == true;

    /// <summary>
    /// Backing list for current validation errors
    /// </summary>
    private List<ValidationError> _validationErrors = [];

    /// <summary>
    /// Current validation errors
    /// </summary>
    public IEnumerable<ValidationError> ValidationErrors => _validationErrors;

    public event EventHandler<EventArgs>? Validating;
    public event EventHandler<EventArgs>? Validated;
    public event EventHandler<EventArgs>? ValidationFailed;


    /// <summary>
    /// Clears the cached validation results and fires the validation cache invalidated event.
    /// This method should be called whenever something changes that could affect validation.
    /// Does not trigger re-validation - call Validate() separately if needed.
    /// </summary>
    protected void ClearValidationResults()
    {
        _validationState = null;
        _validationErrors = [];
        // Fire PropertyChanged events directly to avoid infinite recursion through NotifyPropertyChanged
        NotifyPropertyChanged(new(nameof(IsValid)));
        NotifyPropertyChanged(new(nameof(ValidationErrors)));
    }

    protected virtual IEnumerable<ValidationError> OnValidate() => [];

    public bool Validate(bool ignoreCached = false)
    {
        // Return cached results if available
        if (!ignoreCached && _validationState != null)
        {
            Logger?.LogDebug("Using cached validation result: {IsValid}", IsValid);

            // Always log validation errors when using cached results, especially for failed validation
            if (!IsValid && ValidationErrors.Any())
            {
                Logger?.LogDebug("Cached validation errors:");
                foreach (var error in ValidationErrors)
                {
                    Logger?.LogDebug("  - {ErrorMessage} (Severity: {Severity})", error.Message, error.Severity);
                }
            }

            return IsValid;
        }

        Validating?.Invoke(this, EventArgs.Empty);

        // Ignore child validation
        // children will simply not be activated if invalid
        _validationErrors.Clear();
        _validationErrors.AddRange(OnValidate());
        _validationState = _validationErrors.Count == 0; // true when no errors (valid), false when errors exist (invalid)
        if (_validationState == false)
        {
            ValidationFailed?.Invoke(this, EventArgs.Empty);
        }

        // Log validation errors for debugging
        if (ValidationErrors.Any())
        {
            Logger?.LogDebug("validation errors:");
            foreach (var error in ValidationErrors)
            {
                Logger?.LogDebug(
                    "  - {ErrorMessage} (Severity: {Severity})",
                    error.Message,
                    error.Severity
                );
            }
        }

        // Fire PropertyChanged event directly to avoid infinite recursion through NotifyPropertyChanged
        NotifyPropertyChanged(new(nameof(IsValid)));

        if (_validationState == true)
        {
            Validated?.Invoke(this, EventArgs.Empty);
            return true;
        }
        else
        {
            Deactivate();
            return false;
        }
    }

    #endregion

    #region Activation

    /// <summary>
    /// Whether this component is currently active and should participate in updates.
    /// </summary>
    private bool _active = false;
    public bool IsActive
    {
        get => IsEnabled && _active;
        set
        {
            if (_active == value) return;

            _active = value;
            NotifyPropertyChanged();
        }
    }

    public event EventHandler<EventArgs>? Activating;
    public event EventHandler<EventArgs>? Activated;

    protected virtual void OnActivate() { }

    public void Activate()
    {
        // Validate before activation (uses cached results if available)
        if (!Validate())
        {
            Logger?.LogDebug("Validation failed, cannot be activated");
            return;
        }

        Activating?.Invoke(this, EventArgs.Empty);

        // Activate root to leaf
        OnActivate();

        foreach (var child in Children)
        {
            Logger?.LogDebug("Activating child: {ChildType} '{ChildName}'", child.GetType().Name, child.Name);
            child.Activate();
        }

        // Set active state after successful activation
        IsActive = true;

        Activated?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Update

    /// <summary>
    /// Flag indicating whether or not the component is updating
    /// Used to suppress unwanted event notifications and tree propagation
    /// </summary>
    public bool IsUpdating { get; private set; } = false;

    public event EventHandler<EventArgs>? Updating;
    public event EventHandler<EventArgs>? Updated;

    /// <summary>
    /// Override this in derived classes for custom update logic.
    /// Called after UpdateAnimations() has been processed.
    /// </summary>
    protected virtual void OnUpdate(double deltaTime) { }

    /// <summary>
    /// Updates the component and its children.
    /// This method is sealed to ensure UpdateAnimations is called before OnUpdate.
    /// </summary>
    public void Update(double deltaTime)
    {
        if (!IsActive)
        {
            Logger?.LogTrace("Skipping update - not active");
            return;
        }

        IsUpdating = true;

        Updating?.Invoke(this, EventArgs.Empty);

        // Apply deferred updates  root to leaf
        // so deferred updates are done before the first OnUpdate()
        UpdateAnimations(deltaTime);

        foreach (var child in Children)
        {
            child.Update(deltaTime);
        }

        // All children should be up to date when OnUpdate is called
        // All updates are deferred to the next frame for temporal consistency
        OnUpdate(deltaTime);

        Updated?.Invoke(this, EventArgs.Empty);
        IsUpdating = false;
    }

    /// <summary>
    /// Partial method generated by source generator in derived classes.
    /// Updates all animated properties based on elapsed time.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    partial void UpdateAnimations(double deltaTime);

    #endregion

    #region Animation Events

    /// <summary>
    /// Raised when a property animation starts.
    /// </summary>
    public event EventHandler<PropertyAnimationEventArgs>? AnimationStarted;

    /// <summary>
    /// Raised when a property animation completes.
    /// </summary>
    public event EventHandler<PropertyAnimationEventArgs>? AnimationEnded;

    /// <summary>
    /// Called when a property animation starts.
    /// Override in derived classes to respond to animation lifecycle.
    /// </summary>
    protected virtual void OnPropertyAnimationStarted(string propertyName)
    {
        AnimationStarted?.Invoke(this, new PropertyAnimationEventArgs(propertyName));
    }

    /// <summary>
    /// Called when a property animation ends.
    /// Override in derived classes to respond to animation lifecycle.
    /// </summary>
    protected virtual void OnPropertyAnimationEnded(string propertyName)
    {
        AnimationEnded?.Invoke(this, new PropertyAnimationEventArgs(propertyName));
    }

    #endregion

    #region Deactivation

    /// <summary>
    /// Whether this component is currently unloaded and can be disposed.
    /// </summary>
    private bool _unloaded = false;
    public bool IsUnloaded
    {
        get => IsEnabled && _unloaded;
        set
        {
            if (_unloaded == value) return;

            _unloaded = value;
            NotifyPropertyChanged();
        }
    }

    public event EventHandler<EventArgs>? Deactivating;
    public event EventHandler<EventArgs>? Deactivated;

    protected virtual void OnDeactivate() { }

    public void Deactivate()
    {
        if (IsUnloaded)
        {
            Logger?.LogDebug("Already deactivated");
            return; // Already inactive
        }

        Deactivating?.Invoke(this, EventArgs.Empty);

        // Set inactive state before deactivating children
        IsActive = false;

        // Deactivate leaf to root
        foreach (var child in Children)
        {
            Logger?.LogDebug("Deactivating child: {ChildType} '{ChildName}'", child.GetType().Name, child.Name);
            child.Deactivate();
        }

        OnDeactivate();

        Deactivated?.Invoke(this, EventArgs.Empty);

        IsUnloaded = true;
    }

    #endregion

    // Tree Management Events
    public event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged;

    private readonly List<IRuntimeComponent> _children = [];

    /// <summary>
    /// Parent component in the component tree.
    /// Internal setter prevents source generation while allowing tree management.
    /// </summary>
    public virtual IRuntimeComponent? Parent { get; internal set; }

    /// <summary>
    /// Child components of this component.
    /// </summary>
    public virtual IEnumerable<IRuntimeComponent> Children => _children;

    // Component tree management
    public virtual void AddChild(IRuntimeComponent child)
    {
        if (!_children.Contains(child))
        {
            if (string.IsNullOrEmpty(child.Name)) child.Name = child.GetType().Name;

            Logger?.LogDebug("Adding child: {ChildType} '{ChildName}'", child.GetType().Name, child.Name);

            _children.Add(child);

            // Set parent using concrete type to access internal setter
            if (child is RuntimeComponent runtimeChild)
            {
                runtimeChild.Parent = this;
            }

            ChildCollectionChanged?.Invoke(child, new()
            {
                Added = [child]
            });

            Logger?.LogDebug("Child added successfully. Total children: {ChildCount}", _children.Count);
        }
        else
        {
            Logger?.LogDebug("Child already exists: {ChildType} '{ChildName}'", child.GetType().Name, child.Name);
        }
    }

    public virtual IRuntimeComponent? CreateChild(Type componentType)
    {
        Logger?.LogDebug("CreateChild called for Type: {TypeName}", componentType.Name);

        var component = ComponentFactory?.Create(componentType);

        Logger?.LogDebug("ComponentFactory.Create returned: {Result}", component != null ? component.GetType().Name : "null");

        if (component == null) return null;

        AddChild(component);

        return component;
    }

    public virtual IRuntimeComponent? CreateChild(IComponentTemplate template)
    {
        if (template == null)
        {
            Logger?.LogDebug("CreateChild called on {TypeName} with null template", GetType().Name);
            return null;
        }

        Logger?.LogDebug("CreateChild called on {TypeName} with template: {TemplateTypeName}", GetType().Name, template.GetType().Name);
        Logger?.LogDebug("ComponentFactory is: {Availability}", ComponentFactory != null ? "available" : "null");

        var component = ComponentFactory?.CreateInstance(template);

        Logger?.LogDebug("ComponentFactory.Instantiate returned: {Result}", component != null ? component.GetType().Name : "null");

        if (component == null) return null;

        AddChild(component);

        return component;
    }

    public virtual void RemoveChild(IRuntimeComponent child)
    {
        if (_children.Remove(child))
        {
            Logger?.LogDebug("Removing child: {ChildType} '{ChildName}'", child.GetType().Name, child.Name);

            // Clear parent using concrete type to access internal setter
            if (child is RuntimeComponent runtimeChild)
            {
                runtimeChild.Parent = null;
            }

            ChildCollectionChanged?.Invoke(child, new()
            {
                Removed = [child]
            });

            Logger?.LogDebug("Child removed successfully. Total children: {ChildCount}", _children.Count);
        }
        else
        {
            Logger?.LogDebug("Child not found for removal: {ChildType} '{ChildName}'", child.GetType().Name, child.Name);
        }
    }

    // Tree navigation methods (basic implementations)
    public virtual IEnumerable<T> GetChildren<T>(Func<T, bool>? filter = null, bool depthFirst = false)
        where T : IRuntimeComponent
    {
        if (depthFirst)
        {
            foreach (var child in Children)
            {
                foreach (var grandchild in child.GetChildren(filter, depthFirst))
                {
                    yield return grandchild;
                }

                if (child is T result && (filter == null || filter(result)))
                    yield return result;
            }
        }
        else
        {
            foreach (var child in Children)
            {
                if (child is T result && (filter == null || filter(result)))
                    yield return result;

                foreach (var grandchild in child.GetChildren(filter, depthFirst))
                {
                    yield return grandchild;
                }
            }
        }
    }

    public virtual IEnumerable<T> GetSiblings<T>(Func<T, bool>? filter = null)
        where T : IRuntimeComponent
    {
        if (Parent == null) return [];
        return Parent.GetChildren<T>(filter);
    }

    public virtual T? FindParent<T>(Func<T, bool>? filter = null)
        where T : IRuntimeComponent
    {
        var current = Parent;

        while (current != null)
        {
            if (current is T typed && (filter == null || filter(typed)))
                return typed;

            current = current.Parent;
        }

        return default;
    }

    #region Disposing (IDisposable)

    protected virtual void OnDispose() { }

    public void Dispose()
    {
        Logger?.LogDebug("Disposing...");

        // Dispose children first (leaf to root)
        foreach (var child in Children.OfType<IDisposable>().ToArray()) // ToArray to avoid collection modification during iteration
        {
            if (child is IDisposable disposableChild)
            {
                var childName = child is IRuntimeComponent runtimeChild ? runtimeChild.Name : "Unknown";
                Logger?.LogDebug("Disposing child: {ChildType} '{ChildName}'", child.GetType().Name, childName);
                disposableChild.Dispose();
            }
        }

        // Clear the children collection
        _children.Clear();

        // Call component-specific disposal logic
        OnDispose();

        Logger?.LogDebug("Disposed successfully");
    }

    #endregion
}