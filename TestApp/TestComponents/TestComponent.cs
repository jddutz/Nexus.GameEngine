using Nexus.GameEngine.Components;

namespace TestApp.TestComponents;

public partial class TestComponent : RuntimeComponent, ITestComponent
{
    public virtual Template[] Templates => [new()];

    [ComponentProperty]
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
