namespace Nexus.GameEngine.Physics.Collision;

/// <summary>
/// Represents a collision contact point.
/// </summary>
public struct CollisionContact(Vector2D<float> point, Vector2D<float> normal, float penetration,
                       Vector2D<float> relativeVelocity, Vector2D<float> impulse, ICollidable other)
{
    public Vector2D<float> Point { get; } = point;
    public Vector2D<float> Normal { get; } = normal;
    public float Penetration { get; } = penetration;
    public Vector2D<float> RelativeVelocity { get; } = relativeVelocity;
    public Vector2D<float> Impulse { get; } = impulse;
    public ICollidable Other { get; } = other;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
