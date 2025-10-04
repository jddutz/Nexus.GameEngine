namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for rotation changed events.
/// </summary>
public class RotationChangedEventArgs(float oldRotation, float newRotation) : EventArgs
{
    public float OldRotation { get; } = oldRotation;
    public float NewRotation { get; } = newRotation;
    public float Delta => NewRotation - OldRotation;
}
