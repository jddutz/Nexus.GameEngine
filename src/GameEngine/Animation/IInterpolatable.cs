namespace Nexus.GameEngine.Animation;

/// <summary>
/// Defines a custom interpolation strategy for types that support animation.
/// </summary>
/// <typeparam name="T">The type being interpolated.</typeparam>
public interface IInterpolatable<T>
{
    /// <summary>
    /// Interpolates between this value and another value.
    /// </summary>
    /// <param name="other">The target value.</param>
    /// <param name="t">The interpolation parameter (0.0 to 1.0).</param>
    /// <param name="mode">The interpolation mode to use.</param>
    /// <returns>The interpolated value.</returns>
    T Interpolate(T other, float t, InterpolationMode mode);
}
