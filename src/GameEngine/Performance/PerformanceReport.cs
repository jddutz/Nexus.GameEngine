namespace Nexus.GameEngine.Performance;

/// <summary>
/// Aggregated performance analysis across multiple frames.
/// Provides bottleneck identification, threshold violation detection,
/// and statistical summaries of timing data.
/// </summary>
public class PerformanceReport
{
    private readonly List<FrameProfile> _frames;

    /// <summary>
    /// Gets the number of frames included in this report.
    /// </summary>
    public int FrameCount => _frames.Count;

    /// <summary>
    /// Gets the average frame time across all frames in milliseconds.
    /// </summary>
    public double AverageFrameTimeMs { get; }

    /// <summary>
    /// Gets the minimum frame time in milliseconds.
    /// </summary>
    public double MinFrameTimeMs { get; }

    /// <summary>
    /// Gets the maximum frame time in milliseconds.
    /// </summary>
    public double MaxFrameTimeMs { get; }

    /// <summary>
    /// Gets the average FPS across all frames.
    /// </summary>
    public double AverageFPS => AverageFrameTimeMs > 0 ? 1000.0 / AverageFrameTimeMs : 0;

    /// <summary>
    /// Gets the frame time variance (max - min).
    /// </summary>
    public double FrameTimeVarianceMs => MaxFrameTimeMs - MinFrameTimeMs;

    /// <summary>
    /// Creates a new performance report from frame profiles.
    /// </summary>
    /// <param name="frames">Collection of frame profiles to analyze.</param>
    public PerformanceReport(IEnumerable<FrameProfile> frames)
    {
        _frames = frames.ToList();

        if (_frames.Count == 0)
        {
            AverageFrameTimeMs = 0;
            MinFrameTimeMs = 0;
            MaxFrameTimeMs = 0;
            return;
        }

        AverageFrameTimeMs = _frames.Average(f => f.TotalFrameTimeMs);
        MinFrameTimeMs = _frames.Min(f => f.TotalFrameTimeMs);
        MaxFrameTimeMs = _frames.Max(f => f.TotalFrameTimeMs);
    }

    /// <summary>
    /// Gets the top N slowest operations across all frames.
    /// </summary>
    /// <param name="count">Number of operations to return.</param>
    /// <returns>Collection of labels and their average execution times, ordered by slowest first.</returns>
    public List<(string Label, double AverageMs)> GetTopNSlowest(int count)
    {
        if (_frames.Count == 0)
            return new List<(string, double)>();

        // Aggregate all samples by label
        var labelStats = _frames
            .SelectMany(f => f.Samples)
            .GroupBy(s => s.Label)
            .Select(g => new
            {
                Label = g.Key,
                AverageMs = g.Average(s => s.ElapsedMs)
            })
            .OrderByDescending(x => x.AverageMs)
            .Take(count);

        return labelStats.Select(x => (x.Label, x.AverageMs)).ToList();
    }

    /// <summary>
    /// Gets frames where total frame time exceeded the specified threshold.
    /// </summary>
    /// <param name="thresholdMs">Frame time threshold in milliseconds.</param>
    /// <returns>Collection of frames exceeding the threshold.</returns>
    public List<FrameProfile> GetThresholdViolations(double thresholdMs)
    {
        return _frames.Where(f => f.TotalFrameTimeMs > thresholdMs).ToList();
    }

    /// <summary>
    /// Gets the average time spent per label across all frames.
    /// </summary>
    /// <returns>Dictionary mapping labels to average execution time in milliseconds.</returns>
    public Dictionary<string, double> GetAverageTimePerLabel()
    {
        if (_frames.Count == 0)
            return new Dictionary<string, double>();

        var allSamples = _frames.SelectMany(f => f.Samples);

        return allSamples
            .GroupBy(s => s.Label)
            .ToDictionary(
                g => g.Key,
                g => g.Average(s => s.ElapsedMs)
            );
    }

    /// <summary>
    /// Gets the total time spent per label across all frames.
    /// </summary>
    /// <returns>Dictionary mapping labels to total execution time in milliseconds.</returns>
    public Dictionary<string, double> GetTotalTimePerLabel()
    {
        if (_frames.Count == 0)
            return new Dictionary<string, double>();

        var allSamples = _frames.SelectMany(f => f.Samples);

        return allSamples
            .GroupBy(s => s.Label)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(s => s.ElapsedMs)
            );
    }

    /// <summary>
    /// Gets statistical summary for a specific label.
    /// </summary>
    /// <param name="label">Label to analyze.</param>
    /// <returns>Statistical summary with min, max, average, and total time.</returns>
    public LabelStatistics? GetStatisticsForLabel(string label)
    {
        var samples = _frames.SelectMany(f => f.Samples).Where(s => s.Label == label).ToList();

        if (samples.Count == 0)
            return null;

        return new LabelStatistics
        {
            Label = label,
            SampleCount = samples.Count,
            AverageMs = samples.Average(s => s.ElapsedMs),
            MinMs = samples.Min(s => s.ElapsedMs),
            MaxMs = samples.Max(s => s.ElapsedMs),
            TotalMs = samples.Sum(s => s.ElapsedMs)
        };
    }

    public override string ToString()
    {
        return $"Performance Report: {FrameCount} frames, Avg: {AverageFrameTimeMs:F2}ms ({AverageFPS:F1} FPS), " +
               $"Min: {MinFrameTimeMs:F2}ms, Max: {MaxFrameTimeMs:F2}ms, Variance: {FrameTimeVarianceMs:F2}ms";
    }
}

/// <summary>
/// Statistical summary for a specific operation label.
/// </summary>
public class LabelStatistics
{
    public required string Label { get; init; }
    public int SampleCount { get; init; }
    public double AverageMs { get; init; }
    public double MinMs { get; init; }
    public double MaxMs { get; init; }
    public double TotalMs { get; init; }

    public override string ToString()
    {
        return $"{Label}: Avg={AverageMs:F3}ms, Min={MinMs:F3}ms, Max={MaxMs:F3}ms, Total={TotalMs:F2}ms ({SampleCount} samples)";
    }
}
