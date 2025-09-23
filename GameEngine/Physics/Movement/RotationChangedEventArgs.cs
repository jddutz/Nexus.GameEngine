namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for rotation changed events.
/// </summary>
public class RotationChangedEventArgs : EventArgs
{
    public float OldRotation { get; }
    public float NewRotation { get; }
    public float Delta => NewRotation - OldRotation;
    public RotationChangedEventArgs(float oldRotation, float newRotation)
    {
        OldRotation = oldRotation;
        NewRotation = newRotation;
    }
}
