using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for movement blocked events.
/// </summary>
public class MovementBlockedEventArgs : EventArgs
{
    public Vector2D<float> AttemptedMovement { get; }
    public MovementBlockedReasonEnum Reason { get; }
    public object? BlockingObject { get; }
    public Vector2D<float>? CollisionPoint { get; }
    public MovementBlockedEventArgs(Vector2D<float> attemptedMovement, MovementBlockedReasonEnum reason, object? blockingObject = null, Vector2D<float>? collisionPoint = null)
    {
        AttemptedMovement = attemptedMovement;
        Reason = reason;
        BlockingObject = blockingObject;
        CollisionPoint = collisionPoint;
    }
}
