using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Events;

/// <summary>
/// Base interface for all game events in the system.
/// Events are immutable data structures that represent something that has happened in the game.
/// </summary>
public interface IGameEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Type of the event for filtering and categorization.
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Source component that generated this event.
    /// Can be null for system-generated events.
    /// </summary>
    ComponentId? SourceComponentId { get; }

    /// <summary>
    /// Optional metadata associated with the event.
    /// Can contain additional context or properties specific to the event type.
    /// </summary>
    IReadOnlyDictionary<string, object>? Metadata { get; }

    /// <summary>
    /// Priority level of the event for processing order.
    /// Higher values indicate higher priority.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Indicates whether this event should be propagated up the component hierarchy.
    /// </summary>
    bool Bubbles { get; }

    /// <summary>
    /// Indicates whether this event has been handled and should stop propagation.
    /// </summary>
    bool IsHandled { get; }

    /// <summary>
    /// Marks the event as handled, preventing further propagation.
    /// </summary>
    void MarkAsHandled();
}
