using System.Reflection;

namespace Nexus.GameEngine.Events;

/// <summary>
/// Internal implementation of IEventSubscription.
/// </summary>
internal class EventSubscription(Type eventType, Delegate handler, int priority) : IEventSubscription
{
    private readonly Delegate _handler = handler;
    private bool _isActive = true;

    public Guid SubscriptionId { get; } = Guid.NewGuid();
    public Type EventType { get; } = eventType;
    public int Priority { get; } = priority;
    public bool IsActive => _isActive;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public Type HandlerType => _handler.GetType();

    public void InvokeSync(object sender, IGameEvent eventArgs)
    {
        if (!_isActive)
            return;

        if (_handler is EventHandler<IGameEvent> syncHandler)
        {
            syncHandler(sender, eventArgs);
        }
        else
        {
            try
            {
                _handler.DynamicInvoke(eventArgs);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Unwrap the TargetInvocationException to get the original exception
                throw ex.InnerException;
            }
        }
    }

    public async Task InvokeAsync(object sender, IGameEvent eventArgs, CancellationToken cancellationToken)
    {
        if (!_isActive)
            return;

        // Check for strongly-typed async handlers first
        if (_handler is Func<IGameEvent, Task> asyncFunc)
        {
            await asyncFunc(eventArgs);
        }
        else if (_handler is EventHandler<IGameEvent> syncHandler)
        {
            // Execute sync handler on thread pool
            await Task.Run(() => syncHandler(sender, eventArgs), cancellationToken);
        }
        else
        {
            // Dynamic invoke for generic handlers
            try
            {
                var result = _handler.DynamicInvoke(eventArgs);
                if (result is Task task)
                {
                    // If the task is already completed, cancellation has no effect
                    // But if it's still running, we can use the cancellation token to timeout
                    if (!task.IsCompleted && cancellationToken.CanBeCanceled)
                    {
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        await task.WaitAsync(timeoutCts.Token);
                    }
                    else
                    {
                        await task;
                    }
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Unwrap the TargetInvocationException to get the original exception
                throw ex.InnerException;
            }
        }
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    public void Dispose()
    {
        Deactivate();
    }
}