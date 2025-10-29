namespace Nexus.GameEngine.Components;

/// <summary>
/// Defines the interpolation strategy for animating property values.
/// </summary>
public enum InterpolationMode
{
    /// <summary>
    /// Standard linear interpolation (Lerp).
    /// </summary>
    Linear,

    /// <summary>
    /// Linear interpolation with ease-in acceleration.
    /// </summary>
    LinearEaseIn,

    /// <summary>
    /// Linear interpolation with ease-out deceleration.
    /// </summary>
    LinearEaseOut,

    /// <summary>
    /// Linear interpolation with ease-in-out (S-curve).
    /// </summary>
    LinearEaseInOut,

    /// <summary>
    /// Cubic spline interpolation.
    /// </summary>
    Cubic,

    /// <summary>
    /// Cubic interpolation with ease-in.
    /// </summary>
    CubicEaseIn,

    /// <summary>
    /// Cubic interpolation with ease-out.
    /// </summary>
    CubicEaseOut,

    /// <summary>
    /// Cubic interpolation with ease-in-out.
    /// </summary>
    CubicEaseInOut,

    /// <summary>
    /// Spherical linear interpolation (for quaternions and unit vectors).
    /// </summary>
    Slerp,

    /// <summary>
    /// No interpolation, snap to target immediately.
    /// Default mode. Works with any type (numeric, bool, string, enum, etc.).
    /// </summary>
    Step,

    /// <summary>
    /// Hold current value, then switch to target value at end of duration.
    /// Useful for delayed state changes with non-interpolatable types (bool, string, enum, etc.).
    /// Example: Delay showing a UI element, or switching a state after a timer.
    /// </summary>
    Hold
}
