namespace Nexus.GameEngine.Events;

/// <summary>
/// Exception thrown when an error occurs during event handling.
/// Contains information about the event, handler, and underlying exception.
/// </summary>
/// <remarks>
/// Initializes a new instance of the EventHandlingException class.
/// </remarks>
/// <param name="message">Error message</param>
/// <param name="gameEvent">Event that was being processed</param>
/// <param name="handlerType">Type of the handler that threw the exception</param>
/// <param name="subscriptionId">Subscription ID that caused the exception</param>
/// <param name="innerException">Underlying exception</param>
public class EventHandlingException(
    string message,
    IGameEvent gameEvent,
    Type handlerType,
    Guid subscriptionId,
    Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>
    /// The event that was being processed when the exception occurred.
    /// </summary>
    public IGameEvent Event { get; } = gameEvent;

    /// <summary>
    /// Type of the event handler that threw the exception.
    /// </summary>
    public Type HandlerType { get; } = handlerType;

    /// <summary>
    /// Unique identifier of the subscription that caused the exception.
    /// </summary>
    public Guid SubscriptionId { get; } = subscriptionId;

    /// <summary>
    /// Timestamp when the exception occurred.
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a formatted error message with event and handler details.
    /// </summary>
    /// <returns>Detailed error message</returns>
    public override string ToString()
    {
        var details = $"Event: {Event.EventType} (ID: {Event.EventId}), " +
                     $"Handler: {HandlerType.Name}, " +
                     $"Subscription: {SubscriptionId}, " +
                     $"Occurred: {OccurredAt:yyyy-MM-dd HH:mm:ss} UTC";

        return $"{GetType().Name}: {Message}\n{details}\n{base.ToString()}";
    }
}
