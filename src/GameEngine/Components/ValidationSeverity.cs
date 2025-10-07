namespace Nexus.GameEngine.Components;

/// <summary>
/// Severity levels for validation errors.
/// </summary>
public enum ValidationSeverityEnum
{
    /// <summary>
    /// Information only, does not prevent activation.
    /// </summary>
    Info,

    /// <summary>
    /// Warning that should be addressed but doesn't prevent activation.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevents component activation.
    /// </summary>
    Error
}