namespace Nexus.GameEngine.Input.Components;

/// <summary>
/// Runtime component that serves as a container for input binding components.
/// Provides context-aware input handling by managing the activation/deactivation
/// of child input binding components. When this mapping is active, all child
/// input bindings are activated. When deactivated, all child bindings are deactivated.
/// </summary>
public partial class InputMap : Component
{
    /// <summary>
    /// Description of when this input mapping should be active.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Priority level for this input mapping when multiple mappings are active.
    /// Higher values have higher priority.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private int _priority = 0;

    /// <summary>
    /// Whether this input mapping should be enabled by default.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private bool _enabledByDefault = true;

    /// <summary>
    /// Activate this input mapping and all child input bindings.
    /// Called when this input context becomes active.
    /// </summary>
    protected override void OnActivate()
    {
        // Child components are automatically activated by the base class
        // Each input binding will handle its own event subscription
    }

    /// <summary>
    /// Deactivate this input mapping and all child input bindings.
    /// Called when this input context becomes inactive.
    /// </summary>
    protected override void OnDeactivate()
    {
        // Child components are automatically deactivated by the base class
        // Each input binding will handle its own event unsubscription
    }

    /// <summary>
    /// Validate the input mapping configuration.
    /// </summary>
    /// <returns>Collection of validation errors</returns>
    protected override IEnumerable<ValidationError> OnValidate()
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ValidationError(
                this,
                "Name should be specified for better debugging and identification",
                ValidationSeverityEnum.Warning
            ));
        }

        return errors;
    }

    /// <summary>
    /// Get all input binding components that are children of this mapping.
    /// </summary>
    /// <returns>Enumerable of input binding components</returns>
    public IEnumerable<IComponent> GetInputBindings()
    {
        return Children.Where(child =>
            child.GetType().IsGenericType &&
            child.GetType().GetGenericTypeDefinition().Name.Contains("Binding"));
    }

    /// <summary>
    /// Get all input binding components of a specific type.
    /// </summary>
    /// <typeparam name="T">Type of input binding to retrieve</typeparam>
    /// <returns>Enumerable of input binding components of the specified type</returns>
    public IEnumerable<T> GetInputBindings<T>() where T : IComponent
    {
        return GetChildren<T>();
    }
}
