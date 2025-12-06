namespace Nexus.GameEngine.Data;

/// <summary>
/// Defines a one-way value transformation for property bindings.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Converts a source value to the target property's format.
    /// </summary>
    /// <param name="value">The source value to convert.</param>
    /// <returns>The converted value, or null to skip the binding update.</returns>
    /// <remarks>
    /// - Returning null will skip the binding update (target property unchanged)
    /// - Implementations SHOULD handle type mismatches gracefully
    /// - Implementations MUST NOT throw exceptions (caught and logged)
    /// </remarks>
    object? Convert(object? value);
}
