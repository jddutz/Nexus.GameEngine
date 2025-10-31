namespace Nexus.GameEngine.Audio;

public class ListenerOrientationChangedEventArgs(Vector3D<float> oldForward, Vector3D<float> newForward, Vector3D<float> oldUp, Vector3D<float> newUp) : EventArgs
{
    public Vector3D<float> OldForward { get; } = oldForward;
    public Vector3D<float> NewForward { get; } = newForward;
    public Vector3D<float> OldUp { get; } = oldUp;
    public Vector3D<float> NewUp { get; } = newUp;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
