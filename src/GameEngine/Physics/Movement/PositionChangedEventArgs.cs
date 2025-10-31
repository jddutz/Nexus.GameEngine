namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for position changed events.
/// </summary>
public class PositionChangedEventArgs(Vector2D<float> oldPosition, Vector2D<float> newPosition) : EventArgs
{
    public Vector2D<float> OldPosition { get; } = oldPosition;
    public Vector2D<float> NewPosition { get; } = newPosition;
    public Vector2D<float> Delta => NewPosition - OldPosition;
    public float DistanceSquared => Delta.LengthSquared;
}
