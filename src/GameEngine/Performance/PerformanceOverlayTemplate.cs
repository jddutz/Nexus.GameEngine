namespace Nexus.GameEngine.Performance;

/// <summary>
/// Template for creating PerformanceOverlay components.
/// Configures real-time performance monitoring display.
/// </summary>
public record PerformanceOverlayTemplate : Template
{
    /// <summary>
    /// Whether the overlay is visible and collecting data.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Frame time threshold in milliseconds for performance warnings.
    /// Default: 6.67ms (150 FPS target)
    /// </summary>
    public double WarningThresholdMs { get; init; } = 6.67;

    /// <summary>
    /// Number of frames to average for FPS calculation.
    /// Default: 60 frames (~1 second at 60 FPS)
    /// </summary>
    public int AverageFrameCount { get; init; } = 60;

    /// <summary>
    /// How often to update the display, in seconds.
    /// Default: 0.5 seconds (2 updates per second)
    /// </summary>
    public double UpdateIntervalSeconds { get; init; } = 0.5;
}
