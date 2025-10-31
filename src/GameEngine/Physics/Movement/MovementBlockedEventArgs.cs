namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for movement blocked events.
/// </summary>
public class MovementBlockedEventArgs(Vector2D<float> attemptedMovement, MovementBlockedReasonEnum reason, object? blockingObject = null, Vector2D<float>? collisionPoint = null) : EventArgs
{
    public Vector2D<float> AttemptedMovement { get; } = attemptedMovement;
    public MovementBlockedReasonEnum Reason { get; } = reason;
    public object? BlockingObject { get; } = blockingObject;
    public Vector2D<float>? CollisionPoint { get; } = collisionPoint;
}
