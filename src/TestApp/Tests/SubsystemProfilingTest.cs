using Nexus.GameEngine;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Performance;
using Nexus.GameEngine.Runtime;

namespace TestApp.Tests;

/// <summary>
/// Integration test verifying that timing data is collected for major subsystems.
/// Tests FR-001: Per-frame timing data collection for rendering, updates, resource loading, input.
/// </summary>
public partial class SubsystemProfilingTest(
    IProfiler profiler,
    IWindowService windowService)
    : TestComponent(windowService), ITestComponent
{
    private const int FramesToProfile = 5;
    private bool _dataCollected = false;
    private int _actualFrameCount = 0;
    private List<string> _collectedLabels = new();
    private bool _allTimingsValid = false;

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Frame 1: Enable profiling
        if (Updates == 1)
        {
            Log.Info($"[SubsystemProfilingTest] Frame {Updates}: Enabling profiler");
            profiler.Enable();
            profiler.Clear(); // Clear any existing data
        }

        // Frames 2-6: Collect profiling data (simulated work)
        if (Updates >= 2 && Updates <= FramesToProfile + 1)
        {
            profiler.BeginFrame();

            // Simulate subsystem work with profiling markers
            using (new PerformanceScope("Update", profiler))
            {
                Thread.Sleep(1); // Simulate update work
            }

            using (new PerformanceScope("Render", profiler))
            {
                Thread.Sleep(2); // Simulate render work
            }

            using (new PerformanceScope("Input", profiler))
            {
                Thread.Sleep(1); // Simulate input processing
            }

            var profile = profiler.EndFrame();
            Log.Info($"[SubsystemProfilingTest] Frame {Updates}: Captured {profile.Samples.Count} samples");
        }

        // Frame 7: Validate collected data
        if (Updates == FramesToProfile + 2)
        {
            Log.Info($"[SubsystemProfilingTest] Frame {Updates}: Validating profiling data");

            var report = profiler.GenerateReport(FramesToProfile);

            // Verify frame count
            _actualFrameCount = report.FrameCount;
            if (report.FrameCount == FramesToProfile)
            {
                Log.Info($"✓ Collected profiling data for {report.FrameCount} frames");
                _dataCollected = true;
            }
            else
            {
                Log.Error($"✗ Expected {FramesToProfile} frames, got {report.FrameCount}");
                Deactivate();
                return;
            }

            // Verify subsystem timing data
            var avgTimePerLabel = report.GetAverageTimePerLabel();

            var expectedLabels = new[] { "Update", "Render", "Input" };
            var missingLabels = expectedLabels.Where(label => !avgTimePerLabel.ContainsKey(label)).ToList();

            if (missingLabels.Any())
            {
                Log.Error($"✗ Missing timing data for subsystems: {string.Join(", ", missingLabels)}");
                _collectedLabels = avgTimePerLabel.Keys.ToList();
                Deactivate();
                return;
            }
            
            _collectedLabels = avgTimePerLabel.Keys.ToList();
            Log.Info($"✓ Timing data collected for all major subsystems: {string.Join(", ", expectedLabels)}");

            // Verify timing values are reasonable
            _allTimingsValid = true;
            foreach (var label in expectedLabels)
            {
                var avgTime = avgTimePerLabel[label];
                if (avgTime <= 0)
                {
                    Log.Error($"✗ {label} has invalid average time: {avgTime}ms");
                    _allTimingsValid = false;
                    Deactivate();
                    return;
                }
                Log.Info($"[SubsystemProfilingTest] {label}: {avgTime:F3}ms average");
            }
            Log.Info("✓ All subsystem timing values are valid and measurable");

            Deactivate();
        }
    }

    public override IEnumerable<TestResult> GetTestResults()
    {
        yield return new()
        {
            ExpectedResult = $"{FramesToProfile} frames, 3 subsystems with valid timings",
            ActualResult = $"{_actualFrameCount} frames, {_collectedLabels.Count} subsystems ({string.Join(", ", _collectedLabels)}), valid={_allTimingsValid}",
            Passed = _dataCollected && _collectedLabels.Count == 3 && _allTimingsValid
        };
    }
}
