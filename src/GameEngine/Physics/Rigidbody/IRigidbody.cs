using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics;

/// <summary>
/// Represents a component that participates in physics simulation with forces, mass, and momentum.
/// </summary>
public interface IRigidbody
{
    /// <summary>
    /// Gets or sets whether physics simulation is enabled for this rigidbody.
    /// </summary>
    bool IsPhysicsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the mass of the rigidbody.
    /// </summary>
    float Mass { get; set; }

    /// <summary>
    /// Gets or sets the linear velocity of the rigidbody.
    /// </summary>
    Vector2D<float> Velocity { get; set; }

    /// <summary>
    /// Gets or sets the angular velocity of the rigidbody (radians per second).
    /// </summary>
    float AngularVelocity { get; set; }

    /// <summary>
    /// Gets or sets the linear drag coefficient (air resistance).
    /// </summary>
    float LinearDrag { get; set; }

    /// <summary>
    /// Gets or sets the angular drag coefficient (rotational resistance).
    /// </summary>
    float AngularDrag { get; set; }

    /// <summary>
    /// Gets or sets the gravity scale factor for this rigidbody.
    /// </summary>
    float GravityScale { get; set; }

    /// <summary>
    /// Gets or sets whether this rigidbody is kinematic (not affected by forces).
    /// </summary>
    bool IsKinematic { get; set; }

    /// <summary>
    /// Gets or sets whether this rigidbody should freeze rotation.
    /// </summary>
    bool FreezeRotation { get; set; }

    /// <summary>
    /// Gets or sets the body type of this rigidbody.
    /// </summary>
    RigidbodyTypeEnum BodyType { get; set; }

    /// <summary>
    /// Gets or sets which movement axes are frozen.
    /// </summary>
    RigidbodyConstraintsEnum Constraints { get; set; }

    /// <summary>
    /// Gets or sets the center of mass offset from the transform position.
    /// </summary>
    Vector2D<float> CenterOfMass { get; set; }

    /// <summary>
    /// Gets or sets the moment of inertia for rotational physics.
    /// </summary>
    float Inertia { get; set; }

    /// <summary>
    /// Gets or sets whether the inertia should be calculated automatically.
    /// </summary>
    bool AutoCalculateInertia { get; set; }

    /// <summary>
    /// Gets or sets the sleep threshold for optimization.
    /// </summary>
    float SleepThreshold { get; set; }

    /// <summary>
    /// Gets whether the rigidbody is currently sleeping (at rest).
    /// </summary>
    bool IsSleeping { get; }

    /// <summary>
    /// Gets the current kinetic energy of the rigidbody.
    /// </summary>
    float KineticEnergy { get; }

    /// <summary>
    /// Gets the current momentum of the rigidbody.
    /// </summary>
    Vector2D<float> Momentum { get; }

    /// <summary>
    /// Gets the current angular momentum of the rigidbody.
    /// </summary>
    float AngularMomentum { get; }

    /// <summary>
    /// Gets the world center of mass position.
    /// </summary>
    Vector2D<float> WorldCenterOfMass { get; }

    /// <summary>
    /// Occurs when the rigidbody starts sleeping.
    /// </summary>
    event EventHandler<RigidbodyEventArgs> Sleep;

    /// <summary>
    /// Occurs when the rigidbody wakes up from sleep.
    /// </summary>
    event EventHandler<RigidbodyEventArgs> WakeUp;

    /// <summary>
    /// Occurs when velocity changes significantly.
    /// </summary>
    event EventHandler<VelocityChangedEventArgs> VelocityChanged;

    /// <summary>
    /// Occurs when a force is applied to the rigidbody.
    /// </summary>
    event EventHandler<ForceAppliedEventArgs> ForceApplied;

    /// <summary>
    /// Applies a force to the rigidbody at its center of mass.
    /// </summary>
    /// <param name="force">The force vector to apply.</param>
    /// <param name="mode">How the force should be applied.</param>
    void AddForce(Vector2D<float> force, ForceModeEnum mode = ForceModeEnum.Force);

    /// <summary>
    /// Applies a force to the rigidbody at a specific point.
    /// </summary>
    /// <param name="force">The force vector to apply.</param>
    /// <param name="position">The world position where the force is applied.</param>
    /// <param name="mode">How the force should be applied.</param>
    void AddForceAtPosition(Vector2D<float> force, Vector2D<float> position, ForceModeEnum mode = ForceModeEnum.Force);

    /// <summary>
    /// Applies a torque (rotational force) to the rigidbody.
    /// </summary>
    /// <param name="torque">The torque to apply (positive = counter-clockwise).</param>
    /// <param name="mode">How the torque should be applied.</param>
    void AddTorque(float torque, ForceModeEnum mode = ForceModeEnum.Force);

    /// <summary>
    /// Applies an impulse to the rigidbody at its center of mass.
    /// </summary>
    /// <param name="impulse">The impulse vector to apply.</param>
    void AddImpulse(Vector2D<float> impulse);

    /// <summary>
    /// Applies an impulse to the rigidbody at a specific point.
    /// </summary>
    /// <param name="impulse">The impulse vector to apply.</param>
    /// <param name="position">The world position where the impulse is applied.</param>
    void AddImpulseAtPosition(Vector2D<float> impulse, Vector2D<float> position);

    /// <summary>
    /// Applies an angular impulse to the rigidbody.
    /// </summary>
    /// <param name="impulse">The angular impulse to apply.</param>
    void AddAngularImpulse(float impulse);

    /// <summary>
    /// Sets the velocity of the rigidbody directly.
    /// </summary>
    /// <param name="velocity">The new velocity.</param>
    void SetVelocity(Vector2D<float> velocity);

    /// <summary>
    /// Sets the angular velocity of the rigidbody directly.
    /// </summary>
    /// <param name="angularVelocity">The new angular velocity.</param>
    void SetAngularVelocity(float angularVelocity);

    /// <summary>
    /// Moves the rigidbody to a specific position (for kinematic bodies).
    /// </summary>
    /// <param name="position">The target position.</param>
    void MovePosition(Vector2D<float> position);

    /// <summary>
    /// Rotates the rigidbody to a specific rotation (for kinematic bodies).
    /// </summary>
    /// <param name="rotation">The target rotation in radians.</param>
    void MoveRotation(float rotation);

    /// <summary>
    /// Wakes up the rigidbody if it's sleeping.
    /// </summary>
    void WakeFromSleep();

    /// <summary>
    /// Forces the rigidbody to sleep.
    /// </summary>
    void PutToSleep();

    /// <summary>
    /// Resets all forces and torques acting on the rigidbody.
    /// </summary>
    void ResetForces();

    /// <summary>
    /// Gets the velocity of the rigidbody at a specific world point.
    /// </summary>
    /// <param name="worldPoint">The world point to get velocity for.</param>
    /// <returns>The velocity at the specified point.</returns>
    Vector2D<float> GetPointVelocity(Vector2D<float> worldPoint);

    /// <summary>
    /// Gets the relative velocity of this rigidbody compared to another.
    /// </summary>
    /// <param name="other">The other rigidbody to compare against.</param>
    /// <returns>The relative velocity vector.</returns>
    Vector2D<float> GetRelativeVelocity(IRigidbody other);

    /// <summary>
    /// Calculates the momentum transfer for a collision.
    /// </summary>
    /// <param name="other">The other rigidbody in the collision.</param>
    /// <param name="normal">The collision normal.</param>
    /// <returns>The momentum transfer vector.</returns>
    Vector2D<float> CalculateMomentumTransfer(IRigidbody other, Vector2D<float> normal);
}
