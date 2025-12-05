using System.Diagnostics;

namespace Nexus.GameEngine.Performance;

/// <summary>
/// Represents aggregated performance data for a single frame.
/// Contains all samples collected during the frame and calculated metrics.
/// </summary>
public class FrameProfile
{
    private readonly List<PerformanceSample> _samples;
    private readonly long _frameStartTimestamp;
    private readonly long _frameEndTimestamp;

    /// <summary>
    /// Gets the frame number (sequential counter).
    /// </summary>
    public int FrameNumber { get; }

    /// <summary>
    /// Gets the total frame time in milliseconds.
    /// </summary>
    public double TotalFrameTimeMs { get; }

    /// <summary>
    /// Gets the frames per second calculated from total frame time.
    /// </summary>
    public double FPS => TotalFrameTimeMs > 0 ? 1000.0 / TotalFrameTimeMs : 0;

    /// <summary>
    /// Gets all performance samples collected during this frame.
    /// </summary>
    public IReadOnlyList<PerformanceSample> Samples => _samples;

    /// <summary>
    /// Gets the timestamp when this frame started (Stopwatch ticks).
    /// </summary>
    public long FrameStartTimestamp => _frameStartTimestamp;

    /// <summary>
    /// Gets the timestamp when this frame ended (Stopwatch ticks).
    /// </summary>
    public long FrameEndTimestamp => _frameEndTimestamp;

    /// <summary>
    /// Creates a new frame profile.
    /// </summary>
    /// <param name="frameNumber">Sequential frame number.</param>
    /// <param name="samples">Performance samples collected during the frame.</param>
    /// <param name="frameStartTimestamp">Frame start timestamp (Stopwatch ticks).</param>
    /// <param name="frameEndTimestamp">Frame end timestamp (Stopwatch ticks).</param>
    public FrameProfile(
        int frameNumber,
        IEnumerable<PerformanceSample> samples,
        long frameStartTimestamp,
        long frameEndTimestamp)
    {
        FrameNumber = frameNumber;
        _samples = new List<PerformanceSample>(samples);
        _frameStartTimestamp = frameStartTimestamp;
        _frameEndTimestamp = frameEndTimestamp;

        // Calculate total frame time from timestamps
        long elapsedTicks = frameEndTimestamp - frameStartTimestamp;
        TotalFrameTimeMs = (elapsedTicks * 1000.0) / Stopwatch.Frequency;
    }

    /// <summary>
    /// Gets all samples with the specified label.
    /// </summary>
    /// <param name="label">Label to filter by.</param>
    /// <returns>Collection of samples matching the label.</returns>
    public IEnumerable<PerformanceSample> GetSamplesByLabel(string label)
    {
        return _samples.Where(s => s.Label == label);
    }

    /// <summary>
    /// Gets the total time spent in operations with the specified label.
    /// </summary>
    /// <param name="label">Label to sum time for.</param>
    /// <returns>Total elapsed time in milliseconds.</returns>
    public double GetTotalTimeForLabel(string label)
    {
        return _samples.Where(s => s.Label == label).Sum(s => s.ElapsedMs);
    }

    /// <summary>
    /// Gets all unique labels present in this frame's samples.
    /// </summary>
    /// <returns>Collection of unique labels.</returns>
    public IEnumerable<string> GetUniqueLabels()
    {
        return _samples.Select(s => s.Label).Distinct();
    }

    /// <summary>
    /// Gets a breakdown of time spent per label.
    /// </summary>
    /// <returns>Dictionary mapping labels to total elapsed time in milliseconds.</returns>
    public Dictionary<string, double> GetTimeBreakdown()
    {
        return _samples
            .GroupBy(s => s.Label)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.ElapsedMs));
    }

    public override string ToString()
    {
        return $"Frame {FrameNumber}: {TotalFrameTimeMs:F2}ms ({FPS:F1} FPS) - {_samples.Count} samples";
    }
}
