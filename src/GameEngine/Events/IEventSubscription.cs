namespace Nexus.GameEngine.Events;

/// <summary>
/// Represents a subscription to an event type.
/// Used to manage and unsubscribe from events.
/// </summary>
public interface IEventSubscription : IDisposable
{
    /// <summary>
    /// Unique identifier for this subscription.
    /// </summary>
    Guid SubscriptionId { get; }

    /// <summary>
    /// Type of event this subscription handles.
    /// </summary>
    Type EventType { get; }

    /// <summary>
    /// Priority of this subscription for execution order.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Indicates whether this subscription is still active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Timestamp when the subscription was created.
    /// </summary>
    DateTime CreatedAt { get; }
}