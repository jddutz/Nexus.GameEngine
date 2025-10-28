using Nexus.GameEngine.Components;

namespace TestApp.TestComponents;

public partial class TestComponent : RuntimeComponent, ITestComponent
{
    public new record Template : RuntimeComponent.Template
    {
        public int FrameCount { get; set; }
    }

    public virtual Template[] Templates => [new()];

    public int Updates { get; private set; } = 0;
    public int FrameCount { get; protected set; } = 1;

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        if (componentTemplate is Template template)
        {
            FrameCount = template.FrameCount;
        }
    }

    protected override void OnUpdate(double deltaTime)
    {
        // Deactivate one frame after FrameCount, 
        // to allow the last frame to be fully executed
        if (Updates > FrameCount) Deactivate();
        Updates++;
    }

    public virtual IEnumerable<TestResult> GetTestResults() => [];
}