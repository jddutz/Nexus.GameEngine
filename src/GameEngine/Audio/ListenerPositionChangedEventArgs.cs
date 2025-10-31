namespace Nexus.GameEngine.Audio;

public class ListenerPositionChangedEventArgs(Vector3D<float> oldPosition, Vector3D<float> newPosition) : EventArgs
{
    public Vector3D<float> OldPosition { get; } = oldPosition;
    public Vector3D<float> NewPosition { get; } = newPosition;
    public Vector3D<float> PositionDelta { get; } = newPosition - oldPosition;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
