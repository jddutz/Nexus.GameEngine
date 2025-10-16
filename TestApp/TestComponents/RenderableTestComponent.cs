using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;

namespace TestApp.TestComponents;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public class RenderableTestComponent()
    : RenderableBase(), IRenderable, ITestComponent
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

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        FramesRendered++;
        return [];
    }

    public virtual IEnumerable<TestResult> GetTestResults()
    {
        bool passed = FramesRendered > 0;

        var result = new TestResult()
        {
            TestName = "GetDrawCommands() should be called at least once",
            Passed = passed
        };

        if (!passed)
        {
            result.ErrorMessage = $"Expected FramesRendered > 0, Actual: {FramesRendered}";
        }

        yield return result;
    }
}