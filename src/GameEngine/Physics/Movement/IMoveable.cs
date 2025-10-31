namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Represents a component that can move or be moved through various movement systems.
/// Supports physics-based, kinematic, teleportation, and interpolated movement modes.
/// </summary>
public interface IMoveable
{
    bool CanMove { get; set; }
    MovementModeEnum MovementModeEnum { get; set; }
    Vector2D<float> Position { get; set; }
    Vector2D<float> PreviousPosition { get; set; }
    Vector2D<float> TargetPosition { get; set; }
    Vector2D<float> Velocity { get; set; }
    float MaxSpeed { get; set; }
    float Acceleration { get; set; }
    float Deceleration { get; set; }
    float Rotation { get; set; }
    float AngularVelocity { get; set; }
    float MaxAngularSpeed { get; set; }
    bool IsMoving { get; }
    bool IsRotating { get; }
    bool HasReachedTarget { get; }
    float DistanceToTarget { get; }
    MovementConstraintsEnum Constraints { get; set; }
    Rectangle<float> MovementBounds { get; set; }
    bool UseBoundaries { get; set; }
    InterpolationSettings Interpolation { get; set; }
    MovementPhysics Physics { get; set; }
    float Friction { get; set; }
    float AirResistance { get; set; }
    float GravityScale { get; set; }
    bool SnapToGrid { get; set; }
    Vector2D<float> GridSize { get; set; }
    float TargetTolerance { get; set; }
    IReadOnlyList<Vector2D<float>> MovementPath { get; }
    int CurrentPathIndex { get; }
    bool LoopPath { get; set; }
    event EventHandler<MovementEventArgs> MovementStarted;
    event EventHandler<MovementEventArgs> MovementStopped;
    event EventHandler<MovementEventArgs> TargetReached;
    event EventHandler<PositionChangedEventArgs> PositionChanged;
    event EventHandler<RotationChangedEventArgs> RotationChanged;
    event EventHandler<MovementBlockedEventArgs> MovementBlocked;
    event EventHandler<BoundaryReachedEventArgs> BoundaryReached;
    event EventHandler<WaypointReachedEventArgs> WaypointReached;
    void Move(Vector2D<float> offset);
    void MoveTo(Vector2D<float> position, MovementModeEnum? mode = null);
    void Teleport(Vector2D<float> position);
    void ApplyForce(Vector2D<float> force);
    void ApplyImpulse(Vector2D<float> impulse);
    void SetVelocity(Vector2D<float> velocity);
    void Rotate(float angle);
    void LookAt(Vector2D<float> direction);
    void LookAt(Vector2D<float> position, bool immediate);
    void Stop();
    void StopMovement();
    void StopRotation();
    void SetPath(IEnumerable<Vector2D<float>> path, bool loop = false);
    void AddWaypoint(Vector2D<float> waypoint);
    void ClearPath();
    void NextWaypoint();
    void PreviousWaypoint();
    bool CanMoveTo(Vector2D<float> position);
    Vector2D<float> GetClosestValidPosition(Vector2D<float> targetPosition);
    Vector2D<float> PredictPosition(float time);
    Vector2D<float> GetMovementDirection();
    float GetSpeed();
    void UpdateMovement(float deltaTime);
    void SetConstraints(MovementConstraintsEnum constraints);
    void SetBoundaries(Rectangle<float> bounds);
    void SnapToGridPosition();
    Vector2D<float> GetGridAlignedPosition(Vector2D<float> position);
}
