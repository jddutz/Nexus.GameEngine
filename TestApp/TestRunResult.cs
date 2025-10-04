namespace TestApp;

/// <summary>
/// Represents the overall result of a test run containing multiple tests.
/// Provides aggregated statistics and execution metrics for all tests executed in a single run.
/// </summary>
public class TestRunResult
{
    /// <summary>
    /// Gets or sets the collection of individual test results from this test run.
    /// </summary>
    /// <value>A list containing the results of all tests executed during this run.</value>
    public List<TestResult> TestResults { get; set; } = new();

    /// <summary>
    /// Gets or sets the total time taken to execute all tests in this run.
    /// </summary>
    /// <value>The cumulative execution time for the entire test run.</value>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// Gets the number of tests that passed in this run.
    /// </summary>
    /// <value>The count of tests with <see cref="TestResult.Passed"/> set to true.</value>
    public int PassedCount => TestResults.Count(r => r.Passed);

    /// <summary>
    /// Gets the number of tests that failed in this run.
    /// </summary>
    /// <value>The count of tests with <see cref="TestResult.Passed"/> set to false.</value>
    public int FailedCount => TestResults.Count(r => !r.Passed);

    /// <summary>
    /// Gets the total number of tests executed in this run.
    /// </summary>
    /// <value>The total count of all test results, both passed and failed.</value>
    public int TotalCount => TestResults.Count;

    /// <summary>
    /// Gets a value indicating whether all tests in this run passed.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if no tests failed (FailedCount is 0); otherwise, <see langword="false"/>.
    /// </value>
    public bool AllPassed => FailedCount == 0;
}