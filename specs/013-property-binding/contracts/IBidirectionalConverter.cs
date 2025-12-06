namespace Nexus.GameEngine.Data;

/// <summary>
/// Defines a two-way value transformation for property bindings with TwoWay mode.
/// </summary>
public interface IBidirectionalConverter : IValueConverter
{
    /// <summary>
    /// Converts a target value back to the source property's format.
    /// </summary>
    /// <param name="value">The target value to convert back.</param>
    /// <returns>The converted value, or null to skip the binding update.</returns>
    /// <remarks>
    /// - MUST be the mathematical/logical inverse of Convert()
    /// - Example: If Convert(0.5) = 50, then ConvertBack(50) = 0.5
    /// - Returning null will skip the binding update (source property unchanged)
    /// - Implementations MUST NOT throw exceptions (caught and logged)
    /// </remarks>
    object? ConvertBack(object? value);
}
