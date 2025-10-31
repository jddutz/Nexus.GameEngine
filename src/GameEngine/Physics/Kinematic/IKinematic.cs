namespace Nexus.GameEngine.Physics.Kinematic;

/// <summary>
/// Represents a component that can be moved in a controlled manner without physics simulation.
/// Kinematic movement is ideal for platforms, elevators, doors, and other controlled objects.
/// </summary>
public interface IKinematic
{
    /// <summary>
    /// Gets or sets whether kinematic movement is enabled.
    /// </summary>
    bool IsKinematicEnabled { get; set; }

    /// <summary>
    /// Gets or sets the current kinematic velocity.
    /// </summary>
    Vector2D<float> KinematicVelocity { get; set; }

    /// <summary>
    /// Gets or sets the current angular velocity for rotation.
    /// </summary>
    float KinematicAngularVelocity { get; set; }

    /// <summary>
    /// Gets or sets the movement speed for kinematic operations.
    /// </summary>
    float MovementSpeed { get; set; }

    /// <summary>
    /// Gets or sets the rotation speed for kinematic operations.
    /// </summary>
    float RotationSpeed { get; set; }

    /// <summary>
    /// Gets or sets the movement mode for kinematic operations.
    /// </summary>
    KinematicModeEnum MovementMode { get; set; }

    /// <summary>
    /// Gets or sets the interpolation method for smooth movement.
    /// </summary>
    InterpolationMethodEnum InterpolationMethodEnum { get; set; }

    /// <summary>
    /// Gets or sets whether movement should use smooth interpolation.
    /// </summary>
    bool SmoothMovement { get; set; }

    /// <summary>
    /// Gets or sets the smoothing factor for interpolated movement (0.0 to 1.0).
    /// </summary>
    float SmoothingFactor { get; set; }

    /// <summary>
    /// Gets or sets whether the kinematic object should push other rigidbodies.
    /// </summary>
    bool PushRigidbodies { get; set; }

    /// <summary>
    /// Gets or sets the maximum force applied when pushing other objects.
    /// </summary>
    float MaxPushForce { get; set; }

    /// <summary>
    /// Gets whether the kinematic object is currently moving.
    /// </summary>
    bool IsMoving { get; }

    /// <summary>
    /// Gets whether the kinematic object is currently rotating.
    /// </summary>
    bool IsRotating { get; }

    /// <summary>
    /// Gets the current target position (if moving to a specific position).
    /// </summary>
    Vector2D<float>? TargetPosition { get; }

    /// <summary>
    /// Gets the current target rotation (if rotating to a specific angle).
    /// </summary>
    float? TargetRotation { get; }

    /// <summary>
    /// Gets the distance remaining to the target position.
    /// </summary>
    float DistanceToTarget { get; }

    /// <summary>
    /// Gets the angular distance remaining to the target rotation.
    /// </summary>
    float AngularDistanceToTarget { get; }

    /// <summary>
    /// Occurs when kinematic movement starts.
    /// </summary>
    event EventHandler<KinematicMovementEventArgs> MovementStarted;

    /// <summary>
    /// Occurs when kinematic movement stops.
    /// </summary>
    event EventHandler<KinematicMovementEventArgs> MovementStopped;

    /// <summary>
    /// Occurs when a target position is reached.
    /// </summary>
    event EventHandler<TargetReachedEventArgs> TargetReached;

    /// <summary>
    /// Occurs when the kinematic object pushes another rigidbody.
    /// </summary>
    event EventHandler<KinematicPushEventArgs> RigidbodyPushed;

    /// <summary>
    /// Moves the kinematic object by a specific offset.
    /// </summary>
    /// <param name="offset">The movement offset.</param>
    /// <param name="deltaTime">The time step for the movement.</param>
    void Move(Vector2D<float> offset, float deltaTime);

    /// <summary>
    /// Moves the kinematic object towards a target position.
    /// </summary>
    /// <param name="targetPosition">The target position to move towards.</param>
    /// <param name="speed">The movement speed (optional, uses MovementSpeed if not specified).</param>
    void MoveTo(Vector2D<float> targetPosition, float? speed = null);

    /// <summary>
    /// Moves the kinematic object along a specific direction.
    /// </summary>
    /// <param name="direction">The normalized direction vector.</param>
    /// <param name="speed">The movement speed (optional, uses MovementSpeed if not specified).</param>
    void MoveInDirection(Vector2D<float> direction, float? speed = null);

    /// <summary>
    /// Rotates the kinematic object by a specific angle.
    /// </summary>
    /// <param name="angleRadians">The rotation angle in radians.</param>
    /// <param name="deltaTime">The time step for the rotation.</param>
    void Rotate(float angleRadians, float deltaTime);

    /// <summary>
    /// Rotates the kinematic object towards a target angle.
    /// </summary>
    /// <param name="targetAngle">The target angle in radians.</param>
    /// <param name="speed">The rotation speed (optional, uses RotationSpeed if not specified).</param>
    void RotateTo(float targetAngle, float? speed = null);

    /// <summary>
    /// Rotates the kinematic object to face a specific position.
    /// </summary>
    /// <param name="targetPosition">The position to face.</param>
    /// <param name="speed">The rotation speed (optional, uses RotationSpeed if not specified).</param>
    void LookAt(Vector2D<float> targetPosition, float? speed = null);

    /// <summary>
    /// Stops all kinematic movement immediately.
    /// </summary>
    void Stop();

    /// <summary>
    /// Stops kinematic position movement but continues rotation.
    /// </summary>
    void StopMovement();

    /// <summary>
    /// Stops kinematic rotation but continues position movement.
    /// </summary>
    void StopRotation();

    /// <summary>
    /// Pauses kinematic movement (can be resumed).
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes paused kinematic movement.
    /// </summary>
    void Resume();

    /// <summary>
    /// Sets the kinematic velocity directly.
    /// </summary>
    /// <param name="velocity">The new velocity.</param>
    void SetVelocity(Vector2D<float> velocity);

    /// <summary>
    /// Sets the kinematic angular velocity directly.
    /// </summary>
    /// <param name="angularVelocity">The new angular velocity.</param>
    void SetAngularVelocity(float angularVelocity);

    /// <summary>
    /// Predicts the position after a given time with current velocity.
    /// </summary>
    /// <param name="time">The time to predict ahead.</param>
    /// <returns>The predicted position.</returns>
    Vector2D<float> PredictPosition(float time);

    /// <summary>
    /// Predicts the rotation after a given time with current angular velocity.
    /// </summary>
    /// <param name="time">The time to predict ahead.</param>
    /// <returns>The predicted rotation in radians.</returns>
    float PredictRotation(float time);

    /// <summary>
    /// Checks if the kinematic object can move to a specific position.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>True if movement is possible, false if blocked.</returns>
    bool CanMoveTo(Vector2D<float> position);

    /// <summary>
    /// Gets the estimated time to reach the target position at current speed.
    /// </summary>
    /// <returns>The estimated time in seconds, or null if no target is set.</returns>
    float? GetTimeToTarget();
}