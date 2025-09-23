namespace Nexus.GameEngine.Physics;

/// <summary>
/// Specifies how forces are applied to rigidbodies.
/// </summary>
public enum ForceModeEnum
{
    /// <summary>
    /// Continuous force that accounts for mass and time.
    /// </summary>
    Force,

    /// <summary>
    /// Continuous acceleration that ignores mass.
    /// </summary>
    Acceleration,

    /// <summary>
    /// Instant force impulse that accounts for mass.
    /// </summary>
    Impulse,

    /// <summary>
    /// Instant velocity change that ignores mass.
    /// </summary>
    VelocityChange
}