namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents a validation error found during component tree validation.
/// </summary>
public class ValidationError(IRuntimeComponent component, string message, ValidationSeverityEnum severity = ValidationSeverityEnum.Error)
{
    /// <summary>
    /// The component that failed validation.
    /// </summary>
    public IRuntimeComponent RuntimeComponent { get; set; } = component;

    /// <summary>
    /// A descriptive message about the validation error.
    /// </summary>
    public string Message { get; set; } = message;

    /// <summary>
    /// The severity of the validation error.
    /// </summary>
    public ValidationSeverityEnum Severity { get; set; } = severity;

    public override string ToString()
    {
        var componentName = RuntimeComponent.Name ?? RuntimeComponent.GetType().Name;
        return $"[{Severity}] {componentName}: {Message}";
    }
}
