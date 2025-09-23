namespace Nexus.GameEngine.Physics.Kinematic;

/// <summary>
/// Specifies different kinematic movement modes.
/// </summary>
public enum KinematicModeEnum
{
    /// <summary>
    /// Constant velocity movement.
    /// </summary>
    ConstantVelocity,

    /// <summary>
    /// Movement with acceleration and deceleration.
    /// </summary>
    Accelerated,

    /// <summary>
    /// Smooth ease-in and ease-out movement.
    /// </summary>
    Smooth,

    /// <summary>
    /// Movement along predefined paths or waypoints.
    /// </summary>
    Waypoint,

    /// <summary>
    /// Custom movement behavior.
    /// </summary>
    Custom
}