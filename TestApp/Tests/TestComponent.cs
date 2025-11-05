using Nexus.GameEngine.Components;

namespace TestApp.Tests;

public partial class TestComponent : RuntimeComponent, ITestComponent
{
    [ComponentProperty]
    [TemplateProperty]
    public int _frameCount = 1;
    public int Updates { get; private set; } = 0;

    protected override void OnUpdate(double deltaTime)
    {
        // Deactivate one frame after FrameCount, 
        // to allow the last frame to be fully executed
        if (Updates > FrameCount) Deactivate();
        Updates++;
    }

    public virtual IEnumerable<TestResult> GetTestResults() => [];
}
