namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Specifies reasons why movement was blocked.
/// </summary>
public enum MovementBlockedReasonEnum
{
    Collision,
    Constraints,
    Boundaries,
    Disabled,
    Custom
}
