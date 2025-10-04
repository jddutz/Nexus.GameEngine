using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Components;


/// <summary>
/// Base class for all components in the system.
/// Provides component tree management, lifecycle methods, and configuration.
/// </summary>
public class RuntimeComponent : IRuntimeComponent, INotifyPropertyChanged
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


    /// <summary>
    /// Factory used to create new components.
    /// </summary>
    private IComponentFactory? _componentFactory;
    public IComponentFactory? ComponentFactory
    {
        get => _componentFactory;
        set
        {
            if (_componentFactory == value) return;

            _componentFactory = value;
            // Infrastructure property - don't trigger validation but do notify property change
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Logger, for logging of course
    /// </summary>
    private ILogger? _logger;
    public ILogger? Logger
    {
        get => _logger;
        set
        {
            if (_logger == value) return;

            _logger = value;
            // Infrastructure property - don't trigger validation
        }
    }

    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    private ComponentId _id = ComponentId.None;
    public ComponentId Id
    {
        get => _id;
        set
        {
            if (_id == value) return;

            _id = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Human-readable name for this component instance.
    /// </summary>
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;

            _name = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Whether this component is currently enabled and should participate in updates.
    /// </summary>
    private bool _enabled = true;
    public bool IsEnabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;

            _enabled = value;
            NotifyPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Call this method when a property changes.
    /// </summary>
    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new(propertyName));
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

        Logger?.LogTrace("{ComponentType} '{Name}' queued deferred update. Total pending: {PendingCount}",
            GetType().Name, Name, _deferredUpdates.Count);
    }

    /// <summary>
    /// Applies all queued deferred updates.
    /// Called by the renderer before rendering each component.
    /// </summary>
    public void ApplyUpdates()
    {
        if (_deferredUpdates.Count == 0) return;

        Logger?.LogTrace("{ComponentType} '{Name}' applying {UpdateCount} deferred updates",
            GetType().Name, Name, _deferredUpdates.Count);

        foreach (var update in _deferredUpdates)
        {
            try
            {
                update();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "{ComponentType} '{Name}' error applying deferred update",
                    GetType().Name, Name);
            }
        }

        _deferredUpdates.Clear();

        // Trigger validation after all updates have been applied
        Validate();

        Logger?.LogTrace("{ComponentType} '{Name}' finished applying deferred updates",
            GetType().Name, Name);
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
    /// Base implementation calls OnConfigure() for component-specific configuration.
    /// </summary>
    /// <summary>
    /// Configure the component using the specified template.
    /// Satisfies the IRuntimeComponent<TTemplate> interface.
    /// </summary>s
    /// <returns>This component instance for fluent chaining.</returns>
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

        Logger?.LogDebug("{ComponentType} '{Name}' configuration completed", GetType().Name, Name);
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
        PropertyChanged?.Invoke(this, new(nameof(IsValid)));
        PropertyChanged?.Invoke(this, new(nameof(ValidationErrors)));
    }

    protected virtual IEnumerable<ValidationError> OnValidate() => [];

    public bool Validate(bool ignoreCached = false)
    {
        // Return cached results if available
        if (!ignoreCached && _validationState != null)
        {
            Logger?.LogDebug("{ComponentType} '{Name}' using cached validation result: {IsValid}", GetType().Name, Name, IsValid);

            // Always log validation errors when using cached results, especially for failed validation
            if (!IsValid && ValidationErrors.Any())
            {
                Logger?.LogDebug("{ComponentType} '{Name}' cached validation errors:", GetType().Name, Name);
                foreach (var error in ValidationErrors)
                {
                    Logger?.LogDebug("  - {ErrorMessage} (Severity: {Severity})", error.Message, error.Severity);
                }
            }

            return IsValid;
        }

        Logger?.LogDebug("{ComponentType} '{Name}' validating...", GetType().Name, Name);

        Validating?.Invoke(this, EventArgs.Empty);

        // Ignore child validation
        // children will simply not be activated if invalid
        _validationErrors.Clear();
        _validationErrors.AddRange(OnValidate());
        _validationState = _validationErrors.Count == 0; // true when no errors (valid), false when errors exist (invalid)
        if (_validationState == false)
        {
            Logger?.LogDebug("{ComponentType} '{Name}' validation failed with {ErrorCount} errors", GetType().Name, Name, _validationErrors.Count);
            ValidationFailed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Logger?.LogDebug("{ComponentType} '{Name}' validation passed", GetType().Name, Name);
        }

        // Log validation errors for debugging
        if (ValidationErrors.Any())
        {
            Logger?.LogDebug("{ComponentType} '{Name}' validation errors:", GetType().Name, Name);
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
        PropertyChanged?.Invoke(this, new(nameof(IsValid)));

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
            Logger?.LogDebug("{ComponentType} '{Name}' failed validation, cannot activate", GetType().Name, Name);
            return;
        }

        Logger?.LogDebug("{ComponentType} '{Name}' activating...", GetType().Name, Name);

        Activating?.Invoke(this, EventArgs.Empty);

        // Activate root to leaf
        OnActivate();

        foreach (var child in Children)
        {
            Logger?.LogDebug("{ComponentType} '{Name}' activating child: {ChildType} '{ChildName}'", GetType().Name, Name, child.GetType().Name, child.Name);
            child.Activate();
        }

        // Set active state after successful activation
        IsActive = true;

        Logger?.LogDebug("{ComponentType} '{Name}' activated successfully. IsActive: {IsActive}", GetType().Name, Name, IsActive);

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

    protected virtual void OnUpdate(double deltaTime) { }

    public void Update(double deltaTime)
    {
        if (!IsActive)
        {
            Logger?.LogTrace("{ComponentType} '{Name}' skipping update - not active", GetType().Name, Name);
            return;
        }

        IsUpdating = true;

        Logger?.LogTrace("{ComponentType} '{Name}' updating with deltaTime: {DeltaTime:F4}s", GetType().Name, Name, deltaTime);

        Updating?.Invoke(this, EventArgs.Empty);

        // Update root to leaf
        OnUpdate(deltaTime);

        foreach (var child in Children)
        {
            child.Update(deltaTime);
        }

        Updated?.Invoke(this, EventArgs.Empty);
        IsUpdating = false;
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
            Logger?.LogDebug("{ComponentType} '{Name}' already deactivated", GetType().Name, Name);
            return; // Already inactive
        }

        Logger?.LogDebug("{ComponentType} '{Name}' deactivating...", GetType().Name, Name);

        Deactivating?.Invoke(this, EventArgs.Empty);

        // Set inactive state before deactivating children
        IsActive = false;

        // Deactivate leaf to root
        foreach (var child in Children)
        {
            Logger?.LogDebug("{ComponentType} '{Name}' deactivating child: {ChildType} '{ChildName}'", GetType().Name, Name, child.GetType().Name, child.Name);
            child.Deactivate();
        }

        OnDeactivate();

        Deactivated?.Invoke(this, EventArgs.Empty);

        IsUnloaded = true;

        Logger?.LogDebug("{ComponentType} '{Name}' deactivated successfully. IsUnloaded: {IsUnloaded}", GetType().Name, Name, IsUnloaded);
    }

    #endregion

    #region Disposing (IDisposable)

    protected virtual void OnDispose() { }

    public void Dispose()
    {
        Logger?.LogDebug("{ComponentType} '{Name}' disposing...", GetType().Name, Name);

        // Dispose children first (leaf to root)
        foreach (var child in Children.OfType<IDisposable>().ToArray()) // ToArray to avoid collection modification during iteration
        {
            if (child is IDisposable disposableChild)
            {
                var childName = child is IRuntimeComponent runtimeChild ? runtimeChild.Name : "Unknown";
                Logger?.LogDebug("{ComponentType} '{Name}' disposing child: {ChildType} '{ChildName}'", GetType().Name, Name, child.GetType().Name, childName);
                disposableChild.Dispose();
            }
        }

        // Clear the children collection
        _children.Clear();

        // Call component-specific disposal logic
        OnDispose();

        Logger?.LogDebug("{ComponentType} '{Name}' disposed successfully", GetType().Name, Name);
    }

    #endregion

    // Tree Management Events
    public event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged;

    private readonly List<IRuntimeComponent> _children = [];

    /// <summary>
    /// Parent component in the component tree.
    /// </summary>
    public virtual IRuntimeComponent? Parent { get; set; }

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

            Logger?.LogDebug("{ComponentType} '{Name}' adding child: {ChildType} '{ChildName}'", GetType().Name, Name, child.GetType().Name, child.Name);

            _children.Add(child);

            child.Parent = this;

            ChildCollectionChanged?.Invoke(child, new()
            {
                Added = [child]
            });

            Logger?.LogDebug("{ComponentType} '{Name}' child added successfully. Total children: {ChildCount}", GetType().Name, Name, _children.Count);
        }
        else
        {
            Logger?.LogDebug("{ComponentType} '{Name}' child already exists: {ChildType} '{ChildName}'", GetType().Name, Name, child.GetType().Name, child.Name);
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
            Logger?.LogDebug("{ComponentType} '{Name}' removing child: {ChildType} '{ChildName}'", GetType().Name, Name, child.GetType().Name, child.Name);

            child.Parent = null;

            ChildCollectionChanged?.Invoke(child, new()
            {
                Removed = [child]
            });

            Logger?.LogDebug("{ComponentType} '{Name}' child removed successfully. Total children: {ChildCount}", GetType().Name, Name, _children.Count);
        }
        else
        {
            Logger?.LogDebug("{ComponentType} '{Name}' child not found for removal: {ChildType} '{ChildName}'", GetType().Name, Name, child.GetType().Name, child.Name);
        }
    }

    // Tree navigation methods (basic implementations)
    public virtual IEnumerable<T> GetChildren<T>(Func<T, bool>? filter = null)
        where T : IRuntimeComponent
    {
        var query = Children.OfType<T>();
        if (filter != null)
            query = query.Where(c => filter(c));
        return query;
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
}