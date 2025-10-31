namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Provides a singleton factory for the Silk.NET application window.
/// Handles window creation, access, and disposal for the application lifecycle.
/// </summary>
public class WindowService : IWindowService, IDisposable
{
    private IWindow? _window;
    private IInputContext? _inputContext;

    /// <summary>
    /// Gets the singleton application window instance.
    /// Throws <see cref="InvalidOperationException"/> if the window has not been created.
    /// </summary>
    /// <returns>The initialized Silk.NET <see cref="IWindow"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the window has not been created.</exception>
    public IWindow GetWindow() => _window ??
        throw new InvalidOperationException("Application Window has not been initialized yet.");

    /// <summary>
    /// Gets the singleton application window, creating it if it does not already exist.
    /// </summary>
    /// <param name="options">The Silk.NET window options to use for creation.</param>
    /// <returns>The Silk.NET <see cref="IWindow"/> instance.</returns>
    public IWindow GetOrCreateWindow(WindowOptions options)
    {
        _window ??= Window.Create(options);
        return _window!;
    }

    public IInputContext InputContext
    {
        get
        {
            _inputContext ??= GetWindow().CreateInput();
            return _inputContext;
        }
    }

    /// <summary>
    /// Disposes the window service and the underlying Silk.NET window instance.
    /// Calls <see cref="GC.SuppressFinalize(object)"/> to prevent finalization if derived types introduce a finalizer.
    /// </summary>
    public void Dispose()
    {
        _window?.Dispose();
        _window = null;
        GC.SuppressFinalize(this);
    }
}
