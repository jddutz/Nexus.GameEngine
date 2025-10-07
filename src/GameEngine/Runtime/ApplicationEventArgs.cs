namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Event arguments for application lifecycle events.
/// </summary>
public class ApplicationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets optional context information about the event.
    /// </summary>
    public string? Context { get; init; }
}
