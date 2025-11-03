using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Defines the contract for the main application entry point in the Nexus Game Engine runtime.
/// Provides lifecycle management, startup configuration, and execution control for the game loop.
/// </summary>
public interface IApplication
{
    /// <summary>
    /// Starts the main application event loop with the specified window options.
    /// Initializes all runtime systems, applies the startup template, and enters the main event loop.
    /// This method blocks until the application is terminated or the window is closed.
    /// </summary>
    /// <param name="windowOptions">The configuration options for the main application window.</param>
    /// <param name="startupTemplate">The template for the root component to load on startup.</param>
    void Run(WindowOptions windowOptions, Template startupTemplate);
}
