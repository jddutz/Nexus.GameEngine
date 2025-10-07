using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Collision;

/// <summary>
/// Represents the result of a raycast operation.
/// </summary>
public struct RaycastHit(Vector2D<float> point, Vector2D<float> normal, float distance, ICollidable collider, float fraction)
{
    public Vector2D<float> Point { get; } = point;
    public Vector2D<float> Normal { get; } = normal;
    public float Distance { get; } = distance;
    public ICollidable Collider { get; } = collider;
    public float Fraction { get; } = fraction;
}
