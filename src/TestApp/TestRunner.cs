using Nexus.GameEngine;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Runtime;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Reflection;
using TestApp.Tests;

namespace TestApp;

/// <summary>
/// RuntimeComponent that orchestrates the discovery and execution of integration tests.
/// Executes tests during Update (Setup) and PostRender middleware (Evaluate) phases.
/// </summary>
/// <summary>
/// TestRunner is a RuntimeComponent that discovers and executes integration tests.
/// It manages test lifecycle, result output, and application exit based on test outcomes.
/// </summary>
public partial class TestRunner : RuntimeComponent
{
    public TestRunner(IWindowService windowService, IRenderer renderer) : base()
    {
        this.window = windowService.GetWindow();
        this.renderer = renderer;
    }

    private readonly IWindow window;
    private readonly IRenderer renderer;

    private readonly Stopwatch stopwatch = new();
    private int frameCount = 0;
    private int framesRendered = 0;
    private ITestComponent? currentTest = null;
    private readonly Queue<ITestComponent> tests = new();

    protected override void OnActivate()
    {
        renderer.AfterRendering += OnRenderComplete;
        stopwatch.Start();
    }

    /// <summary>
    /// Override default activation behavior to register test children instead of activating them.
    /// Tests will be activated one at a time in OnUpdate().
    /// </summary>
    public override void ActivateChildren()
    {
        // Register test components instead of activating them
        // Enqueue so we process them in order
        tests.Clear();

        // Only get immediate children (recursive=false is default, but being explicit)
        foreach(var child in GetChildren<ITestComponent>(recursive: false))
        {
            tests.Enqueue(child);
            Log.Debug($"Test component registered: {child.GetType().Name}");
        }

        if (tests.Count == 0)
        {
            Log.Debug($"No test components were registered");
        }
    }

    private void OnRenderComplete(object? sender, RenderEventArgs e)
    {
        framesRendered++;
    }

    /// <summary>
    /// Updates the TestRunner each frame. Activates test components one at a time.
    /// Closes the window and outputs results when all tests are complete.
    /// </summary>
    /// <param name="deltaTime">Elapsed time since last update.</param>
    protected override void OnUpdate(double deltaTime)
    {
        frameCount++;

        if (currentTest != null && currentTest.IsActive()) 
        {
            Log.Debug($"Update {frameCount}, current test: {currentTest.Name ?? "null"}");
            return;
        }
        
        // If we're done, deactivate and exit
        if (tests.Count == 0)
        {
            Log.Info($"Update {frameCount}, all tests complete - deactivating");
            Deactivate();
            return; 
        }

        // Dequeue next test if current test is complete or we don't have one
        currentTest = tests.Dequeue();
        Log.Debug($"Update {frameCount}, starting test: {currentTest.Name ?? "null"}");
        currentTest.Activate();
    }

    protected override void OnDeactivate()
    {
        stopwatch.Stop();
        
        OutputTestResults();

        try
        {
            window.Close();
        }
        catch
        {
            Environment.Exit(Environment.ExitCode);
        }
    }

    /// <summary>
    /// Outputs the results of all executed tests to the logger, sets the exit code, and closes the application window.
    /// </summary>
    private void OutputTestResults()
    {
        var passed = new List<TestResult>();
        var failed = new List<TestResult>();

        var failedTestsSummary = "\n==== Failed Tests ====";

        var testComponents = GetChildren<ITestComponent>().ToList();

        foreach (var test in testComponents)
        {
            if (test is null) continue;

            var results = test.GetTestResults();

            foreach (var result in results)
            {
                // Collect pass/fail counts. Avoid noisy per-result logging here so test harness
                // output remains concise; we will emit a compact summary below.
                if (result.Passed)
                {
                    passed.Add(result);
                }
                else
                {
                    failed.Add(result);
                    var testName = test.Name ?? test.GetType().Name;
                    failedTestsSummary += $"\n{testName}: Expected {result.ExpectedResult}, Actual {result.ActualResult}";
                }
            }
        }

        var resultsSummary = $"Test run complete!\n"
            + $"\n====== SUMMARY ======\n"
            + $"Test Components Discovered: {testComponents.Count}\n"
            + $"Number of Test Results: {passed.Count + failed.Count}\n"
            + $"Total Time: {stopwatch.ElapsedMilliseconds}ms\n"
            + $"Updates: {frameCount}\n"            
            + $"Rendered: {framesRendered}\n"
            + $"Avg FPS: {framesRendered / stopwatch.Elapsed.TotalSeconds:F0}\n"
            + $"Passed: {passed.Count}\n"
            + $"Failed: {failed.Count}\n"
            + $"Overall Result: {(passed.Count > 0 && failed.Count == 0 ? "[PASS]" : "[FAIL]")}\n";

        if (failed.Count > 0)
            resultsSummary += failedTestsSummary + "\n";

    // Emit concise summary information to the log (Test Explorer will capture this output).
    Nexus.GameEngine.Log.Info(resultsSummary);

        // Set exit code
        Environment.ExitCode = failed.Count == 0 ? 0 : 1;
    }
}
