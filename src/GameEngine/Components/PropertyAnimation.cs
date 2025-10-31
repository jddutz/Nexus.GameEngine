namespace Nexus.GameEngine.Components;

/// <summary>
/// Manages interpolation and timing for animated property values.
/// </summary>
/// <typeparam name="T">The value type being animated.</typeparam>
public sealed class PropertyAnimation<T> where T : struct
{
    /// <summary>
    /// Gets or sets the animation duration in seconds.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// Gets or sets the interpolation mode.
    /// </summary>
    public InterpolationMode Interpolation { get; set; }

    /// <summary>
    /// Gets a value indicating whether the animation is currently active.
    /// </summary>
    public bool IsAnimating { get; private set; }

    private T _startValue;
    private T _endValue;
    private double _elapsed;

    /// <summary>
    /// Starts a new animation from the current value to a target value.
    /// </summary>
    /// <param name="startValue">The starting value.</param>
    /// <param name="endValue">The target value.</param>
    /// <param name="currentTime">The current time in seconds.</param>
    public void StartAnimation(T startValue, T endValue)
    {
        _startValue = startValue;
        _endValue = endValue;
        _elapsed = 0.0;
        IsAnimating = Duration > 0;
    }

    /// <summary>
    /// Updates the animation and returns the current interpolated value.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    /// <returns>The interpolated value at the current time.</returns>
    public T Update(double deltaTime)
    {
        if (!IsAnimating)
            return _endValue;

        _elapsed += deltaTime;

        if (_elapsed >= Duration)
        {
            IsAnimating = false;
            return _endValue;
        }

        float t = (float)(_elapsed / Duration);

        // Apply easing function based on interpolation mode
        t = ApplyEasing(t, Interpolation);

        // Perform interpolation (this will be implemented by the generator for specific types)
        return Interpolate(_startValue, _endValue, t);
    }

    /// <summary>
    /// Applies an easing function to the interpolation parameter.
    /// </summary>
    private static float ApplyEasing(float t, InterpolationMode mode)
    {
        return mode switch
        {
            InterpolationMode.Linear => t,
            InterpolationMode.LinearEaseIn => t * t,
            InterpolationMode.LinearEaseOut => t * (2 - t),
            InterpolationMode.LinearEaseInOut => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t,
            InterpolationMode.Cubic => t * t * t,
            InterpolationMode.CubicEaseIn => t * t * t,
            InterpolationMode.CubicEaseOut => (--t) * t * t + 1,
            InterpolationMode.CubicEaseInOut => t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1,
            InterpolationMode.Step => 1.0f,
            _ => t
        };
    }

    /// <summary>
    /// Performs type-specific interpolation between two values.
    /// NOTE: The source generator should create optimized type-specific subclasses
    /// to avoid runtime type checking. This is a fallback implementation.
    /// </summary>
    private T Interpolate(T start, T end, float t)
    {
        // Fallback: Use dynamic to attempt operator-based interpolation
        // This works for types with operator+ and operator* overloads (vectors, matrices, etc.)
        try
        {
            dynamic startVal = start;
            dynamic endVal = end;
            return (T)(object)(startVal + (endVal - startVal) * t);
        }
        catch
        {
            // For unsupported types, snap to end value
            return end;
        }
    }
}
