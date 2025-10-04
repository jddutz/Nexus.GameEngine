namespace TestApp;

/// <summary>
/// Represents the outcome of a single integration test execution, including status and error details.
/// </summary>
public class TestResult
{
    /// <summary>
    /// Gets or sets the name of the test.
    /// </summary>
    /// <value>
    /// The unique identifier or display name for the test case.
    /// </value>
    public string TestName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description of the test.
    /// </summary>
    /// <value>
    /// A brief summary of the test's purpose or scenario.
    /// </value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether the test passed.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the test succeeded; otherwise, <see langword="false"/>.
    /// </value>
    public bool Passed { get; set; } = false;
    /// <summary>
    /// Gets or sets the error message if the test failed.
    /// </summary>
    /// <value>
    /// The error message describing the reason for test failure, or <see langword="null"/> if the test passed.
    /// </value>
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Gets or sets the exception thrown during test execution, if any.
    /// </summary>
    /// <value>
    /// The <see cref="Exception"/> instance if an error occurred; otherwise, <see langword="null"/>.
    /// </value>
    public Exception? Exception { get; set; }
}