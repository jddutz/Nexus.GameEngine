namespace Nexus.GameEngine.Physics.Kinematic;

/// <summary>
/// Specifies different interpolation methods for smooth movement.
/// </summary>
public enum InterpolationMethodEnum
{
    /// <summary>
    /// Linear interpolation.
    /// </summary>
    Linear,

    /// <summary>
    /// Smooth step interpolation.
    /// </summary>
    SmoothStep,

    /// <summary>
    /// Smoother step interpolation.
    /// </summary>
    SmootherStep,

    /// <summary>
    /// Cubic Bezier interpolation.
    /// </summary>
    CubicBezier,

    /// <summary>
    /// Catmull-Rom spline interpolation.
    /// </summary>
    CatmullRom,

    /// <summary>
    /// Ease-in interpolation.
    /// </summary>
    EaseIn,

    /// <summary>
    /// Ease-out interpolation.
    /// </summary>
    EaseOut,

    /// <summary>
    /// Ease-in-out interpolation.
    /// </summary>
    EaseInOut
}