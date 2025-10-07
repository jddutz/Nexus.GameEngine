namespace Nexus.GameEngine.Events;

/// <summary>
/// Central event bus for publishing and subscribing to game events.
/// Provides thread-safe event handling with support for synchronous and asynchronous handlers.
/// </summary>
public interface IEventBus : IDisposable
{
    /// <summary>
    /// Publishes an event to all registered handlers synchronously.
    /// </summary>
    /// <typeparam name="T">Type of event to publish</typeparam>
    /// <param name="eventArgs">The event to publish</param>
    void Publish<T>(object sender, T eventArgs) where T : IGameEvent;

    /// <summary>
    /// Publishes an event to all registered handlers asynchronously.
    /// </summary>
    /// <typeparam name="T">Type of event to publish</typeparam>
    /// <param name="eventArgs">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync<T>(object sender, T eventArgs, CancellationToken cancellationToken = default) where T : IGameEvent;

    /// <summary>
    /// Subscribes to events of a specific type with a synchronous handler.
    /// </summary>
    /// <typeparam name="T">Type of event to subscribe to</typeparam>
    /// <param name="handler">Handler method to call when event is published</param>
    /// <param name="priority">Priority for handler execution order (higher values execute first)</param>
    /// <returns>Subscription token that can be used to unsubscribe</returns>
    IEventSubscription Subscribe<T>(EventHandler<T> handler, int priority = 0) where T : IGameEvent;

    /// <summary>
    /// Subscribes to events of a specific type with an asynchronous handler.
    /// </summary>
    /// <typeparam name="T">Type of event to subscribe to</typeparam>
    /// <param name="handler">Async handler method to call when event is published</param>
    /// <param name="priority">Priority for handler execution order (higher values execute first)</param>
    /// <returns>Subscription token that can be used to unsubscribe</returns>
    IEventSubscription SubscribeAsync<T>(AsyncEventHandler<T> handler, int priority = 0) where T : IGameEvent;

    /// <summary>
    /// Unsubscribes a previously registered handler.
    /// </summary>
    /// <param name="subscription">Subscription token returned from Subscribe methods</param>
    void Unsubscribe(IEventSubscription subscription);

    /// <summary>
    /// Unsubscribes all handlers for a specific event type.
    /// </summary>
    /// <typeparam name="T">Type of event to unsubscribe from</typeparam>
    void UnsubscribeAll<T>() where T : IGameEvent;

    /// <summary>
    /// Unsubscribes all handlers from all event types.
    /// </summary>
    void UnsubscribeAll();

    /// <summary>
    /// Gets the number of active subscriptions for a specific event type.
    /// </summary>
    /// <typeparam name="T">Type of event to check</typeparam>
    /// <returns>Number of active subscriptions</returns>
    int GetSubscriptionCount<T>() where T : IGameEvent;

    /// <summary>
    /// Gets the total number of active subscriptions across all event types.
    /// </summary>
    /// <returns>Total number of active subscriptions</returns>
    int GetTotalSubscriptionCount();

    /// <summary>
    /// Event fired when an exception occurs during event handling.
    /// </summary>
    event Action<EventHandlingException>? EventHandlingError;
}