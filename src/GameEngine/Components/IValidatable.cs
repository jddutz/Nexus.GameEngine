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
    bool IsValid();

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
    /// Always performs validation and updates the cache.
    /// Use IsValid() to check cached validation state without re-validating.
    /// </summary>
    /// <returns>True if valid, false if validation errors exist</returns>
    bool Validate();
}