using Nexus.GameEngine.Components;

namespace TestApp.TestComponents;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// Test components should deactivate themselves when testing is complete (IsActive = false).
/// TestRunner monitors IsActive to determine when tests are finished.
/// </summary>
public interface ITestComponent : IRuntimeComponent
{
    IEnumerable<TestResult> GetTestResults();
}
