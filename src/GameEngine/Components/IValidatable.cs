namespace Nexus.GameEngine.Components;

/// <summary>
/// Base interface for all components in the system.
/// Defines the contract for component identity, hierarchy, lifecycle, and event management.
/// </summary>
public interface IValidatable
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

    // Validation Events
    event EventHandler<EventArgs>? Validating;
    event EventHandler<EventArgs>? Validated;
    event EventHandler<EventArgs>? ValidationFailed;

    /// <summary>
    /// Validate this component and all its subcomponents.
    /// Stores validation errors internally and returns them.
    /// Subsequent calls return cached results until configuration changes.
    /// </summary>
    /// <returns>Collection of validation errors</returns>
    bool Validate(bool ignoreCached = false);
}