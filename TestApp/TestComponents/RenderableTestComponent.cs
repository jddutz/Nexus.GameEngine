using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;

namespace TestApp.TestComponents;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public class RenderableTestComponent : RuntimeComponent, IRenderable, ITestComponent
{
    public int FramesRendered { get; private set; } = 0;
    public int FrameCount { get; set; } = 1;

    public virtual uint RenderPriority => 0;

    protected override void OnUpdate(double deltaTime)
    {
        if (FramesRendered >= FrameCount)
        {
            Deactivate();
        }
    }

    public IEnumerable<ElementData> GetElements()
    {
        FramesRendered++;
        return [];
    }

    public virtual IEnumerable<TestResult> GetTestResults()
    {
        bool passed = FramesRendered > 0;

        var result = new TestResult()
        {
            TestName = "GetElements() should be called at least once",
            Passed = passed
        };

        if (!passed)
        {
            result.ErrorMessage = $"Expected FramesRendered > 0, Actual: {FramesRendered}";
        }

        yield return result;
    }
}