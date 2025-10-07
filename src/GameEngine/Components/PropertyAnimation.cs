using Nexus.GameEngine.Animation;

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
    private double _startTime;
    private double _elapsed;

    /// <summary>
    /// Starts a new animation from the current value to a target value.
    /// </summary>
    /// <param name="startValue">The starting value.</param>
    /// <param name="endValue">The target value.</param>
    /// <param name="currentTime">The current time in seconds.</param>
    public void StartAnimation(T startValue, T endValue, double currentTime)
    {
        _startValue = startValue;
        _endValue = endValue;
        _startTime = currentTime;
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
    /// This method will be specialized by the source generator for each supported type.
    /// </summary>
    private T Interpolate(T start, T end, float t)
    {
        // The source generator will provide type-specific implementations
        // For now, we'll use a generic approach that works for basic types
        if (typeof(T) == typeof(float))
        {
            float startF = (float)(object)start;
            float endF = (float)(object)end;
            return (T)(object)(startF + (endF - startF) * t);
        }
        else if (typeof(T) == typeof(double))
        {
            double startD = (double)(object)start;
            double endD = (double)(object)end;
            return (T)(object)(startD + (endD - startD) * t);
        }
        else if (typeof(T) == typeof(int))
        {
            int startI = (int)(object)start;
            int endI = (int)(object)end;
            return (T)(object)(int)(startI + (endI - startI) * t);
        }
        else if (start is IInterpolatable<T> interpolatable)
        {
            return interpolatable.Interpolate(end, t, Interpolation);
        }

        // For unsupported types, snap to end value
        return end;
    }
}
