using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Silk.NET.Maths;

namespace TestApp.Tests;

public partial class TestComponent : RuntimeComponent, ITestComponent
{
    [ComponentProperty]
    [TemplateProperty]
    public int _frameCount = 1;
    public int Updates { get; private set; } = 0;

    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Set size constraints for all UI element children
        // This ensures UI elements get properly sized and positioned
        // Note: Window size is 1280x720 in test environment
        var constraints = new Rectangle<float>(-640f, -360f, 1280f, 720f);
        foreach (var child in Children)
        {
            if (child is IUserInterfaceElement uiElement)
            {
                uiElement.UpdateLayout(constraints);
                
                // Immediately apply deferred updates so Frame 0 renders with correct size/position
                // Normally ContentManager.OnUpdate() applies these at the frame boundary,
                // but since tests activate mid-frame, we need to apply them immediately
                if (child is Entity entity)
                {
                    entity.ApplyUpdates(0.0);
                }
            }
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
