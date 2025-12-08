using Nexus.GameEngine;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Performance;
using Nexus.GameEngine.Runtime;

namespace TestApp.Tests;

/// <summary>
/// Integration test verifying bottleneck identification functionality.
/// Tests FR-003: Identify operations exceeding time thresholds.
/// Tests that PerformanceReport can identify top N slowest operations.
/// </summary>
public partial class BottleneckIdentificationTest(
    IProfiler profiler)
    : TestComponent, ITestComponent
{
    private const int FramesToProfile = 10;
    private bool _topSlowestIdentified = false;
    private string _slowestOperation = string.Empty;
    private bool _correctlySorted = false;
    private int _violationCount = 0;

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Frame 1: Enable profiling
        if (Updates == 1)
        {
            Log.Debug($"[BottleneckIdentificationTest] Frame {Updates}: Enabling profiler");
            profiler.Enable();
            profiler.Clear();
        }

        // Frames 2-11: Collect profiling data with varying workloads
        if (Updates >= 2 && Updates <= FramesToProfile + 1)
        {
            // Note: BeginFrame/EndFrame are managed by Application.Run()
            // We just create profiling scopes for artificial operations

            // Simulate operations with different performance characteristics
            using (new PerformanceScope("FastOperation", profiler))
            {
                Thread.Sleep(1); // ~1ms
            }

            using (new PerformanceScope("MediumOperation", profiler))
            {
                Thread.Sleep(5); // ~5ms
            }

            using (new PerformanceScope("SlowOperation", profiler))
            {
                Thread.Sleep(10); // ~10ms - this should be identified as a bottleneck
            }

            using (new PerformanceScope("VerySlowOperation", profiler))
            {
                Thread.Sleep(15); // ~15ms - this should be the top bottleneck
            }
        }

        // Frame 12: Validate bottleneck identification
        if (Updates == FramesToProfile + 2)
        {
            Log.Debug($"[BottleneckIdentificationTest] Frame {Updates}: Identifying bottlenecks");

            var report = profiler.GenerateReport(FramesToProfile);

            // Test 1: Get top 5 slowest operations
            var topSlowest = report.GetTopNSlowest(5);
            
            if (topSlowest.Count < 4)
            {
                Log.Error($"✗ Expected at least 4 operations, got {topSlowest.Count}");
                Deactivate();
                return;
            }
            Log.Debug($"✓ GetTopNSlowest returned {topSlowest.Count} operations");

            // Test 2: Verify VerySlowOperation is among the slowest operations
            // Note: The "Update" operation may be the slowest because it contains all component updates,
            // including the artificial operations created by this test. This is expected behavior.
            _slowestOperation = topSlowest[0].Label;
            var verySlowOpIndex = topSlowest.FindIndex(stat => stat.Label == "VerySlowOperation");
            
            if (verySlowOpIndex >= 0)
            {
                var verySlowOp = topSlowest[verySlowOpIndex];
                Log.Debug($"✓ VerySlowOperation identified in top bottlenecks: {verySlowOp.AverageMs:F2}ms avg (rank #{verySlowOpIndex + 1})");
                _topSlowestIdentified = true;
            }
            else
            {
                Log.Error($"✗ Expected 'VerySlowOperation' in top 5 slowest operations");
                Deactivate();
                return;
            }

            // Test 3: Verify operations are sorted by slowest first
            _correctlySorted = true;
            for (int i = 0; i < topSlowest.Count - 1; i++)
            {
                if (topSlowest[i].AverageMs < topSlowest[i + 1].AverageMs)
                {
                    Log.Error($"✗ Operations not sorted correctly: {topSlowest[i].Label} ({topSlowest[i].AverageMs}ms) < {topSlowest[i + 1].Label} ({topSlowest[i + 1].AverageMs}ms)");
                    _correctlySorted = false;
                    Deactivate();
                    return;
                }
            }
            Log.Debug("✓ Operations correctly sorted from slowest to fastest");

            // Test 4: Threshold violation detection (6.67ms target for 150 FPS)
            var violations = report.GetThresholdViolations(thresholdMs: 6.67);
            _violationCount = violations.Count;
            
            if (violations.Count == 0)
            {
                Log.Error("✗ Expected threshold violations for frames with slow operations");
                Deactivate();
                return;
            }
            Log.Debug($"✓ Detected {violations.Count} frames exceeding 6.67ms threshold");

            // Log all identified bottlenecks
            Log.Debug("[BottleneckIdentificationTest] Top 5 slowest operations:");
            for (int i = 0; i < Math.Min(5, topSlowest.Count); i++)
            {
                Log.Debug($"  {i + 1}. {topSlowest[i].Label}: {topSlowest[i].AverageMs:F3}ms average");
            }

            Deactivate();
        }
    }

    public override IEnumerable<TestResult> GetTestResults()
    {
        yield return new()
        {
            ExpectedResult = "VerySlowOperation among top 5 slowest, operations sorted correctly, threshold violations detected",
            ActualResult = $"VerySlowFound={_topSlowestIdentified}, ActualSlowest={_slowestOperation}, Sorted={_correctlySorted}, Violations={_violationCount}",
            Passed = _topSlowestIdentified && _correctlySorted && _violationCount > 0
        };
    }
}
