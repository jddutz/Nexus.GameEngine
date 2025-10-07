using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Kinematic;

/// <summary>
/// Provides data for target reached events.
/// </summary>
public class TargetReachedEventArgs(Vector2D<float>? targetPosition, float? targetRotation,
                             Vector2D<float> finalPosition, float finalRotation, TimeSpan travelTime) : EventArgs
{
    /// <summary>
    /// Gets the target position that was reached.
    /// </summary>
    public Vector2D<float>? TargetPosition { get; } = targetPosition;

    /// <summary>
    /// Gets the target rotation that was reached.
    /// </summary>
    public float? TargetRotation { get; } = targetRotation;

    /// <summary>
    /// Gets the final position.
    /// </summary>
    public Vector2D<float> FinalPosition { get; } = finalPosition;

    /// <summary>
    /// Gets the final rotation.
    /// </summary>
    public float FinalRotation { get; } = finalRotation;

    /// <summary>
    /// Gets the time taken to reach the target.
    /// </summary>
    public TimeSpan TravelTime { get; } = travelTime;

    /// <summary>
    /// Gets the timestamp of the target reached event.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}