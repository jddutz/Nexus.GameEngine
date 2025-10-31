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
    IWindow GetWindow();

    /// <summary>
    /// Gets the singleton application window, creating it if it doesn't exist
    /// </summary>
    /// <remarks>
    /// This returns the same IWindow instance throughout the application lifecycle.
    /// Use this method to access window properties and events directly from Silk.NET.
    /// </remarks>
    IWindow GetOrCreateWindow(WindowOptions options);

    /// <summary>
    /// Gets or creates input context associated with the application window.
    /// Throws InvalidOperationException if the window does not exist
    /// </summary>
    /// <returns></returns>
    IInputContext InputContext { get; }

    // TODO: Determine proper timing for SwapChain creation based on window event testing
    // /// <summary>
    // /// Gets the swapchain associated with the application window.
    // /// The swapchain is created when GetOrCreateWindow() is first called.
    // /// Throws InvalidOperationException if the window (and thus swapchain) does not exist.
    // /// </summary>
    // ISwapChain SwapChain { get; }
}