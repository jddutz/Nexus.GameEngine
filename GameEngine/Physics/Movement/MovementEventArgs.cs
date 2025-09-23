using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for movement events.
/// </summary>
public class MovementEventArgs : EventArgs
{
    public IMoveable Moveable { get; }
    public Vector2D<float> Position { get; }
    public Vector2D<float> PreviousPosition { get; }
    public Vector2D<float> Velocity { get; }
    public MovementModeEnum MovementModeEnum { get; }
    public DateTime Timestamp { get; }
    public MovementEventArgs(IMoveable moveable, Vector2D<float> position, Vector2D<float> previousPosition, Vector2D<float> velocity, MovementModeEnum movementModeEnum)
    {
        Moveable = moveable;
        Position = position;
        PreviousPosition = previousPosition;
        Velocity = velocity;
        MovementModeEnum = movementModeEnum;
        Timestamp = DateTime.UtcNow;
    }
}
