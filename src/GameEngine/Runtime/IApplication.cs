using Nexus.GameEngine.Components;
using Silk.NET.Windowing;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Defines the contract for the main application entry point in the Nexus Game Engine runtime.
/// Provides lifecycle management, startup configuration, and execution control for the game loop.
/// </summary>
public interface IApplication
{
    /// <summary>
    /// Gets or sets the startup template used to initialize the root content tree for the application.
    /// This property must be set before invoking <see cref="Run(WindowOptions)"/>.
    /// The template is applied after window and input context initialization, enabling deferred component creation.
    /// </summary>
    IComponentTemplate? StartupTemplate { get; set; }

    /// <summary>
    /// Starts the main application event loop with the specified window options.
    /// Initializes all runtime systems, applies the startup template, and enters the main event loop.
    /// This method blocks until the application is terminated or the window is closed.
    /// </summary>
    /// <param name="windowOptions">The configuration options for the main application window.</param>
    void Run(WindowOptions windowOptions);
}
