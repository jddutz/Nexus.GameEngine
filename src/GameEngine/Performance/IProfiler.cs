namespace Nexus.GameEngine.Performance;

/// <summary>
/// Service interface for performance profiling infrastructure.
/// Provides methods to collect, aggregate, and analyze timing data
/// for engine subsystems and custom operations.
/// </summary>
public interface IProfiler
{
    /// <summary>
    /// Gets whether profiling is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables profiling data collection.
    /// </summary>
    void Enable();

    /// <summary>
    /// Disables profiling data collection.
    /// </summary>
    void Disable();

    /// <summary>
    /// Records a performance sample with the specified label and elapsed time.
    /// </summary>
    /// <param name="label">Identifier for the operation being measured.</param>
    /// <param name="elapsedMs">Measured duration in milliseconds.</param>
    void RecordSample(string label, double elapsedMs);

    /// <summary>
    /// Marks the start of a new frame for profiling data collection.
    /// Resets per-frame tracking and prepares for new samples.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// Marks the end of the current frame and finalizes frame profile data.
    /// </summary>
    /// <returns>Frame profile containing all samples collected during the frame.</returns>
    FrameProfile EndFrame();

    /// <summary>
    /// Generates an aggregated performance report across the specified number of frames.
    /// </summary>
    /// <param name="frameCount">Number of recent frames to include in the report.</param>
    /// <returns>Performance report with aggregated timing data and analysis.</returns>
    PerformanceReport GenerateReport(int frameCount);

    /// <summary>
    /// Clears all collected profiling data.
    /// </summary>
    void Clear();
}
