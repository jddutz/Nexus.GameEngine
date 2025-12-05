using System.Diagnostics;

namespace Nexus.GameEngine.Performance;

/// <summary>
/// Implementation of performance profiling service.
/// Collects timing samples, manages frame profiles, and generates performance reports.
/// </summary>
public class Profiler : IProfiler
{
    private const int MaxFrameHistory = 1000; // Support 1000+ frames per FR-008
    private const int MaxSamplesPerFrame = 100;

    private readonly List<PerformanceSample> _currentFrameSamples = new();
    private readonly List<FrameProfile> _frameHistory = new();
    
    private bool _isEnabled;
    private int _currentFrameNumber;
    private long _frameStartTimestamp;

    /// <inheritdoc/>
    public bool IsEnabled => _isEnabled;

    /// <inheritdoc/>
    public void Enable()
    {
        _isEnabled = true;
    }

    /// <inheritdoc/>
    public void Disable()
    {
        _isEnabled = false;
    }

    /// <inheritdoc/>
    public void RecordSample(string label, double elapsedMs)
    {
        if (!_isEnabled)
            return;

        long timestamp = Stopwatch.GetTimestamp();
        var sample = new PerformanceSample(label, elapsedMs, timestamp);
        
        lock (_currentFrameSamples)
        {
            // Prevent unbounded growth if EndFrame is not called
            if (_currentFrameSamples.Count < MaxSamplesPerFrame)
            {
                _currentFrameSamples.Add(sample);
            }
        }
    }

    /// <inheritdoc/>
    public void BeginFrame()
    {
        if (!_isEnabled)
            return;

        _frameStartTimestamp = Stopwatch.GetTimestamp();
        
        lock (_currentFrameSamples)
        {
            _currentFrameSamples.Clear();
        }
    }

    /// <inheritdoc/>
    public FrameProfile EndFrame()
    {
        long frameEndTimestamp = Stopwatch.GetTimestamp();

        FrameProfile profile;
        lock (_currentFrameSamples)
        {
            // Create snapshot of current frame samples
            var samples = new List<PerformanceSample>(_currentFrameSamples);
            
            profile = new FrameProfile(
                _currentFrameNumber++,
                samples,
                _frameStartTimestamp,
                frameEndTimestamp
            );
        }

        lock (_frameHistory)
        {
            _frameHistory.Add(profile);

            // Limit history size to prevent unbounded memory growth
            if (_frameHistory.Count > MaxFrameHistory)
            {
                _frameHistory.RemoveAt(0);
            }
        }

        return profile;
    }

    /// <inheritdoc/>
    public PerformanceReport GenerateReport(int frameCount)
    {
        lock (_frameHistory)
        {
            // Get the most recent N frames
            var framesToAnalyze = _frameHistory
                .Skip(Math.Max(0, _frameHistory.Count - frameCount))
                .ToList();

            return new PerformanceReport(framesToAnalyze);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_currentFrameSamples)
        {
            _currentFrameSamples.Clear();
        }

        lock (_frameHistory)
        {
            _frameHistory.Clear();
        }

        _currentFrameNumber = 0;
    }
}
