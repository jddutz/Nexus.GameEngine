using Nexus.GameEngine;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Performance;
using Nexus.GameEngine.Runtime;

namespace TestApp.Tests;

/// <summary>
/// Integration test verifying that profiling can be activated at runtime.
/// Tests FR-005: Runtime enable/disable without restart.
/// </summary>
public partial class ProfilingActivationTest(
    IProfiler profiler)
    : TestComponent, ITestComponent
{
    private bool _initialStateChecked = false;
    private bool _enabledStateChecked = false;
    private bool _disabledStateChecked = false;

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Frame 0: Check initial state (should be disabled by default)
        if (Updates == 1 && !_initialStateChecked)
        {
            Log.Debug($"[ProfilingActivationTest] Frame {Updates}: Checking initial profiler state");
            
            if (!profiler.IsEnabled)
            {
                Log.Debug("✓ Profiler is initially disabled (expected)");
                _initialStateChecked = true;
            }
            else
            {
                Log.Error("✗ Profiler should be initially disabled");
            }
        }

        // Frame 1: Enable profiling
        if (Updates == 2 && !_enabledStateChecked)
        {
            Log.Debug($"[ProfilingActivationTest] Frame {Updates}: Enabling profiler");
            profiler.Enable();

            if (profiler.IsEnabled)
            {
                Log.Debug("✓ Profiler.Enable() sets IsEnabled to true");
                _enabledStateChecked = true;
            }
            else
            {
                Log.Error("✗ Profiler.Enable() did not set IsEnabled to true");
            }
        }

        // Frame 2: Disable profiling
        if (Updates == 3 && !_disabledStateChecked)
        {
            Log.Debug($"[ProfilingActivationTest] Frame {Updates}: Disabling profiler");
            profiler.Disable();

            if (!profiler.IsEnabled)
            {
                Log.Debug("Profiler.Disable() sets IsEnabled to false");
                _disabledStateChecked = true;
            }
            else
            {
                Log.Error("Profiler.Disable() did not set IsEnabled to false");
            }
            
            // Test complete
            Deactivate();
        }
    }

    public override IEnumerable<TestResult> GetTestResults()
    {
        yield return new()
        {
            ExpectedResult = "Initial=disabled, Enable=enabled, Disable=disabled",
            ActualResult = $"Initial={(_initialStateChecked ? "disabled" : "FAILED")}, Enable={(_enabledStateChecked ? "enabled" : "FAILED")}, Disable={(_disabledStateChecked ? "disabled" : "FAILED")}",
            Passed = _initialStateChecked && _enabledStateChecked && _disabledStateChecked
        };
    }
}
