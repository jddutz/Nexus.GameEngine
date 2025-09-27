using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Nexus.GameEngine.Graphics.Rendering;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Abstract base class for managing collections of active runtime components.
/// Handles component collection management, activation, and deactivation with thread safety
/// and intelligent memory management features.
/// 
/// Thread Safety: Component registration and state queries are thread-safe.
/// Render operations must occur on the main game thread.
/// 
/// Memory Management: Uses weak references for inactive components, resource pooling,
/// automatic cleanup of disposed components, and memory pressure monitoring.
/// </summary>
public abstract class ComponentCollection : IComponentCollection, IDisposable
{
    private readonly ConcurrentDictionary<ComponentId, ComponentReference> _components = new();
    private readonly List<IRenderable> _renderableComponents = [];
    private readonly ReaderWriterLockSlim _renderLock = new();
    private readonly object _poolLock = new();
    private readonly Dictionary<Type, Queue<IRuntimeComponent>> _componentPool = [];
    private readonly Timer _cleanupTimer;
    private volatile bool _disposed = false;

    // Performance monitoring
    private long _memoryPressureThreshold = 50 * 1024 * 1024; // 50MB
    private DateTime _lastMemoryCheck = DateTime.UtcNow;
    private readonly TimeSpan _memoryCheckInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Internal class to manage component references with weak reference support
    /// </summary>
    private class ComponentReference(IRuntimeComponent component)
    {
        private readonly WeakReference<IRuntimeComponent> _weakRef = new(component);
        private IRuntimeComponent? _strongRef = component.IsEnabled ? component : null;
        private readonly ComponentId _id = component.Id;

        public ComponentId Id => _id;

        public IRuntimeComponent? GetComponent()
        {
            if (_strongRef != null)
                return _strongRef;

            _weakRef.TryGetTarget(out var component);
            return component;
        }

        public void SetActive(bool isActive)
        {
            if (isActive)
            {
                _weakRef.TryGetTarget(out _strongRef);
            }
            else
            {
                _strongRef = null;
            }
        }

        public bool IsAlive => _strongRef != null || (_weakRef.TryGetTarget(out _));
    }

    protected ComponentCollection()
    {
        // Start cleanup timer - runs every 5 minutes
        _cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Event raised when a component is added to the collection
    /// </summary>
    public event EventHandler<ComponentAddedEventArgs>? ComponentAdded;

    /// <summary>
    /// Event raised when a component is removed from the collection
    /// </summary>
    public event EventHandler<ComponentRemovedEventArgs>? ComponentRemoved;

    /// <summary>
    /// Adds a component to the managed collection. Does not handle activation.
    /// RuntimeComponent should already be loaded and ready for activation.
    /// Thread-safe for component registration.
    /// </summary>
    /// <param name="component">RuntimeComponent to add to the collection</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Add(IRuntimeComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (_disposed)
            return;

        var componentRef = new ComponentReference(component);
        if (_components.TryAdd(component.Id, componentRef))
        {
            // Add to renderable collection if applicable (thread-safe)
            if (component is IRenderable renderable)
            {
                AddRenderableComponent(renderable);
            }

            ComponentAdded?.Invoke(this, new() { Component = component });
        }
    }

