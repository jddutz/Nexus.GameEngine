namespace Nexus.GameEngine.GUI.Abstractions;

/// <summary>
/// Manages user interface lifecycle, handling active UI switching and updates.
/// Maintains a single active UserInterface. Rendering is handled by the IRenderer.
/// </summary>
public interface IUserInterfaceManager : IDisposable
{
    /// <summary>
    /// Gets the currently active user interface.
    /// </summary>
    IUserInterface? Active { get; }

    /// <summary>
    /// Creates a new user interface instance with the specified name and template.
    /// </summary>
    /// <param name="uiTemplate">The template to use for creating the user interface.</param>
    void Create(UserInterface.Template uiTemplate);

    /// <summary>
    /// Activates the user interface with the specified name.
    /// </summary>
    /// <param name="name">The name of the user interface to activate.</param>
    /// <returns>True if the activation was successful; otherwise, false.</returns>
    bool Activate(string name);

    /// <summary>
    /// Updates the user interface with the specified delta time.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    void Update(double deltaTime);

    /// <summary>
    /// Tries to get the user interface with the specified name.
    /// </summary>
    /// <param name="name">The name of the user interface to retrieve.</param>
    /// <param name="ui">The retrieved user interface, if found.</param>
    /// <returns>True if the user interface was found; otherwise, false.</returns>
    bool TryGet(string name, out IUserInterface? ui);
}