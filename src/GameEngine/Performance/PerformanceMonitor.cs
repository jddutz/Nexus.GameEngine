using Nexus.GameEngine.Components;
using Nexus.GameEngine.Data.Binding;
using Nexus.GameEngine.GUI;

namespace Nexus.GameEngine.Performance;

/// <summary>
/// Component that monitors performance metrics and exposes them as bindable properties.
/// Acts as a facade over IProfiler service, making performance data available for data binding.
/// Does not handle rendering - rendering should be wired up separately via data binding.
/// </summary>
public partial class PerformanceMonitor : UserInterfaceElement
{
    private readonly IProfiler _profiler = new Profiler();
    
    [TemplateProperty]
    protected bool _enabled = true;

    [TemplateProperty]
    protected double _warningThresholdMs = 6.67; // 150 FPS target

    [TemplateProperty]
    protected int _averageFrameCount = 60;

    [TemplateProperty]
    protected double _updateIntervalSeconds = 0.5;

    // Exposed performance metrics as component properties
    [ComponentProperty]
    protected double _currentFps;

    [ComponentProperty]
    protected double _averageFps;

    [ComponentProperty]
    protected double _currentFrameTimeMs;

    [ComponentProperty]
    protected double _averageFrameTimeMs;

    [ComponentProperty]
    protected double _minFrameTimeMs;

    [ComponentProperty]
    protected double _maxFrameTimeMs;

    [ComponentProperty]
    protected bool _performanceWarning;

    [ComponentProperty]
    protected string _updateTimeMs = "0.00";

    [ComponentProperty]
    protected string _renderTimeMs = "0.00";

    [ComponentProperty]
    protected string _resourceLoadTimeMs = "0.00";

    [ComponentProperty]
    protected string _performanceSummary = string.Empty;

    private double _timeSinceLastUpdate;

    protected override void OnActivate()
    {
        base.OnActivate();
        
        if (_enabled)
        {
            _profiler.Enable();
        }
        
        _timeSinceLastUpdate = 0;
    }

    protected override void OnDeactivate()
    {
        if (_enabled)
        {
            _profiler.Disable();
        }
        
        base.OnDeactivate();
    }

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (!_enabled || !_profiler.IsEnabled)
            return;

        _timeSinceLastUpdate += deltaTime;

        // Update display at configured interval
        if (_timeSinceLastUpdate >= _updateIntervalSeconds)
        {
            UpdatePerformanceMetrics();
            _timeSinceLastUpdate = 0;
        }
    }

    private void UpdatePerformanceMetrics()
    {
        var report = _profiler.GenerateReport(_averageFrameCount);

        // Use report-level frame time statistics
        _currentFrameTimeMs = report.AverageFrameTimeMs; // Latest frame approximation
        _averageFrameTimeMs = report.AverageFrameTimeMs;
        _minFrameTimeMs = report.MinFrameTimeMs;
        _maxFrameTimeMs = report.MaxFrameTimeMs;

        // FPS = 1000 / frame_time_ms
        _currentFps = _currentFrameTimeMs > 0 ? 1000.0 / _currentFrameTimeMs : 0;
        _averageFps = report.AverageFPS;

        // Get subsystem timings
        var updateStats = report.GetStatisticsForLabel("Update");
        _updateTimeMs = updateStats != null ? $"{updateStats.AverageMs:F2}" : "0.00";

        var renderStats = report.GetStatisticsForLabel("Render");
        _renderTimeMs = renderStats != null ? $"{renderStats.AverageMs:F2}" : "0.00";

        var resourceStats = report.GetStatisticsForLabel("ResourceLoad");
        _resourceLoadTimeMs = resourceStats != null ? $"{resourceStats.AverageMs:F2}" : "0.00";

        // Check for performance warnings
        _performanceWarning = _averageFrameTimeMs > _warningThresholdMs;

        // Build summary string
        BuildPerformanceSummary();
    }

    private void BuildPerformanceSummary()
    {
        var lines = new List<string>
        {
            $"FPS: {_currentFps:F1} (avg: {_averageFps:F1})",
            $"Frame: {_currentFrameTimeMs:F2}ms (avg: {_averageFrameTimeMs:F2}ms)",
            $"Update: {_updateTimeMs}ms",
            $"Render: {_renderTimeMs}ms"
        };

        if (_performanceWarning)
        {
            lines.Add($"⚠ Performance below target ({_warningThresholdMs:F2}ms)");
        }

        _performanceSummary = string.Join("\n", lines);
    }
}
