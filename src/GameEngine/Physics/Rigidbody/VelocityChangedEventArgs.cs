using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics;

/// <summary>
/// Provides data for velocity changed events.
/// </summary>
public class VelocityChangedEventArgs(Vector2D<float> oldVelocity, Vector2D<float> newVelocity,
                               float oldAngularVelocity, float newAngularVelocity) : EventArgs
{
    /// <summary>
    /// Gets the previous velocity.
    /// </summary>
    public Vector2D<float> OldVelocity { get; } = oldVelocity;

    /// <summary>
    /// Gets the new velocity.
    /// </summary>
    public Vector2D<float> NewVelocity { get; } = newVelocity;

    /// <summary>
    /// Gets the change in velocity.
    /// </summary>
    public Vector2D<float> VelocityDelta { get; } = newVelocity - oldVelocity;

    /// <summary>
    /// Gets the previous angular velocity.
    /// </summary>
    public float OldAngularVelocity { get; } = oldAngularVelocity;

    /// <summary>
    /// Gets the new angular velocity.
    /// </summary>
    public float NewAngularVelocity { get; } = newAngularVelocity;

    /// <summary>
    /// Gets the change in angular velocity.
    /// </summary>
    public float AngularVelocityDelta { get; } = newAngularVelocity - oldAngularVelocity;

    /// <summary>
    /// Gets the timestamp of the velocity change.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}