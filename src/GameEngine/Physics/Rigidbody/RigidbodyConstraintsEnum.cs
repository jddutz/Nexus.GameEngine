namespace Nexus.GameEngine.Physics;

/// <summary>
/// Specifies constraints that can be applied to rigidbody movement.
/// </summary>
[Flags]
public enum RigidbodyConstraintsEnum
{
    /// <summary>
    /// No constraints.
    /// </summary>
    None = 0,

    /// <summary>
    /// Freeze movement on the X axis.
    /// </summary>
    FreezePositionX = 1,

    /// <summary>
    /// Freeze movement on the Y axis.
    /// </summary>
    FreezePositionY = 2,

    /// <summary>
    /// Freeze rotation around the Z axis.
    /// </summary>
    FreezeRotationZ = 4,

    /// <summary>
    /// Freeze all position movement.
    /// </summary>
    FreezePosition = FreezePositionX | FreezePositionY,

    /// <summary>
    /// Freeze all rotation.
    /// </summary>
    FreezeRotation = FreezeRotationZ,

    /// <summary>
    /// Freeze all movement and rotation.
    /// </summary>
    FreezeAll = FreezePosition | FreezeRotation
}