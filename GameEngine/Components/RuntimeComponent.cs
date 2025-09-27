using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

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
            NotifyPropertyChanged();
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
        ClearValidationResults();
        Validate();

        PropertyChanged?.Invoke(this, new(propertyName));
    }

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
            Console.WriteLine($"[RuntimeComponent] Configure called on {GetType().Name} with null template");
            return;
        }

        Console.WriteLine($"[RuntimeComponent] Configure called on {GetType().Name} with template: {componentTemplate.GetType().Name}");

        Name = componentTemplate.Name;
        IsEnabled = componentTemplate.Enabled;

        Console.WriteLine($"[RuntimeComponent] Component name set to: '{Name}', enabled: {IsEnabled}");

        BeforeConfiguration?.Invoke(this, new(componentTemplate));

        // Configure root to leaf
        OnConfigure(componentTemplate);

        if (componentTemplate is Template template)
        {
            Console.WriteLine($"[RuntimeComponent] Template is RuntimeComponent.Template with {template.Subcomponents?.Length ?? 0} subcomponents");

            if (template.Subcomponents != null)
            {
                foreach (var subcomponentTemplate in template.Subcomponents)
                {
                    if (subcomponentTemplate != null)
                    {
                        Console.WriteLine($"[RuntimeComponent] Creating child from subcomponent template: {subcomponentTemplate.GetType().Name} with name '{subcomponentTemplate.Name ?? "null"}'");
                        var child = CreateChild(subcomponentTemplate);
                        Console.WriteLine($"[RuntimeComponent] Child creation result: {(child != null ? $"SUCCESS - {child.GetType().Name}" : "FAILED")}");
                    }
                    else
                    {
                        Console.WriteLine($"[RuntimeComponent] Skipping null subcomponent template");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"[RuntimeComponent] Template is not RuntimeComponent.Template, it's: {componentTemplate.GetType().Name}");
        }

        AfterConfiguration?.Invoke(this, new(componentTemplate));
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

    public bool Validate()
    {
        // Return cached results if available
        if (_validationState != null) return IsValid;

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
        foreach (var error in ValidationErrors)
        {
            Logger?.LogDebug(
                "Validation Error: {ComponentType} {ComponentName} {ErrorMessage}",
                error.RuntimeComponent.GetType().Name,
                error.RuntimeComponent.Name,
                error.Message
            );
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
        if (!Validate()) return;

        Activating?.Invoke(this, EventArgs.Empty);

        // Activate root to leaf
        OnActivate();

        foreach (var child in Children)
        {
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

    protected virtual void OnUpdate(double deltaTime) { }

    public void Update(double deltaTime)
    {
        IsUpdating = true;

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
        if (IsUnloaded) return; // Already inactive

        Deactivating?.Invoke(this, EventArgs.Empty);

        // Set inactive state before deactivating children
        IsActive = false;

        // Deactivate leaf to root
        foreach (var child in Children)
        {
            child.Deactivate();
        }

        OnDeactivate();

        Deactivated?.Invoke(this, EventArgs.Empty);

        IsUnloaded = true;
    }

    #endregion

    #region Disposing (IDisposable)

    protected virtual void OnDispose() { }

    public void Dispose()
    {
        // Dispose children first (leaf to root)
        foreach (var child in Children.OfType<IDisposable>().ToArray()) // ToArray to avoid collection modification during iteration
        {
            if (child is IDisposable disposableChild)
            {
                disposableChild.Dispose();
            }
        }

        // Clear the children collection
        _children.Clear();

        // Call component-specific disposal logic
        OnDispose();
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
            _children.Add(child);

            child.Parent = this;

            ChildCollectionChanged?.Invoke(child, new()
            {
                Added = [child]
            });
        }
    }

    public virtual IRuntimeComponent? CreateChild(IComponentTemplate template)
    {
        if (template == null)
        {
            Console.WriteLine($"[RuntimeComponent] CreateChild called on {GetType().Name} with null template");
            return null;
        }

        Console.WriteLine($"[RuntimeComponent] CreateChild called on {GetType().Name} with template: {template.GetType().Name}");
        Console.WriteLine($"[RuntimeComponent] ComponentFactory is: {(ComponentFactory != null ? "available" : "null")}");

        var component = ComponentFactory?.Instantiate(template);

        Console.WriteLine($"[RuntimeComponent] ComponentFactory.Instantiate returned: {(component != null ? component.GetType().Name : "null")}");

        if (component == null) return null;

        AddChild(component);
        Console.WriteLine($"[RuntimeComponent] Added child {component.GetType().Name} to {GetType().Name}. Total children now: {_children.Count}");

        return component;
    }

    public virtual void RemoveChild(IRuntimeComponent child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;

            ChildCollectionChanged?.Invoke(child, new()
            {
                Removed = [child]
            });
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