using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;

namespace TestApp.TestComponents;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public class RenderableTest()
    : RuntimeComponent, IDrawable, ITestComponent
{
    public uint RenderPriority { get; set; } = 0;
    public int FramesRendered { get; private set; } = 0;
    public int FrameCount { get; set; } = 1;
    
    protected override void OnUpdate(double deltaTime)
    {
        if (FramesRendered >= FrameCount)
        {
            Deactivate();
        }
    }

    public IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        FramesRendered++;
        return [];
    }

    public virtual IEnumerable<TestResult> GetTestResults()
    {
        bool passed = FramesRendered > 0;

        yield return new TestResult()
        {
            TestName = "GetDrawCommands() should be called at least once",
            ExpectedResult = "FramesRendered > 0",
            ActualResult = $"FramesRendered: {FramesRendered}",
            Passed = passed
        };
    }
}