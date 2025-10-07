using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Interface for window management services
/// Handles window lifecycle and provides access to the singleton application window
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Gets the singleton application window
    /// Throws InvalidOperationException if it does not exist
    /// </summary>
    IWindow? GetWindow();

    /// <summary>
    /// Gets the singleton application window, creating it if it doesn't exist
    /// </summary>
    /// <remarks>
    /// This returns the same IWindow instance throughout the application lifecycle.
    /// Use this method to access window properties and events directly from Silk.NET.
    /// </remarks>
    IWindow GetOrCreateWindow();

    /// <summary>
    /// Gets the input context for the window
    /// </summary>
    IInputContext GetInputContext();

    /// <summary>
    /// Whether the window has been created and is available
    /// </summary>
    bool IsWindowCreated { get; }

    /// <summary>
    /// Event raised when the window is loaded and ready
    /// </summary>
    /// <remarks>
    /// This is a convenience event that wraps the IWindow.Load event
    /// </remarks>
    event EventHandler? WindowLoaded;

    /// <summary>
    /// Event raised when the window is closing
    /// </summary>
    /// <remarks>
    /// This is a convenience event that wraps the IWindow.Closing event
    /// </remarks>
    event EventHandler? WindowClosing;

    /// <summary>
    /// Initialize the window service
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Toggle between fullscreen and windowed mode
    /// </summary>
    /// <remarks>
    /// This is a convenience method for common fullscreen toggling.
    /// For more control, use GetOrCreateWindow().WindowState directly.
    /// </remarks>
    void ToggleFullscreen();

    /// <summary>
    /// Set fullscreen mode
    /// </summary>
    /// <param name="fullscreen">True for fullscreen, false for windowed</param>
    /// <remarks>
    /// This is a convenience method for setting fullscreen state.
    /// For more control, use GetOrCreateWindow().WindowState directly.
    /// </remarks>
    Task SetFullscreenAsync(bool fullscreen);

    /// <summary>
    /// Run the window's main loop
    /// </summary>
    /// <remarks>
    /// This delegates to the underlying IWindow.Run() method
    /// </remarks>
    void Run();

    /// <summary>
    /// Close the window
    /// </summary>
    /// <remarks>
    /// This delegates to the underlying IWindow.Close() method
    /// </remarks>
    void Close();

    /// <summary>
    /// Cleanup resources
    /// </summary>
    Task CleanupAsync();
}
