namespace Nexus.GameEngine.Components;

/// <summary>
/// Defines behavior of components that can be Loadd and validated.
/// </summary>
public interface IConfigurable : IEntity
{
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

    // Configuration Events
    event EventHandler<ConfigurationEventArgs>? Loading;
    event EventHandler<ConfigurationEventArgs>? Loaded;

    // Validation Events
    event EventHandler<EventArgs>? Validating;
    event EventHandler<EventArgs>? Validated;
    event EventHandler<EventArgs>? ValidationFailed;

    /// <summary>
    /// Load the component using the specified template.
    /// Automatically triggers re-validation of the component.
    /// </summary>
    /// <param name="template">Template used for configuration. Can be null for template-less components.</param>
    void Load(Template template);

    /// <summary>
    /// Validate this component and all its subcomponents.
    /// Stores validation errors internally and returns them.
    /// Subsequent calls return cached results until configuration changes.
    /// </summary>
    /// <returns>Collection of validation errors</returns>
    bool Validate(bool ignoreCached = false);
}