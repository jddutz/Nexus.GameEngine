namespace Nexus.GameEngine.Animation;

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
    /// </summary>
    Step
}
