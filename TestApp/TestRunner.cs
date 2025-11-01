using Nexus.GameEngine;
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
        // Find all public static fields of type Template with a [Test] attribute
        var fields = typeof(TestComponent)
            .Assembly
            .GetTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .ToList();

        foreach (var field in fields)
        {
            if (!typeof(Template).IsAssignableFrom(field.FieldType))
                continue;

            var testAttr = field.GetCustomAttribute<TestAttribute>();
            if (testAttr == null)
                continue;

            var template = field.GetValue(null) as Template;
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

        if (Children.OfType<ITestComponent>().Where(c => c.IsActive()).Any()) return;

        if (currentTestIndex < tests.Count)
        {
            var test = tests[currentTestIndex++];

            if (test != null && test.Template != null)
            {
                var child = CreateChild(test.Template);
                if (child is ITestComponent testComponent)
                {
                    // Explicitly activate the test component
                    Log.Debug($"Activating test component {testComponent.GetType().Name}");
                    testComponent.Activate();
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

        var failedTestsSummary = "\n==== Failed Tests ====";

        foreach (var test in tests)
        {
            if (test.TestComponent is null) continue;

            var results = test.TestComponent.GetTestResults();

            foreach (var result in results)
            {
                Nexus.GameEngine.Log.Info(
                    $"[{(result.Passed ? "Pass" : "Fail")}]{test.TestName} {test.Description}: Expected {result.ExpectedResult}, Actual {result.ActualResult}"
                );

                if (result.Passed)
                {
                    passed.Add(result);
                }
                else
                {
                    failed.Add(result);
                    failedTestsSummary += $"\n{test.TestName} {test.Description}: Expected {result.ExpectedResult}, Actual {result.ActualResult}";
                }
            }
        }

        var resultsSummary = $"Test run complete!\n"
            + $"\n====== SUMMARY ======\n"
            + $"Test Components Discovered: {tests.Count}\n"
            + $"Number of Test Results: {passed.Count + failed.Count}\n"
            + $"Passed: {passed.Count}\n"
            + $"Failed: {failed.Count}\n"
            + $"Total Time: {stopwatch.ElapsedMilliseconds}ms\n"
            + $"Frames Rendered: {framesRendered}\n"
            + $"Avg FPS: {framesRendered / stopwatch.Elapsed.TotalSeconds:F0}\n"
            + $"Overall Result: {(passed.Count > 0 && failed.Count == 0 ? "[PASS]" : "[FAIL]")}\n";

        if (failed.Count > 0)
            resultsSummary += failedTestsSummary + "\n";

        Nexus.GameEngine.Log.Debug(resultsSummary);

        // Set exit code
        Environment.ExitCode = failed.Count == 0 ? 0 : 1;
    }
}
