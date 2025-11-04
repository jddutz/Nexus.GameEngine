using System.Collections.Concurrent;

namespace Nexus.GameEngine.Events;

/// <summary>
/// Concrete implementation of IEventBus providing thread-safe event publishing and subscription.
/// Uses concurrent collections for thread safety and maintains handler priority ordering.
/// </summary>
/// <remarks>
/// Initializes a new instance of the EventBus class.
/// </remarks>
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<IEventSubscription>> _subscriptions = new();
    private readonly ConcurrentDictionary<Guid, EventSubscription> _subscriptionLookup = new();
    private readonly object _subscriptionLock = new();
    private bool _disposed;

    /// <summary>
    /// Event fired when an exception occurs during event handling.
    /// </summary>
    public event Action<EventHandlingException>? EventHandlingError;

    /// <summary>
    /// Publishes an event to all registered handlers synchronously.
    /// </summary>
    /// <typeparam name="T">Type of event to publish</typeparam>
    /// <param name="eventArgs">The event to publish</param>
    public void Publish<T>(object sender, T eventArgs) where T : IGameEvent
    {
        ThrowIfDisposed();

        if (eventArgs == null)
            throw new ArgumentNullException(nameof(eventArgs));

        var eventType = typeof(T);
        if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
            return;

        var sortedSubscriptions = subscriptions
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Priority)
            .Cast<EventSubscription>()
            .ToList();

        foreach (var subscription in sortedSubscriptions)
        {
            if (eventArgs.IsHandled)
                break;

            try
            {
                subscription.InvokeSync(sender, eventArgs);
            }
            catch (Exception ex)
            {
                var handlingException = new EventHandlingException(
                    $"Error handling event {eventType.Name}",
                    eventArgs,
                    subscription.HandlerType,
                    subscription.SubscriptionId,
                    ex);

                EventHandlingError?.Invoke(handlingException);
            }
        }
    }

    /// <summary>
    /// Publishes an event to all registered handlers asynchronously.
    /// </summary>
    /// <typeparam name="T">Type of event to publish</typeparam>
    /// <param name="eventArgs">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the async operation</returns>
    public async Task PublishAsync<T>(object sender, T eventArgs, CancellationToken cancellationToken = default) where T : IGameEvent
    {
        ThrowIfDisposed();

        if (eventArgs == null)
            throw new ArgumentNullException(nameof(eventArgs));

        var eventType = typeof(T);
        if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
            return;

        var sortedSubscriptions = subscriptions
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Priority)
            .Cast<EventSubscription>()
            .ToList();

        foreach (var subscription in sortedSubscriptions)
        {
            if (eventArgs.IsHandled)
                break;

            try
            {
                await subscription.InvokeAsync(sender, eventArgs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested, stop processing further handlers
                break;
            }
            catch (Exception ex)
            {
                var handlingException = new EventHandlingException(
                    $"Error handling event {eventType.Name} asynchronously",
                    eventArgs,
                    subscription.HandlerType,
                    subscription.SubscriptionId,
                    ex);

                EventHandlingError?.Invoke(handlingException);
            }
        }
    }

    /// <summary>
    /// Subscribes to events of a specific type with a synchronous handler.
    /// </summary>
    /// <typeparam name="T">Type of event to subscribe to</typeparam>
    /// <param name="handler">Handler method to call when event is published</param>
    /// <param name="priority">Priority for handler execution order (higher values execute first)</param>
    /// <returns>Subscription token that can be used to unsubscribe</returns>
    public IEventSubscription Subscribe<T>(EventHandler<T> handler, int priority = 0) where T : IGameEvent
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(handler);

        lock (_subscriptionLock)
        {
            var subscription = new EventSubscription(typeof(T), handler, priority);

            var subscriptions = _subscriptions.GetOrAdd(typeof(T), _ => []);
            subscriptions.Add(subscription);
            _subscriptionLookup[subscription.SubscriptionId] = subscription;

            return subscription;
        }
    }

    /// <summary>
    /// Subscribes to events of a specific type with an asynchronous handler.
    /// </summary>
    /// <typeparam name="T">Type of event to subscribe to</typeparam>
    /// <param name="handler">Async handler method to call when event is published</param>
    /// <param name="priority">Priority for handler execution order (higher values execute first)</param>
    /// <returns>Subscription token that can be used to unsubscribe</returns>
    public IEventSubscription SubscribeAsync<T>(AsyncEventHandler<T> handler, int priority = 0) where T : IGameEvent
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(handler);

        lock (_subscriptionLock)
        {
            var subscription = new EventSubscription(typeof(T), handler, priority);

            var subscriptions = _subscriptions.GetOrAdd(typeof(T), _ => []);
            subscriptions.Add(subscription);
            _subscriptionLookup[subscription.SubscriptionId] = subscription;

            return subscription;
        }
    }

    /// <summary>
    /// Unsubscribes a previously registered handler.
    /// </summary>
    /// <param name="subscription">Subscription token returned from Subscribe methods</param>
    public void Unsubscribe(IEventSubscription subscription)
    {
        if (subscription == null || _disposed)
            return;

        lock (_subscriptionLock)
        {
            if (_subscriptionLookup.TryRemove(subscription.SubscriptionId, out var eventSubscription))
            {
                eventSubscription.Deactivate();
            }
        }
    }

    /// <summary>
    /// Unsubscribes all handlers for a specific event type.
    /// </summary>
    /// <typeparam name="T">Type of event to unsubscribe from</typeparam>
    public void UnsubscribeAll<T>() where T : IGameEvent
    {
        if (_disposed)
            return;

        lock (_subscriptionLock)
        {
            var eventType = typeof(T);
            if (_subscriptions.TryGetValue(eventType, out var subscriptions))
            {
                foreach (var subscription in subscriptions.Cast<EventSubscription>())
                {
                    subscription.Deactivate();
                    _subscriptionLookup.TryRemove(subscription.SubscriptionId, out _);
                }

                _subscriptions.TryRemove(eventType, out _);
            }
        }
    }

    /// <summary>
    /// Unsubscribes all handlers from all event types.
    /// </summary>
    public void UnsubscribeAll()
    {
        if (_disposed)
            return;

        lock (_subscriptionLock)
        {
            foreach (var subscription in _subscriptionLookup.Values)
            {
                subscription.Deactivate();
            }

            _subscriptionLookup.Clear();
            _subscriptions.Clear();
        }
    }

    /// <summary>
    /// Gets the number of active subscriptions for a specific event type.
    /// </summary>
    /// <typeparam name="T">Type of event to check</typeparam>
    /// <returns>Number of active subscriptions</returns>
    public int GetSubscriptionCount<T>() where T : IGameEvent
    {
        if (_disposed)
            return 0;

        var eventType = typeof(T);
        if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
            return 0;

        return subscriptions.Count(s => s.IsActive);
    }

    /// <summary>
    /// Gets the total number of active subscriptions across all event types.
    /// </summary>
    /// <returns>Total number of active subscriptions</returns>
    public int GetTotalSubscriptionCount()
    {
        if (_disposed)
            return 0;

        return _subscriptionLookup.Values.Count(s => s.IsActive);
    }

    /// <summary>
    /// Disposes the event bus and cleans up all subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        UnsubscribeAll();
        EventHandlingError = null;

        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EventBus));
    }
}