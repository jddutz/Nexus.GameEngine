namespace Nexus.GameEngine.Components;

/// <summary>
/// Interface for managing collections of active runtime components.
/// Provides basic collection management without handling component creation or caching.
/// Components are activated/deactivated and enabled/disabled independently.
/// 
/// Thread Safety: Component registration and state queries are thread-safe.
/// Render operations must occur on the main game thread.
/// 
/// Memory Management: Implementations should support weak references for inactive components,
/// resource pooling, automatic cleanup of disposed components, and memory pressure monitoring.
/// </summary>
public interface IComponentCollection : IDisposable
{
    /// <summary>
    /// Event raised when a component is added to the collection
    /// </summary>
    event EventHandler<ComponentAddedEventArgs> ComponentAdded;

    /// <summary>
    /// Event raised when a component is removed from the collection
    /// </summary>
    event EventHandler<ComponentRemovedEventArgs> ComponentRemoved;

    /// <summary>
    /// Adds a component to the managed collection.
    /// RuntimeComponent should already be loaded and ready for activation.
    /// </summary>
    /// <param name="component">RuntimeComponent to add to the collection</param>
    void Add(IRuntimeComponent component);

    /// <summary>
    /// Removes a component from the managed collection.
    /// RuntimeComponent will be deactivated before removal.
    /// </summary>
    /// <param name="component">RuntimeComponent to remove from the collection</param>
    /// <returns>True if component was found and removed, false otherwise</returns>
    bool Remove(IRuntimeComponent component);

    /// <summary>
    /// Removes a component by its ID from the managed collection.
    /// RuntimeComponent will be deactivated before removal.
    /// </summary>
    /// <param name="componentId">ID of component to remove</param>
    /// <returns>True if component was found and removed, false otherwise</returns>
    bool Remove(ComponentId componentId);

    /// <summary>
    /// Gets all components currently in the managed collection.
    /// </summary>
    /// <returns>Enumerable of all managed components</returns>
    IEnumerable<IRuntimeComponent> GetComponents();

    /// <summary>
    /// Gets all components of the specified type from the managed collection.
    /// </summary>
    /// <typeparam name="T">Type of components to retrieve</typeparam>
    /// <returns>Enumerable of components of the specified type</returns>
    IEnumerable<T> GetComponents<T>() where T : class, IRuntimeComponent;

    /// <summary>
    /// Gets a component by its unique ID.
    /// </summary>
    /// <param name="id">RuntimeComponent ID to search for</param>
    /// <returns>RuntimeComponent if found, null otherwise</returns>
    IRuntimeComponent? GetComponent(ComponentId id);

    /// <summary>
    /// Gets a component by its unique ID with type safety.
    /// </summary>
    /// <typeparam name="T">Expected component type</typeparam>
    /// <param name="id">RuntimeComponent ID to search for</param>
    /// <returns>RuntimeComponent if found and of correct type, null otherwise</returns>
    T? GetComponent<T>(ComponentId id) where T : class, IRuntimeComponent;

    /// <summary>
    /// Total number of components in the managed collection.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Checks if a component is in the managed collection.
    /// </summary>
    /// <param name="component">RuntimeComponent to check for</param>
    /// <returns>True if component is managed, false otherwise</returns>
    bool Contains(IRuntimeComponent component);

    /// <summary>
    /// Checks if a component with the specified ID is in the managed collection.
    /// </summary>
    /// <param name="componentId">RuntimeComponent ID to check for</param>
    /// <returns>True if component is managed, false otherwise</returns>
    bool Contains(ComponentId componentId);
}
