namespace Nexus.GameEngine.Physics;

/// <summary>
/// Specifies different types of rigidbodies.
/// </summary>
public enum RigidbodyTypeEnum
{
    /// <summary>
    /// Dynamic body affected by forces and gravity.
    /// </summary>
    Dynamic,

    /// <summary>
    /// Kinematic body not affected by forces but can be moved manually.
    /// </summary>
    Kinematic,

    /// <summary>
    /// Static body that never moves.
    /// </summary>
    Static
}