using System;
using System.Collections.Generic;
using System.Transactions;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Delegate for handling property interpolation.
/// </summary>
/// <typeparam name="T">The type of the property value.</typeparam>
/// <param name="current">The current value of the property.</param>
/// <param name="target">The target value to reach.</param>
/// <param name="deltaTime">The time elapsed since the last update.</param>
/// <param name="isComplete">Output flag indicating if the interpolation is finished.</param>
/// <returns>The new interpolated value.</returns>
public delegate T InterpolationFunction<T>(T current, T target, double deltaTime);

/// <summary>
/// Manages the state for a deferred/interpolated component property.
/// This struct is used by the source generator to back [ComponentProperty] fields.
/// </summary>
/// <typeparam name="T">The type of the property.</typeparam>
public struct ComponentPropertyUpdater<T>
{
    public ComponentPropertyUpdater(T source)
    {
        _target = source;
    }

    private T _target;
    private InterpolationFunction<T>? _interpolator;
    private bool _hasUpdate;
    private static readonly EqualityComparer<T> _comparer = EqualityComparer<T>.Default;

    /// <summary>
    /// Gets the target value that will be applied.
    /// Returns the provided current value if no update is pending.
    /// </summary>
    public readonly T Target => _target;

    /// <summary>
    /// Gets the target value that will be applied.
    /// Returns the provided current value if no update is pending.
    /// </summary>
    public T GetTarget(T current) => _hasUpdate ? _target : current;

    /// <summary>
    /// Indicates whether there is a pending update that needs to be applied.
    /// </summary>
    public readonly bool HasPendingUpdate => _hasUpdate;

    /// <summary>
    /// Schedules an update for the property.
    /// </summary>
    /// <returns>True, if the value can/will be changed, false otherwise</returns>
    public bool Set(ref T value, T target, InterpolationFunction<T>? interpolator = null)
    {
        if (_comparer.Equals(value, target)) return false;

        _target = target;
        _interpolator = interpolator;

        if (_interpolator == null)
        {
            value = _target;
            _hasUpdate = false;
        }
        else
        {
            _hasUpdate = true;
        }

        return true;
    }

    /// <summary>
    /// Applies the pending update to the current value.
    /// </summary>
    /// <param name="current">Reference to the current property value.</param>
    /// <param name="deltaTime">Time elapsed.</param>
    /// <returns>True if the value changed, False otherwise.</returns>
    public bool Apply(ref T current, double deltaTime)
    {
        if (!_hasUpdate) return false;

        if (_comparer.Equals(current, _target))
        {
            _hasUpdate = false;
            return false;
        }

        if (_interpolator == null)
        {
            current = _target;
            _hasUpdate = false;
        }
        else
        {
            // Interpolated update
            current = _interpolator(current, _target, deltaTime);
        }

        return true;
    }
}
