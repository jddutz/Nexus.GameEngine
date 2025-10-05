using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Silk.NET.Maths;

namespace TestApp.TestComponents;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public class RenderableTestComponent : RuntimeComponent, IRenderable, ITestComponent
{
    public int FramesRendered { get; private set; } = 0;
    public int FrameCount { get; set; } = 1;

    public virtual bool IsVisible { get; private set; } = true;

    public virtual uint RenderPriority => 0;

    public virtual Box3D<float> BoundingBox => new();

    public virtual uint RenderPassFlags => 0;

    public virtual void SetVisible(bool visible)
    {
        IsVisible = visible;
    }

    protected override void OnUpdate(double deltaTime)
    {
        if (FramesRendered >= FrameCount)
        {
            IsTestComplete = true;
            Deactivate();
        }
    }

    public virtual IEnumerable<RenderData> OnRender(IViewport viewport, double deltaTime)
    {
        FramesRendered++;
        return [];
    }

    public virtual bool IsTestComplete { get; protected set; } = false;

    public virtual IEnumerable<TestResult> GetTestResults()
    {
        var result = new TestResult()
        {
            TestName = "OnRender(IViewport, double) should be called at least once",
            Passed = FramesRendered > 0
        };

        if (FramesRendered == 0)
        {
            result.ErrorMessage = $"Expected FramesRendered > 0, Actual: {FramesRendered}";
        }

        yield return result;
    }
}