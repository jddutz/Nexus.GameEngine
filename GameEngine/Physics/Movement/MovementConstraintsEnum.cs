namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Defines movement constraints.
/// </summary>
[Flags]
public enum MovementConstraintsEnum
{
    None = 0,
    FreezeX = 1 << 0,
    FreezeY = 1 << 1,
    FreezeRotation = 1 << 2,
    HorizontalOnly = FreezeY,
    VerticalOnly = FreezeX,
    FreezePosition = FreezeX | FreezeY,
    FreezeAll = FreezeX | FreezeY | FreezeRotation
}
