using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Represents the main application interface for the game engine runtime.
/// Orchestrates the lifecycle of game services and manages the main application loop.
/// </summary>
public interface IApplication
{
    /// <summary>
    /// Gets or sets the template used to configure the initial application state after startup.
    /// This template is applied after window and input context initialization, enabling deferred component creation.
    /// </summary>
    IComponentTemplate? StartupTemplate { get; set; }

    /// <summary>
    /// Starts the application asynchronously and runs the main application loop.
    /// Initializes all runtime systems, applies the startup template, and enters the main event loop.
    /// This method will block until the application is terminated or the cancellation token is triggered.
    /// </summary>
    void Run();
}
