using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for position changed events.
/// </summary>
public class PositionChangedEventArgs : EventArgs
{
    public Vector2D<float> OldPosition { get; }
    public Vector2D<float> NewPosition { get; }
    public Vector2D<float> Delta => NewPosition - OldPosition;
    public float DistanceSquared => Delta.LengthSquared;
    public PositionChangedEventArgs(Vector2D<float> oldPosition, Vector2D<float> newPosition)
    {
        OldPosition = oldPosition;
        NewPosition = newPosition;
    }
}
