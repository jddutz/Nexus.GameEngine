namespace Nexus.GameEngine.Physics.Collision;

/// <summary>
/// Represents a component that can participate in collision detection.
/// </summary>
public interface ICollidable
{
    bool IsCollisionEnabled { get; set; }
    CollisionShapeEnum CollisionShapeEnum { get; set; }
    Rectangle<float> CollisionBounds { get; set; }
    float CollisionRadius { get; set; }
    int CollisionLayer { get; set; }
    int CollisionMask { get; set; }
    CollisionMaterial Material { get; set; }
    bool IsTrigger { get; set; }
    bool IsStatic { get; set; }
    CollisionDetectionModeEnum DetectionMode { get; set; }
    IReadOnlyList<CollisionContact> CurrentCollisions { get; }
    bool IsColliding { get; }
    int CollisionCount { get; }
    string CollisionGroup { get; set; }
    event EventHandler<CollisionEventArgs> CollisionEnter;
    event EventHandler<CollisionEventArgs> CollisionStay;
    event EventHandler<CollisionEventArgs> CollisionExit;
    event EventHandler<CollisionPropertyChangedEventArgs> CollisionPropertyChanged;
    bool IsCollidingWith(ICollidable other);
    bool WouldCollideAt(Vector2D<float> position);
    IEnumerable<CollisionContact> GetCollisionsWith(ICollidable other);
    Vector2D<float> GetClosestPoint(Vector2D<float> point);
    float DistanceTo(ICollidable other);
    RaycastHit? Raycast(Vector2D<float> origin, Vector2D<float> direction, float maxDistance = float.MaxValue);
    void UpdateCollisionBounds();
    void SetLayerCollision(int layer, bool enabled);
    bool GetLayerCollision(int layer);
}
