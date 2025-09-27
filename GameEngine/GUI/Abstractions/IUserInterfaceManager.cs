using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.GUI.Abstractions;

/// <summary>
/// Manages user interface lifecycle, handling active UI switching and updates.
/// Maintains a single active component tree. Rendering is handled by the IRenderer.
/// </summary>
public interface IUserInterfaceManager : IDisposable
{
    /// <summary>
    /// Gets the currently active user interface component.
    /// </summary>
    IRuntimeComponent? Active { get; }

    /// <summary>
    /// Creates a new user interface instance with the specified template.
    /// </summary>
    /// <param name="template">The template to use for creating the user interface.</param>
    void Create(IComponentTemplate template);

    /// <summary>
    /// Activates the user interface with the specified name.
    /// </summary>
    /// <param name="name">The name of the user interface to activate.</param>
    /// <returns>True if the activation was successful; otherwise, false.</returns>
    bool Activate(string name);

    /// <summary>
    /// Updates the user interface with the specified delta time.
    /// If no active user interface is available, the update is gracefully skipped.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    void Update(double deltaTime);

    /// <summary>
    /// Tries to get the user interface with the specified name.
    /// </summary>
    /// <param name="name">The name of the user interface to retrieve.</param>
    /// <param name="component">The retrieved component, if found.</param>
    /// <returns>True if the user interface was found; otherwise, false.</returns>
    bool TryGet(string name, out IRuntimeComponent? component);

    /// <summary>
    /// Gets an existing user interface or creates it if it doesn't exist, optionally activating it.
    /// This is a convenience method that combines Create() and Activate() operations.
    /// </summary>
    /// <param name="template">The template to use for creating the user interface if it doesn't exist.</param>
    /// <param name="activate">Whether to activate the user interface after getting/creating it.</param>
    /// <returns>The user interface component, or null if creation failed.</returns>
    IRuntimeComponent? GetOrCreate(IComponentTemplate template, bool activate = true);
}