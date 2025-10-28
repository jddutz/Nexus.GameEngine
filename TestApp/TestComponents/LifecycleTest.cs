using Nexus.GameEngine.Components;

namespace TestApp.TestComponents;

/// <summary>
/// Tests that all RuntimeComponent lifecycle methods are called in the correct order.
/// </summary>
public partial class LifecycleTest : TestComponent, ITestComponent
{
    public new record Template : TestComponent.Template { }

    [Test("Lifecycle methods should be called in correct order")]
    public static readonly Template test = new();
    
    // Track the order of calls
    private readonly List<string> callOrder = [];

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        callOrder.Add("OnConfigure");
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        callOrder.Add("OnActivate");
    }

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);
        callOrder.Add("OnUpdate");
        
        if (Updates >= FrameCount)
        {
            Deactivate();
        }
    }

    protected override void OnDeactivate()
    {
        callOrder.Add("OnDeactivate");
        base.OnDeactivate();
    }

    public override IEnumerable<TestResult> GetTestResults()
    {
        yield return new()
        {
            ExpectedResult = "OnConfigure,OnActivate,OnUpdate,OnDeactivate",
            ActualResult = string.Join(",", callOrder),
            Passed = ValidateCallOrder()
        };
    }

    private bool ValidateCallOrder()
    {
        // Full lifecycle with configuration
        if (callOrder.Count < 4) return false;
        
        return callOrder[0] == "OnConfigure" &&
                callOrder[1] == "OnActivate" &&
                callOrder[2] == "OnUpdate" &&
                callOrder[^1] == "OnDeactivate";
    }
}
