using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for movement events.
/// </summary>
public class MovementEventArgs(IMoveable moveable, Vector2D<float> position, Vector2D<float> previousPosition, Vector2D<float> velocity, MovementModeEnum movementModeEnum) : EventArgs
{
    public IMoveable Moveable { get; } = moveable;
    public Vector2D<float> Position { get; } = position;
    public Vector2D<float> PreviousPosition { get; } = previousPosition;
    public Vector2D<float> Velocity { get; } = velocity;
    public MovementModeEnum MovementModeEnum { get; } = movementModeEnum;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
