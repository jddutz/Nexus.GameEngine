using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Events;

/// <summary>
/// Base implementation of IGameEvent providing common functionality.
/// Events are immutable by default - all properties are set in the constructor.
/// </summary>
/// <remarks>
/// Initializes a new instance of the GameEvent class.
/// </remarks>
/// <param name="sourceComponentId">Optional source component that generated this event</param>
/// <param name="metadata">Optional metadata associated with the event</param>
/// <param name="priority">Priority level of the event (default: 0)</param>
/// <param name="bubbles">Whether this event should propagate up the component hierarchy (default: true)</param>
public abstract class GameEvent(
    ComponentId? sourceComponentId = null,
    IReadOnlyDictionary<string, object>? metadata = null,
    int priority = 0,
    bool bubbles = true) : IGameEvent
{
    private bool _isHandled = false;

    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public abstract string EventType { get; }
    public ComponentId? SourceComponentId { get; } = sourceComponentId;
    public IReadOnlyDictionary<string, object>? Metadata { get; } = metadata;
    public int Priority { get; } = priority;
    public bool Bubbles { get; } = bubbles;
    public bool IsHandled => _isHandled;

    public void MarkAsHandled()
    {
        _isHandled = true;
    }

    public override string ToString()
    {
        return $"{EventType} (ID: {EventId}, Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC, Priority: {Priority}, Source: {SourceComponentId?.ToString() ?? "System"})";
    }
}