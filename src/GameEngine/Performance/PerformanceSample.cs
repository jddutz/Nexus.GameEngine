namespace Nexus.GameEngine.Performance;

/// <summary>
/// Represents a single performance measurement for an operation or subsystem.
/// Value type to minimize allocations during profiling.
/// </summary>
public readonly struct PerformanceSample
{
    /// <summary>
    /// Identifier for the operation being measured (e.g., "Render", "Update", "ResourceLoad").
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Measured duration in milliseconds.
    /// </summary>
    public double ElapsedMs { get; }

    /// <summary>
    /// High-resolution timestamp when the sample was recorded (Stopwatch ticks).
    /// </summary>
    public long Timestamp { get; }

    /// <summary>
    /// Creates a new performance sample.
    /// </summary>
    /// <param name="label">Identifier for the operation.</param>
    /// <param name="elapsedMs">Measured duration in milliseconds.</param>
    /// <param name="timestamp">High-resolution timestamp (Stopwatch ticks).</param>
    public PerformanceSample(string label, double elapsedMs, long timestamp)
    {
        Label = label;
        ElapsedMs = elapsedMs;
        Timestamp = timestamp;
    }

    public override string ToString() => $"{Label}: {ElapsedMs:F3}ms";
}
