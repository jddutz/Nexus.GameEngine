namespace Nexus.GameEngine.Components;

/// <summary>
/// Base interface for all components in the system.
/// Defines the contract for component identity, hierarchy, lifecycle, and event management.
/// </summary>
public interface IComponent : IConfigurable, IDisposable
{    
    /// <summary>
    /// Gets or sets whether this component is currently loaded.
    /// If false, the component is considered unloaded and can be disposed.
    /// </summary>
    bool IsLoaded { get; set; }

    /// <summary>
    /// Gets or sets the content manager used to create and manage subcomponents.
    /// </summary>
    IContentManager? ContentManager { get; set; }

    /// <summary>
    /// Occurs when the child collection of this component changes (child added or removed).
    /// </summary>
    event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged;

    /// <summary>
    /// Occurs when this component is about to be unloaded.
    /// </summary>
    event EventHandler<EventArgs>? Unloading;

    /// <summary>
    /// Occurs when this component has been unloaded.
    /// </summary>
    event EventHandler<EventArgs>? Unloaded;

    /// <summary>
    /// Gets the parent component in the component tree.
    /// Automatically set when <see cref="AddChild"/> is called on the parent.
    /// Read-only from outside the component; the tree structure is managed internally.
    /// </summary>
    IComponent? Parent { get; set; }

    /// <summary>
    /// Gets the child components of this component.
    /// Use <see cref="AddChild"/> and <see cref="RemoveChild"/> methods to modify the collection.
    /// </summary>
    IEnumerable<IComponent> Children { get; }

    /// <summary>
    /// Returns child components of the specified type <typeparamref name="T"/>.
    /// By default returns only immediate children. Set recursive=true to search the entire subtree.
    /// </summary>
    /// <typeparam name="T">The type of child components to return.</typeparam>
    /// <param name="filter">Optional predicate to filter child components.</param>
    /// <param name="recursive">If true, searches all descendants; if false (default), only immediate children.</param>
    /// <param name="depthFirst">If true and recursive, performs depth-first search; otherwise breadth-first.</param>
    /// <returns>Enumerable of child components of type <typeparamref name="T"/>.</returns>
    IEnumerable<T> GetChildren<T>(Func<T, bool>? filter = null, bool recursive = false, bool depthFirst = false)
        where T : IComponent;

    /// <summary>
    /// Returns all sibling components of the specified type <typeparamref name="T"/>.
    /// Optionally filters the siblings using the provided predicate.
    /// </summary>
    /// <typeparam name="T">The type of sibling components to return.</typeparam>
    /// <param name="filter">Optional predicate to filter sibling components.</param>
    /// <returns>Enumerable of sibling components of type <typeparamref name="T"/>.</returns>
    IEnumerable<T> GetSiblings<T>(Func<T, bool>? filter = null)
        where T : IComponent;

    /// <summary>
    /// Finds the nearest parent component of the specified type <typeparamref name="T"/>.
    /// Optionally filters parent components using the provided predicate.
    /// </summary>
    /// <typeparam name="T">The type of parent component to find.</typeparam>
    /// <param name="filter">Optional predicate to filter parent components.</param>
    /// <returns>The nearest parent component of type <typeparamref name="T"/>, or default if not found.</returns>
    T? FindParent<T>(Func<T, bool>? filter = null);

    /// <summary>
    /// Adds a child component to this component's child collection.
    /// </summary>
    /// <param name="child">The child component to add.</param>
    void AddChild(IComponent child);

    /// <summary>
    /// Removes a child component from this component's child collection.
    /// </summary>
    /// <param name="child">The child component to remove.</param>
    void RemoveChild(IComponent child);

    /// <summary>
    /// Unloads this component and all subcomponents, preparing for disposal.
    /// This should release all resources and detach from the component tree.
    /// </summary>
    void Unload();
}