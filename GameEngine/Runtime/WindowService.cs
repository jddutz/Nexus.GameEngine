using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Window service implementation using Silk.NET
/// Manages the singleton application window lifecycle
/// </summary>
public class WindowService(ILoggerFactory loggerFactory) : IWindowService
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(WindowService));
    private IWindow? _window;
    private IInputContext? _inputContext;
    private bool _isInitialized = false;

    public bool IsWindowCreated => _window != null;

    public event EventHandler? WindowLoaded;
    public event EventHandler? WindowClosing;

    public IWindow GetWindow() => _window ??
        throw new InvalidOperationException("Application Window has not been initialized yet.");

    public IWindow GetOrCreateWindow()
    {
        if (_window == null)
        {
            CreateWindow();
        }
        return _window!;
    }

    public IInputContext GetInputContext()
    {
        if (_inputContext == null)
        {
            var window = GetOrCreateWindow();
            if (window != null)
            {
                _inputContext = window.CreateInput();
                _logger.LogDebug("Input context created");
            }
        }
        return _inputContext!;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        GetOrCreateWindow(); // Ensure window is created
        _isInitialized = true;

        _logger.LogDebug("Window service initialized");
        await Task.CompletedTask;
    }

    public void Run()
    {
        var window = GetOrCreateWindow();
        window.Run();
    }

    public void Close()
    {
        _window?.Close();
    }

    public async Task CleanupAsync()
    {
        try
        {
            // Prevent double cleanup
            if (!_isInitialized && _window == null)
            {
                _logger.LogDebug("Window service already cleaned up, skipping");
                return;
            }

            _logger.LogDebug("Starting window service cleanup...");

            // Dispose input context first, before closing window
            // This prevents GLFW callback issues during disposal
            if (_inputContext != null)
            {
                try
                {
                    _inputContext.Dispose();
                    _logger.LogDebug("Input context disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing input context");
                }
                _inputContext = null;
            }

            // Give a brief moment for any pending input operations to complete
            await Task.Delay(10);

            // Close and dispose window
            if (_window != null)
            {
                try
                {
                    _window.Close();
                    _window.Dispose();
                    _logger.LogDebug("Window closed and disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing window");
                }
                _window = null;
            }

            _isInitialized = false;
            _logger.LogDebug("Window service cleaned up successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during window service cleanup");
        }
    }

    private void CreateWindow()
    {
        // Create window options for the application
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1920, 1080);
        options.Title = "NexusRealms. - Prelude";
        options.WindowBorder = WindowBorder.Hidden;
        options.WindowState = WindowState.Fullscreen; // Start in fullscreen
        options.VSync = true;

        // Create the window
        _window = Window.Create(options);

        // Set up window event handlers
        _window.Load += OnWindowLoad;
        _window.Closing += OnWindowClosing;
        _window.Resize += OnWindowResize;

        _logger.LogDebug("Window created - {Width}x{Height} (Fullscreen)", options.Size.X, options.Size.Y);
    }

    private void OnWindowLoad()
    {
        _logger.LogDebug("Window loaded");
        WindowLoaded?.Invoke(this, EventArgs.Empty);
    }

    private void OnWindowClosing()
    {
        _logger.LogDebug("Window closing");
        WindowClosing?.Invoke(this, EventArgs.Empty);
    }

    private void OnWindowResize(Vector2D<int> newSize)
    {
        _logger.LogDebug("Window resized to {Width}x{Height}", newSize.X, newSize.Y);
        // Note: Consumers should subscribe to _window.Resize directly if they need resize events
    }

    public void ToggleFullscreen()
    {
        if (_window != null)
        {
            var isCurrentlyFullscreen = _window.WindowState == WindowState.Fullscreen;

            if (isCurrentlyFullscreen)
            {
                // Switch to windowed mode - set properties in correct order
                _window.WindowState = WindowState.Normal;
                _window.WindowBorder = WindowBorder.Resizable;
                _window.Size = new Vector2D<int>(1280, 720);
                _logger.LogDebug("Switched to windowed mode - 1280x720");
            }
            else
            {
                // Switch to fullscreen mode - set border first, then state
                _window.WindowBorder = WindowBorder.Hidden;
                _window.WindowState = WindowState.Fullscreen;
                _logger.LogDebug("Switched to fullscreen mode");
            }
        }
    }

    public async Task SetFullscreenAsync(bool fullscreen)
    {
        if (_window != null)
        {
            if (fullscreen)
            {
                _window.WindowBorder = WindowBorder.Hidden;
                _window.WindowState = WindowState.Fullscreen;
            }
            else
            {
                _window.Size = new Vector2D<int>(1280, 720);
                _window.WindowBorder = WindowBorder.Resizable;
                _window.WindowState = WindowState.Normal;
            }

            // Give the window system time to process the state change
            await Task.Delay(50);

            _logger.LogDebug("Fullscreen mode set to: {Fullscreen}", fullscreen);
        }
    }
}
