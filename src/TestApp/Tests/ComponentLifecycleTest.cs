using Nexus.GameEngine;
using Nexus.GameEngine.Runtime;

namespace TestApp.Tests;

/// <summary>
/// Tests that all RuntimeComponent lifecycle methods are called in the correct order.
/// </summary>
public partial class ComponentLifecycleTest(IWindowService windowService) 
    : TestComponent(windowService), ITestComponent
{    
    // Track the order of calls
    private readonly List<string> callOrder = [];

    protected override void OnActivate()
    {
        base.OnActivate();
        callOrder.Add("OnActivate");
        Log.Info($"OnActivate called");
    }

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);
        callOrder.Add("OnUpdate");
        Log.Info($"OnUpdate called (Frame {Updates}/{FrameCount})");
        
        if (Updates >= FrameCount)
        {
            Log.Info($"Test complete, deactivating");
            Deactivate();
        }
    }

    protected override void OnDeactivate()
    {
        callOrder.Add("OnDeactivate");
        Log.Info($"OnDeactivate called");
        base.OnDeactivate();
    }

    public override IEnumerable<TestResult> GetTestResults()
    {
        yield return new()
        {
            ExpectedResult = "OnActivate,OnUpdate,OnDeactivate",
            ActualResult = string.Join(",", callOrder),
            Passed = ValidateCallOrder()
        };
    }

    private bool ValidateCallOrder()
    {
        // Full lifecycle with configuration
        if (callOrder.Count < 3) return false;
        
        return callOrder[0] == "OnActivate" &&
                callOrder[1] == "OnUpdate" &&
                callOrder[2] == "OnDeactivate";
    }
}
