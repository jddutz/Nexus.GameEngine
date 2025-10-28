namespace TestApp;

/// <summary>
/// Represents the outcome of a single integration test execution, including status and error details.
/// </summary>
public class TestResult
{
    /// <summary>
    /// Gets or sets the expected result of the test.
    /// </summary>
    public string ExpectedResult { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the actual result from the test.
    /// </summary>
    public string ActualResult { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the test passed.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the test succeeded; otherwise, <see langword="false"/>.
    /// </value>
    public bool Passed { get; set; } = false;

    public override string ToString() =>
        $"Expected {ExpectedResult} Actual {ActualResult} {(Passed ? "[PASS]" : "[FAIL]")}";
}