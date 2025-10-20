using Nexus.GameEngine.Animation;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;

namespace TestApp.TestComponents;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public partial class RenderableTest()
    : RuntimeComponent, IDrawable, ITestComponent
{
    public uint RenderPriority { get; set; } = 0;
    public int FramesRendered { get; private set; } = 0;
    public int FrameCount { get; set; } = 1;
    
    /// <summary>
    /// Whether this component should be rendered.
    /// When false, GetDrawCommands will not be called and component is skipped during rendering.
    /// Generated property: IsVisible (read-only), SetIsVisible(...) method for updates.
    /// </summary>
    [ComponentProperty(Duration = AnimationDuration.Fast, Interpolation = InterpolationMode.Step)]
    private bool _isVisible = true;

    
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