using TestApp.TestComponents;

namespace TestApp;

/// <summary>
/// Defines a test to be executed by TestRunner.
/// </summary>
public class ComponentTest(TestComponent.Template template)
{
    /// <summary>
    /// Name of the test component to be tested
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Test description, provided by the [Test] attribute
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Template used to instantiate the test
    /// </summary>
    public TestComponent.Template Template { get; set; } = template;

    /// <summary>
    /// Component created by TestRunner to run the test
    /// </summary>
    public ITestComponent? TestComponent { get; set; }
}