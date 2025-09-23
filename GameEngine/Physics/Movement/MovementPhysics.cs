namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Defines movement physics settings.
/// </summary>
public struct MovementPhysics
{
    public float Mass { get; set; }
    public float Drag { get; set; }
    public float AngularDrag { get; set; }
    public bool UseGravity { get; set; }
    public bool CanPush { get; set; }
    public bool CanBePushed { get; set; }
    public static MovementPhysics Default => new MovementPhysics
    {
        Mass = 1.0f,
        Drag = 0.1f,
        AngularDrag = 0.1f,
        UseGravity = true,
        CanPush = true,
        CanBePushed = true
    };
}
