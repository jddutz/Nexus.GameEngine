namespace Nexus.GameEngine.Components;

public abstract partial class Configurable : Entity, IConfigurable
{
    // Configuration Events
    public event EventHandler<ConfigurationEventArgs>? Loading;
    public event EventHandler<ConfigurationEventArgs>? Loaded;

    // Validation Events
    public event EventHandler<EventArgs>? Validating;
    public event EventHandler<EventArgs>? Validated;
    public event EventHandler<EventArgs>? ValidationFailed;

    public bool IsLoaded { get; set; } = false;

    /// <summary>
    /// Override in derived classes to implement component-specific configuration.
    /// Parent is Loadd before calling this method.
    /// </summary>
    /// <param name="template">Template used for configuration, or null if created without template</param>
    protected virtual void OnLoad(Template? template) { }

    protected virtual void Configure(Template template)
    {
        if (template.Name != null) SetCurrentName(template.Name);
    }

    /// <summary>
    /// Load the component using the specified template (legacy Configurable.Template).
    /// Base implementation calls OnLoad() for component-specific configuration,
    /// then applies all deferred property updates immediately via ApplyUpdates(0).
    /// </summary>
    /// <param name="template">The template to Load from.</param>
    public void Load(Template template)
    {
        Loading?.Invoke(this, new(template));

        if (template.Name != null) SetCurrentName(template.Name);
        Configure(template);

        OnLoad(template);

        IsLoaded = true;
        Loaded?.Invoke(this, new(template));
    }

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

    /// <summary>
    /// Clears the cached validation results and fires the validation cache invalidated event.
    /// This method should be called whenever something changes that could affect validation.
    /// Does not trigger re-validation - call Validate() separately if needed.
    /// </summary>
    protected void ClearValidationResults()
    {
        _validationState = null;
        _validationErrors = [];
    }

    protected virtual IEnumerable<ValidationError> OnValidate() => [];

    public bool Validate(bool ignoreCached = false)
    {
        // Return cached results if available
        if (!ignoreCached && _validationState != null) return IsValid;

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

        if (_validationState == true)
        {
            Validated?.Invoke(this, EventArgs.Empty);
            return true;
        }
        else
        {
            return false;
        }
    }
}