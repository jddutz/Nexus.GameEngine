namespace Nexus.GameEngine.Physics;

/// <summary>
/// Provides data for rigidbody events.
/// </summary>
public class RigidbodyEventArgs(IRigidbody rigidbody) : EventArgs
{
    /// <summary>
    /// Gets the rigidbody that triggered the event.
    /// </summary>
    public IRigidbody Rigidbody { get; } = rigidbody;

    /// <summary>
    /// Gets the timestamp of the rigidbody event.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}