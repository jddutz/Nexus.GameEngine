using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Kinematic;

/// <summary>
/// Provides data for kinematic movement events.
/// </summary>
public class KinematicMovementEventArgs(IKinematic kinematic, Vector2D<float> velocity, float angularVelocity, KinematicModeEnum mode) : EventArgs
{
    /// <summary>
    /// Gets the kinematic object that triggered the event.
    /// </summary>
    public IKinematic Kinematic { get; } = kinematic;

    /// <summary>
    /// Gets the current velocity.
    /// </summary>
    public Vector2D<float> Velocity { get; } = velocity;

    /// <summary>
    /// Gets the current angular velocity.
    /// </summary>
    public float AngularVelocity { get; } = angularVelocity;

    /// <summary>
    /// Gets the movement mode.
    /// </summary>
    public KinematicModeEnum Mode { get; } = mode;

    /// <summary>
    /// Gets the timestamp of the movement event.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}