namespace Nexus.GameEngine.Runtime;

/// <summary>
/// Provides data for performance-related events.
/// </summary>
public class PerformanceEventArgs(double currentFps, double targetFps, bool isPerformanceDrop) : EventArgs
{
    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    public double CurrentFps { get; } = currentFps;

    /// <summary>
    /// Gets the target frames per second.
    /// </summary>
    public double TargetFps { get; } = targetFps;

    /// <summary>
    /// Gets the performance ratio (CurrentFps / TargetFps).
    /// </summary>
    public double PerformanceRatio { get; } = targetFps > 0 ? currentFps / targetFps : 1.0;

    /// <summary>
    /// Gets whether this represents a performance drop.
    /// </summary>
    public bool IsPerformanceDrop { get; } = isPerformanceDrop;
}