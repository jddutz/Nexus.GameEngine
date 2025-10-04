namespace Nexus.GameEngine.Resources;

/// <summary>
/// Result of resource definition validation
/// </summary>
public readonly struct ResourceValidationResult
{
    /// <summary>
    /// Whether the resource definition is valid
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Validation error messages, if any
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ResourceValidationResult Success() => new(true, Array.Empty<string>());

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    public static ResourceValidationResult Failed(params string[] errors) => new(false, errors);

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    public static ResourceValidationResult Failed(IEnumerable<string> errors) => new(false, errors.ToArray());

    private ResourceValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public override string ToString()
    {
        if (IsValid)
            return "Valid";

        return $"Invalid: {string.Join(", ", Errors)}";
    }
}