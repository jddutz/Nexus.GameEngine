using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Kinematic;

/// <summary>
/// Provides data for kinematic push events.
/// </summary>
public class KinematicPushEventArgs(IRigidbody pushedRigidbody, Vector2D<float> appliedForce,
                             Vector2D<float> contactPoint, Vector2D<float> contactNormal) : EventArgs
{
    /// <summary>
    /// Gets the rigidbody that was pushed.
    /// </summary>
    public IRigidbody PushedRigidbody { get; } = pushedRigidbody;

    /// <summary>
    /// Gets the force that was applied.
    /// </summary>
    public Vector2D<float> AppliedForce { get; } = appliedForce;

    /// <summary>
    /// Gets the contact point where the push occurred.
    /// </summary>
    public Vector2D<float> ContactPoint { get; } = contactPoint;

    /// <summary>
    /// Gets the contact normal.
    /// </summary>
    public Vector2D<float> ContactNormal { get; } = contactNormal;

    /// <summary>
    /// Gets the timestamp of the push event.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}