using Nexus.GameEngine.Runtime.Settings;

namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Defines the contract for high-precision game timing with frame rate management
/// and performance monitoring capabilities.
/// </summary>
public interface IGameTimer
{
    /// <summary>
    /// Gets or sets the target frames per second. Set to 0 for unlimited framerate.
    /// </summary>
    int TargetFramerate { get; set; }

    /// <summary>
    /// Gets or sets whether VSync is enabled for frame limiting.
    /// </summary>
    bool EnableVSync { get; set; }

    /// <summary>
    /// Gets or sets whether FPS reporting and metrics collection is enabled.
    /// </summary>
    bool EnableFpsReporting { get; set; }

    /// <summary>
    /// Gets the time elapsed since the last frame in seconds.
    /// </summary>
    double DeltaTime { get; }

    /// <summary>
    /// Gets the total elapsed time since the timer was started in seconds.
    /// </summary>
    double TotalTime { get; }

    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    double CurrentFps { get; }

    /// <summary>
    /// Gets the average frames per second over the measurement window.
    /// </summary>
    double AverageFps { get; }

    /// <summary>
    /// Gets the minimum frames per second recorded in the current measurement window.
    /// </summary>
    double MinFps { get; }

    /// <summary>
    /// Gets the maximum frames per second recorded in the current measurement window.
    /// </summary>
    double MaxFps { get; }

    /// <summary>
    /// Gets the time taken for the last frame in milliseconds.
    /// </summary>
    double FrameTimeMs { get; }

    /// <summary>
    /// Gets whether the timer is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the game timer.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the game timer.
    /// </summary>
    void Stop();

    /// <summary>
    /// Updates the timer for the current frame. Call this once per frame.
    /// </summary>
    void Tick();

    /// <summary>
    /// Waits for the appropriate time to maintain the target frame rate.
    /// Only effective when TargetFramerate > 0 and EnableVSync is false.
    /// </summary>
    void WaitForNextFrame();

    /// <summary>
    /// Resets all performance metrics (FPS, min/max values, etc.).
    /// </summary>
    void ResetMetrics();

    /// <summary>
    /// Updates the timer configuration from application settings.
    /// Useful for applying settings changes at runtime.
    /// </summary>
    /// <param name="settings">The updated application settings</param>
    void UpdateConfiguration(ApplicationSettings settings);

    /// <summary>
    /// Event raised when significant performance changes are detected.
    /// </summary>
    event EventHandler<PerformanceEventArgs>? PerformanceChanged;
}
