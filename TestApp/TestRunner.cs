using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Reflection;
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
public partial class TestRunner : RuntimeComponent
{
    private readonly IWindow window;

    /// <summary>
    /// Configuration template for the TestRunner component.
    /// </summary>
    public new record Template : RuntimeComponent.Template { }

    private readonly List<ComponentTest> tests = [];
    private readonly Stopwatch stopwatch = new();
    private int framesRendered = 0;
    private int currentTestIndex = 0;

    public TestRunner(IWindowService windowService)
    {
        window = windowService.GetWindow();

    tests.AddRange(DiscoverTests());
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        stopwatch.Start();
    }

    private static IEnumerable<ComponentTest> DiscoverTests()
    {
        // Find all public static fields of type TestComponent.Template with a [Test] attribute
        var fields = typeof(TestComponent)
            .Assembly
            .GetTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .ToList();

        foreach (var field in fields)
        {
            if (!typeof(TestComponent.Template).IsAssignableFrom(field.FieldType))
                continue;

            var testAttr = field.GetCustomAttribute<TestAttribute>();
            if (testAttr == null)
                continue;

            var template = field.GetValue(null) as TestComponent.Template;
            if (template == null)
                continue;

            var name = !string.IsNullOrEmpty(template.Name) ? template.Name : field.DeclaringType?.Name ?? field.Name;
            var desc = testAttr.Description;

            yield return new ComponentTest(template)
            {
                TestName = name,
                Description = desc
            };
        }
    }

    /// <summary>
    /// Updates the TestRunner each frame. Instantiates test components and manages test execution flow.
    /// Closes the window and outputs results when all tests are complete.
    /// </summary>
    /// <param name="deltaTime">Elapsed time since last update.</param>
    protected override void OnUpdate(double deltaTime)
    {
        framesRendered++;

        if (Children.Where(c => c.IsActive()).Any()) return;

        if (currentTestIndex < tests.Count)
        {
            var test = tests[currentTestIndex++];
            
            if (test != null && test.Template != null)
            {
                var child = CreateChild(test.Template);
                if (child is ITestComponent testComponent)
                {
                    testComponent?.Activate();
                    test.TestComponent = testComponent;
                }
            }

            return;
        }

        Deactivate();
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

        foreach (var test in tests)
        {
            if (test.TestComponent is null) continue;

            var results = test.TestComponent.GetTestResults();

            foreach (var result in results)
            {
                if (!string.IsNullOrEmpty(test.TestName))
                    Logger?.LogTrace("{TestName}", test.TestName);

                if (!string.IsNullOrEmpty(test.Description))
                    Logger?.LogTrace("{Description}", test.Description);

                Logger?.LogInformation(
                    "{TestName} {Description}: {Output}",
                    test.TestName,
                    test.Description,
                    result
                );

                if (result.Passed)
                {
                    passed.Add(result);
                }
                else
                {
                    failed.Add(result);
                }
            }
        }

        const string RESULTS_SUMMARY = "\n=== TEST RUN SUMMARY ===\n"
            + "Test Components Discovered: {TestComponents}\n"
            + "Number of Test Results: {TotalCount}\n"
            + "Passed: {PassCount}\n"
            + "Failed: {FailCount}\n"
            + "Total Time: {TotalTime:F2}ms\n"
            + "Frames Rendered: {FramesRendered}\n"
            + "Avg FPS: {AverageFPS:F0}\n"
            + "Overall Result: {OverallResult}";

        Logger?.LogInformation(
            RESULTS_SUMMARY,
            tests.Count,
            passed.Count + failed.Count,
            passed.Count,
            failed.Count,
            stopwatch.ElapsedMilliseconds,
            framesRendered,
            framesRendered / stopwatch.Elapsed.TotalSeconds,
            passed.Count > 0 && failed.Count == 0 ? "[PASS]" : "[FAIL]"
            );

        // Set exit code
        Environment.ExitCode = failed.Count == 0 ? 0 : 1;
    }
}