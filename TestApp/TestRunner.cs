using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;
using System.Diagnostics;
using TestApp.TestComponents;

namespace TestApp;

/// <summary>
/// RuntimeComponent that orchestrates the discovery and execution of integration tests.
/// Executes tests during Update (Setup) and PostRender middleware (Evaluate) phases.
/// </summary>
/// <summary>
/// TestRunner is a RuntimeComponent that discovers and executes integration tests.
/// It manages test lifecycle, result output, and application exit based on test outcomes.
/// </summary>
public class TestRunner(
    IWindowService windowService)
    : RuntimeComponent
{
    private const string RESULTS_SUMMARY = "\n=== TEST RUN SUMMARY ===\n"
            + "Test Components Discovered: {TestComponents}\n"
            + "Number of Test Results: {TotalCount}\n"
            + "Passed: {PassCount}\n"
            + "Failed: {FailCount}\n"
            + "Total Time: {TotalTime:F2}ms\n"
            + "Overall Result: {OverallResult}";
    /// <summary>
    /// Configuration template for the TestRunner component.
    /// </summary>
    public new record Template : RuntimeComponent.Template { }

    private Queue<Type> _testComponents = new();

    int discovered = 0;
    private Stopwatch _stopwatch = new();

    /// <summary>
    /// Activates the TestRunner, discovers all ITestComponent types in the assembly, and starts the stopwatch.
    /// </summary>
    protected override void OnActivate()
    {
        Logger?.LogDebug("TestRunner activation started.");
        foreach (var type in GetType().Assembly.GetTypes())
        {
            if (type.IsAssignableTo(typeof(ITestComponent)) && !type.IsAbstract && !type.IsInterface)
            {
                _testComponents.Enqueue(type);
                Logger?.LogDebug("Discovered concrete test component: {TypeName}", type.FullName);
                discovered++;
            }
        }
        Logger?.LogInformation("Total test components discovered: {Count}", discovered);
        _stopwatch.Start();
        Logger?.LogDebug("TestRunner activation complete. Stopwatch started.");
    }

    /// <summary>
    /// Updates the TestRunner each frame. Instantiates test components and manages test execution flow.
    /// Closes the window and outputs results when all tests are complete.
    /// </summary>
    /// <param name="deltaTime">Elapsed time since last update.</param>
    protected override void OnUpdate(double deltaTime)
    {
        Logger?.LogTrace("TestRunner OnUpdate called. DeltaTime: {DeltaTime}", deltaTime);

        bool isTestStillRunning = false;
        foreach (var child in Children)
        {
            if (child.IsActive)
            {
                Logger?.LogTrace("Test component still active: {TypeName}", child.GetType().FullName);
                isTestStillRunning = true;
            }
        }

        if (isTestStillRunning) return;

        if (_testComponents.TryDequeue(out Type? t) && t is Type testComponentType)
        {
            Logger?.LogInformation("Instantiating test component: {TypeName}", testComponentType.FullName);
            var testComponent = CreateChild(testComponentType);

            if (testComponent != null)
                testComponent.Name = testComponentType.Name;
        }
        else
        {
            Logger?.LogInformation("All test components executed. Stopping stopwatch and outputting results.");
            _stopwatch.Stop();
            OutputTestResults();
            Logger?.LogInformation("Closing application window after test run.");
            windowService.GetOrCreateWindow().Close();
        }
    }

    /// <summary>
    /// Outputs the results of all executed tests to the logger, sets the exit code, and closes the application window.
    /// </summary>
    private void OutputTestResults()
    {
        var passed = new List<TestResult>();
        var failed = new List<TestResult>();

        var components = GetChildren<ITestComponent>().ToList();
        var results = components.SelectMany(t => t.GetTestResults());
        foreach (var result in results)
        {
            if (!string.IsNullOrEmpty(result.Description))
                Logger?.LogTrace("{Description}", result.Description);

            if (result.Passed)
            {
                passed.Add(result);
                Logger?.LogInformation("[Pass] {TestName}", result.TestName);
            }
            else
            {
                failed.Add(result);
                Logger?.LogWarning("[Fail] {TestName}", result.TestName);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    Logger?.LogWarning("{ErrorMessage}", result.ErrorMessage);

                if (result.Exception is Exception ex)
                {
                    Logger?.LogTrace("{Exception}\n{StackTrace}", ex.Message, ex.StackTrace);
                }
            }
        }

        Logger?.LogInformation(
            RESULTS_SUMMARY,
            components.Count,
            passed.Count + failed.Count,
            passed.Count,
            failed.Count,
            _stopwatch.ElapsedMilliseconds,
            passed.Count > 0 && failed.Count == 0 ? "[PASS]" : "[FAIL]"
            );

        if (failed.Count > 0)
        {
            Logger?.LogInformation("Failed Tests:");
            foreach (var test in failed)
            {
                Logger?.LogInformation("{TestName}: {ErrorMessage}", test.TestName, test.ErrorMessage ?? "Unknown error");
            }
        }

        // Set exit code
        Environment.ExitCode = failed.Count == 0 ? 0 : 1;

        // Exit the application by closing the window
        try
        {
            var window = windowService.GetOrCreateWindow();
            window.Close();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to close window, using Environment.Exit as fallback");
            Environment.Exit(Environment.ExitCode);
        }
    }

    /// <summary>
    /// Disposes the TestRunner and releases resources.
    /// </summary>
    protected override void OnDispose()
    {
        base.OnDispose();
    }
}