namespace TestApp;

/// <summary>
/// Attribute that marks a test component to be ignored by the TestRunner autodiscovery system.
/// Requires a reason to be specified explaining why the test is being ignored.
/// </summary>
/// <param name="reason">The reason why this test component should be ignored.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TestRunnerIgnoreAttribute(string reason) : Attribute
{
    /// <summary>
    /// Gets the reason why this test component is being ignored.
    /// </summary>
    public string Reason { get; } = reason;
}