    /// <summary>
    /// Removes a component from the managed collection. Does not handle deactivation.
    /// Thread-safe for component removal.
    /// </summary>
    /// <param name="component">RuntimeComponent to remove from the collection</param>
    /// <returns>True if component was found and removed, false otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Remove(IRuntimeComponent component)
    {
        if (component == null || _disposed)
            return false;

        if (_components.TryRemove(component.Id, out var componentRef))
        {
            // Remove from renderable collection and unsubscribe from events
            if (component is IRenderable renderable)
            {
                RemoveRenderableComponent(renderable);
            }

            // Return to pool if it was pooled
            ReturnToPool(component);

            ComponentRemoved?.Invoke(this, new() { Component = component });
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a component by its ID from the managed collection.
    /// RuntimeComponent will be deactivated before removal.
    /// Thread-safe for component removal.
    /// </summary>
    /// <param name="componentId">ID of component to remove</param>
    /// <returns>True if component was found and removed, false otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Remove(ComponentId componentId)
    {
        if (_disposed)
            return false;

        if (_components.TryRemove(componentId, out var componentRef))
        {
            var component = componentRef.GetComponent();
            if (component != null)
            {
                ReturnToPool(component);
                ComponentRemoved?.Invoke(this, new() { Component = component });
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all components currently in the managed collection.
    /// Thread-safe for component queries.
    /// </summary>
    /// <returns>Enumerable of all managed components</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual IEnumerable<IRuntimeComponent> GetComponents()
    {
        var components = new List<IRuntimeComponent>();
        foreach (var kvp in _components)
        {
            var component = kvp.Value.GetComponent();
            if (component != null)
                components.Add(component);
        }
        return components;
    }

    /// <summary>
    /// Gets all components which are currently active.
    /// Thread-safe for component queries.
    /// </summary>
    /// <returns>Enumerable of all active components</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual IEnumerable<IRuntimeComponent> GetActiveComponents()
    {
        var components = new List<IRuntimeComponent>();
        foreach (var kvp in _components)
        {
            var component = kvp.Value.GetComponent();
            if (component != null && component.IsEnabled)
                components.Add(component);
        }
        return components;
    }

    /// <summary>
    /// Gets all components of the specified type from the managed collection.
    /// Thread-safe for component queries.
    /// </summary>
    /// <typeparam name="T">Type of components to retrieve</typeparam>
    /// <returns>Enumerable of components of the specified type</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual IEnumerable<T> GetComponents<T>() where T : class, IRuntimeComponent
    {
        var components = new List<T>();
        foreach (var kvp in _components)
        {
            var component = kvp.Value.GetComponent();
            if (component is T typedComponent)
                components.Add(typedComponent);
        }
        return components;
    }

    /// <summary>
    /// Gets a component by its unique ID.
    /// Thread-safe for component queries.
    /// </summary>
    /// <param name="id">RuntimeComponent ID to search for</param>
    /// <returns>RuntimeComponent if found, null otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual IRuntimeComponent? GetComponent(ComponentId id)
    {
        if (_components.TryGetValue(id, out var componentRef))
        {
            return componentRef.GetComponent();
        }
        return null;
    }

    /// <summary>
    /// Gets a component by its unique ID with type safety.
    /// Thread-safe for component queries.
    /// </summary>
    /// <typeparam name="T">Expected component type</typeparam>
    /// <param name="id">RuntimeComponent ID to search for</param>
    /// <returns>RuntimeComponent if found and of correct type, null otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual T? GetComponent<T>(ComponentId id) where T : class, IRuntimeComponent
    {
        return GetComponent(id) as T;
    }

    /// <summary>
    /// Total number of components in the managed collection.
    /// Thread-safe for component queries.
    /// </summary>
    public virtual int Count => _components.Count;

    /// <summary>
    /// Checks if a component is in the managed collection.
    /// Thread-safe for component queries.
    /// </summary>
    /// <param name="component">RuntimeComponent to check for</param>
    /// <returns>True if component is managed, false otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Contains(IRuntimeComponent component)
    {
        return component != null && _components.ContainsKey(component.Id);
    }

    /// <summary>
    /// Checks if a component with the specified ID is in the managed collection.
    /// Thread-safe for component queries.
    /// </summary>
    /// <param name="componentId">RuntimeComponent ID to check for</param>
    /// <returns>True if component is managed, false otherwise</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Contains(ComponentId componentId)
    {
        return _components.ContainsKey(componentId);
    }

    /// <summary>
    /// Gets all renderable components for rendering operations.
    /// Components are already sorted by render order, no sorting needed.
    /// Must be called from the main game thread.
    /// </summary>
    /// <returns>Enumerable of renderable components in render order</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual IEnumerable<IRenderable> GetRenderableComponents()
    {
        _renderLock.EnterReadLock();
        try
        {
            var renderables = new List<IRenderable>();
            foreach (var renderable in _renderableComponents)
            {
                // Check if renderable is visible and if it's also an IRuntimeComponent, check if enabled
                if (renderable.IsVisible)
                {
                    if (renderable is IRuntimeComponent runtimeComponent)
                    {
                        if (runtimeComponent.IsEnabled)
                            renderables.Add(renderable);
                    }
                    else
                    {
                        // If it's not an IRuntimeComponent, just use visibility
                        renderables.Add(renderable);
                    }
                }
            }
            // Return already-sorted components (no sorting needed!)
            return renderables;
        }
        finally
        {
            _renderLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Updates component activation state and manages weak/strong references accordingly.
    /// Thread-safe method for state management.
    /// </summary>
    /// <param name="componentId">ID of component to update</param>
    /// <param name="isActive">Whether component should be active</param>
    protected virtual void UpdateComponentActivation(ComponentId componentId, bool isActive)
    {
        if (_components.TryGetValue(componentId, out var componentRef))
        {
            componentRef.SetActive(isActive);
        }
    }

    /// <summary>
    /// Adds a renderable component to the collection.
    /// Uses insertion order since 3D rendering relies on depth-based ordering.
    /// </summary>
    /// <param name="renderable">Renderable component to add</param>
    private void AddRenderableComponent(IRenderable renderable)
    {
        _renderLock.EnterWriteLock();
        try
        {
            _renderableComponents.Add(renderable);
        }
        finally
        {
            _renderLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes a renderable component from the sorted collection.
    /// </summary>
    /// <param name="renderable">Renderable component to remove</param>
    private void RemoveRenderableComponent(IRenderable renderable)
    {
        _renderLock.EnterWriteLock();
        try
        {
            _renderableComponents.Remove(renderable);
        }
        finally
        {
            _renderLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets a component from the pool or creates a new one if pool is empty.
    /// Thread-safe pooling mechanism.
    /// </summary>
    /// <typeparam name="T">Type of component to get from pool</typeparam>
    /// <returns>Component from pool or newly created component</returns>
    protected virtual T? GetFromPool<T>() where T : class, IRuntimeComponent, new()
    {
        lock (_poolLock)
        {
            var type = typeof(T);
            if (_componentPool.TryGetValue(type, out var queue) && queue.Count > 0)
            {
                return queue.Dequeue() as T;
            }
        }
        return null; // Caller should create new instance
    }

    /// <summary>
    /// Returns a component to the pool for reuse.
    /// Thread-safe pooling mechanism.
    /// </summary>
    /// <param name="component">Component to return to pool</param>
    protected virtual void ReturnToPool(IRuntimeComponent component)
    {
        if (component == null || _disposed)
            return;

        // Only pool certain types of frequently used components
        var type = component.GetType();
        if (!IsPoolableType(type))
            return;

        lock (_poolLock)
        {
            if (!_componentPool.TryGetValue(type, out var queue))
            {
                queue = new Queue<IRuntimeComponent>();
                _componentPool[type] = queue;
            }

            // Limit pool size to prevent excessive memory usage
            if (queue.Count < 20)
            {
                // Reset component state before pooling
                if (component is IResettable resettable)
                {
                    resettable.Reset();
                }
                queue.Enqueue(component);
            }
        }
    }

    /// <summary>
    /// Determines if a component type should be pooled.
    /// Override in derived classes to customize pooling behavior.
    /// </summary>
    /// <param name="type">Type to check for poolability</param>
    /// <returns>True if type should be pooled</returns>
    protected virtual bool IsPoolableType(Type type)
    {
        // Pool common UI components but avoid pooling heavy resources
        var typeName = type.Name;
        return typeName.Contains("Button") ||
               typeName.Contains("Label") ||
               typeName.Contains("Panel") ||
               typeName.Contains("Layout");
    }

    /// <summary>
    /// Removes components that have been disposed or are no longer reachable.
    /// This method is called periodically by the cleanup timer.
    /// </summary>
    /// <param name="state">Timer state (unused)</param>
    private void PerformCleanup(object? state)
    {
        if (_disposed)
            return;

        try
        {
            CleanupUnloadedComponents();
            CheckMemoryPressure();
        }
        catch (Exception ex)
        {
            // Log error but don't throw to avoid crashing cleanup timer
            System.Diagnostics.Debug.WriteLine($"ComponentCollection cleanup error: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes components that have been unloaded or disposed from the managed collection.
    /// This should be called periodically to clean up unloaded components.
    /// </summary>
    protected virtual void CleanupUnloadedComponents()
    {
        var componentsToRemove = new List<ComponentId>();

        foreach (var kvp in _components)
        {
            var componentRef = kvp.Value;
            var component = componentRef.GetComponent();

            // Remove if component is null (GC'd) or explicitly disposed
            if (component == null || IsComponentUnloaded(component))
            {
                componentsToRemove.Add(kvp.Key);
            }
        }

        // Remove dead components
        foreach (var componentId in componentsToRemove)
        {
            _components.TryRemove(componentId, out _);
        }

        // Clean up renderable collection
        CleanupRenderableComponents();
    }

    /// <summary>
    /// Removes disposed or null renderable components from the renderable collection.
    /// </summary>
    private void CleanupRenderableComponents()
    {
        _renderLock.EnterWriteLock();
        try
        {
            // Remove invalid components while maintaining sort order
            for (int i = _renderableComponents.Count - 1; i >= 0; i--)
            {
                var renderable = _renderableComponents[i];
                if (renderable == null || IsComponentUnloaded(renderable as IRuntimeComponent))
                {
                    _renderableComponents.RemoveAt(i);
                }
            }
        }
        finally
        {
            _renderLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Monitors memory pressure and performs adaptive cleanup if needed.
    /// </summary>
    private void CheckMemoryPressure()
    {
        var now = DateTime.UtcNow;
        if (now - _lastMemoryCheck < _memoryCheckInterval)
            return;

        _lastMemoryCheck = now;

        // Check current memory usage
        var currentMemory = GC.GetTotalMemory(false);
        if (currentMemory > _memoryPressureThreshold)
        {
            // High memory pressure - perform aggressive cleanup
            PerformAggressiveCleanup();

            // Force garbage collection if memory is still high
            if (GC.GetTotalMemory(false) > _memoryPressureThreshold)
            {
                GC.Collect(2, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            }
        }
    }

    /// <summary>
    /// Performs aggressive cleanup during high memory pressure.
    /// </summary>
    private void PerformAggressiveCleanup()
    {
        // Clear component pools
        lock (_poolLock)
        {
            _componentPool.Clear();
        }

        // Force weak reference cleanup for inactive components
        foreach (var kvp in _components)
        {
            var component = kvp.Value.GetComponent();
            if (component != null && !component.IsEnabled)
            {
                kvp.Value.SetActive(false); // Convert to weak reference
            }
        }
    }

    /// <summary>
    /// Determines if a component is unloaded and should be removed.
    /// </summary>
    /// <param name="component">RuntimeComponent to check</param>
    /// <returns>True if component is unloaded or disposed</returns>
    protected virtual bool IsComponentUnloaded(IRuntimeComponent? component)
    {
        if (component == null)
            return true;

        // Check if component implements IDisposable and is disposed
        if (component is IDisposable disposable)
        {
            // Use reflection to check if there's an IsDisposed property
            var isDisposedProperty = component.GetType().GetProperty("IsDisposed");
            if (isDisposedProperty?.GetValue(component) is bool isDisposed && isDisposed)
            {
                return true;
            }
        }

        // Check for unloaded state if the interface exists
        var isUnloadedProperty = component.GetType().GetProperty("IsUnloaded");
        if (isUnloadedProperty?.GetValue(component) is bool isUnloaded)
        {
            return isUnloaded;
        }

        return false;
    }

    /// <summary>
    /// Disposes of the ComponentCollection and releases all resources.
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Stop cleanup timer
        _cleanupTimer?.Dispose();

        // Dispose of all managed components
        foreach (var kvp in _components)
        {
            var component = kvp.Value.GetComponent();
            if (component is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        // Clear collections
        _components.Clear();

        // Clear renderable components and unsubscribe from events
        _renderLock.EnterWriteLock();
        try
        {
            _renderableComponents.Clear();
        }
        finally
        {
            _renderLock.ExitWriteLock();
        }

        // Clear pools
        lock (_poolLock)
        {
            foreach (var queue in _componentPool.Values)
            {
                while (queue.Count > 0)
                {
                    if (queue.Dequeue() is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            _componentPool.Clear();
        }

        // Dispose synchronization objects
        _renderLock.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Interface for components that can be reset when returned to pool.
    /// </summary>
    protected interface IResettable
    {
        void Reset();
    }
}
