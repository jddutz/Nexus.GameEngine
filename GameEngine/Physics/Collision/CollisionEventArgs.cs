using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Collision;

/// <summary>
/// Provides data for collision events.
/// </summary>
public class CollisionEventArgs(ICollidable other, CollisionContact contact,
                         IReadOnlyList<CollisionContact> contacts, Vector2D<float> relativeVelocity) : EventArgs
{
    public ICollidable Other { get; } = other;
    public CollisionContact Contact { get; } = contact;
    public IReadOnlyList<CollisionContact> Contacts { get; } = contacts;
    public Vector2D<float> RelativeVelocity { get; } = relativeVelocity;
    public bool Ignore { get; set; }
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
