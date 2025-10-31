namespace Nexus.GameEngine.Physics;

/// <summary>
/// Provides data for force applied events.
/// </summary>
public class ForceAppliedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the force that was applied.
    /// </summary>
    public Vector2D<float> Force { get; }

    /// <summary>
    /// Gets the position where the force was applied (if applicable).
    /// </summary>
    public Vector2D<float>? Position { get; }

    /// <summary>
    /// Gets the force mode that was used.
    /// </summary>
    public ForceModeEnum Mode { get; }

    /// <summary>
    /// Gets whether this was a torque application.
    /// </summary>
    public bool IsTorque { get; }

    /// <summary>
    /// Gets the torque value (if this was a torque application).
    /// </summary>
    public float Torque { get; }

    /// <summary>
    /// Gets the timestamp of the force application.
    /// </summary>
    public DateTime Timestamp { get; }

    public ForceAppliedEventArgs(Vector2D<float> force, Vector2D<float>? position, ForceModeEnum mode)
    {
        Force = force;
        Position = position;
        Mode = mode;
        IsTorque = false;
        Torque = 0f;
        Timestamp = DateTime.UtcNow;
    }

    public ForceAppliedEventArgs(float torque, ForceModeEnum mode)
    {
        Force = Vector2D<float>.Zero;
        Position = null;
        Mode = mode;
        IsTorque = true;
        Torque = torque;
        Timestamp = DateTime.UtcNow;
    }
}