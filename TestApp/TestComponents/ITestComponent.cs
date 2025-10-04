using Nexus.GameEngine.Components;

namespace TestApp.TestComponents;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public interface ITestComponent : IRuntimeComponent
{
    bool IsTestComplete { get; }
    IEnumerable<TestResult> GetTestResults();
}