using Nexus.GameEngine.Animation;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Manages interpolation and timing for animated array property values.
/// Interpolates each element individually when the element type implements IInterpolatable.
/// </summary>
/// <typeparam name="T">The element type of the array being animated.</typeparam>
public sealed class ArrayPropertyAnimation<T> where T : struct
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

    private T[] _startValue = [];
    private T[] _endValue = [];
    private T[] _currentValue = [];
    private double _startTime;
    private double _elapsed;

    /// <summary>
    /// Starts a new animation from the current array to a target array.
    /// </summary>
    /// <param name="startValue">The starting array.</param>
    /// <param name="endValue">The target array.</param>
    /// <param name="currentTime">The current time in seconds.</param>
    /// <exception cref="ArgumentNullException">Thrown when startValue or endValue is null.</exception>
    /// <exception cref="ArgumentException">Thrown when array lengths don't match.</exception>
    public void StartAnimation(T[] startValue, T[] endValue, double currentTime)
    {
        if (startValue == null)
            throw new ArgumentNullException(nameof(startValue), "Start array cannot be null for animation");
        
        if (endValue == null)
            throw new ArgumentNullException(nameof(endValue), "End array cannot be null for animation");
        
        if (startValue.Length != endValue.Length)
            throw new ArgumentException(
                $"Array length mismatch: cannot animate from array of length {startValue.Length} to array of length {endValue.Length}. " +
                "Array animations require constant element count.",
                nameof(endValue));

        _startValue = startValue;
        _endValue = endValue;
        _currentValue = new T[startValue.Length];
        _startTime = currentTime;
        _elapsed = 0.0;
        IsAnimating = Duration > 0;
    }

    /// <summary>
    /// Updates the animation and returns the current interpolated array.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    /// <returns>The interpolated array at the current time.</returns>
    public T[] Update(double deltaTime)
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

        // Interpolate each element
        for (int i = 0; i < _startValue.Length; i++)
        {
            _currentValue[i] = InterpolateElement(_startValue[i], _endValue[i], t);
        }

        return _currentValue;
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
    /// Performs type-specific interpolation between two element values.
    /// NOTE: The source generator should inline optimized interpolation code
    /// to avoid runtime type checking. This is a fallback implementation.
    /// </summary>
    private T InterpolateElement(T start, T end, float t)
    {
        // Check if type implements IInterpolatable<T> (for custom user types)
        if (start is IInterpolatable<T> interpolatable)
        {
            return interpolatable.Interpolate(end, t, Interpolation);
        }

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
            // For unsupported types, snap to end value (Step interpolation)
            return end;
        }
    }
}
