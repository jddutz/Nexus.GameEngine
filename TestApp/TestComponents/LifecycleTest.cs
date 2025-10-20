using Nexus.GameEngine.Components;

namespace TestApp.TestComponents;

/// <summary>
/// Tests that all RuntimeComponent lifecycle methods are called in the correct order.
/// </summary>
public partial class LifecycleTest : RuntimeComponent, ITestComponent
{
    // Track which lifecycle methods were called
    private int _onConfigureCalled = 0;
    private int _onActivateCalled = 0;
    private int _onUpdateCalled = 0;
    private int _onDeactivateCalled = 0;
    
    // Track the order of calls
    private readonly List<string> _callOrder = new();
    
    public int FrameCount { get; set; } = 5;

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        _onConfigureCalled++;
        _callOrder.Add("OnConfigure");
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        _onActivateCalled++;
        _callOrder.Add("OnActivate");
    }

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        _onUpdateCalled++;
        _callOrder.Add("OnUpdate");
        
        if (_onUpdateCalled >= FrameCount)
        {
            Deactivate();
        }
    }

    protected override void OnDeactivate()
    {
        _onDeactivateCalled++;
        _callOrder.Add("OnDeactivate");
        base.OnDeactivate();
    }

    public IEnumerable<TestResult> GetTestResults()
    {
        yield return new TestResult
        {
            TestName = "OnActivate should be called at least once",
            ExpectedResult = "> 0",
            ActualResult = _onActivateCalled.ToString(),
            Passed = _onActivateCalled > 0
        };

        yield return new TestResult
        {
            TestName = $"OnUpdate should be called at least {FrameCount} times",
            ExpectedResult = $">= {FrameCount}",
            ActualResult = _onUpdateCalled.ToString(),
            Passed = _onUpdateCalled >= FrameCount
        };

        yield return new TestResult
        {
            TestName = "OnDeactivate should be called",
            ExpectedResult = "> 0",
            ActualResult = _onDeactivateCalled.ToString(),
            Passed = _onDeactivateCalled > 0
        };

        yield return new TestResult
        {
            TestName = "Lifecycle methods should be called in correct order",
            ExpectedResult = "OnConfigure,OnActivate,OnUpdate (x5),OnDeactivate",
            ActualResult = string.Join(",", _callOrder),
            Passed = ValidateCallOrder()
        };
    }

    private bool ValidateCallOrder()
    {
        // Expected order without Configure: OnActivate → OnUpdate → OnDeactivate
        // Expected order with Configure: OnConfigure → OnActivate → OnUpdate → OnDeactivate
        
        if (_onConfigureCalled > 0)
        {
            // Full lifecycle with configuration
            if (_callOrder.Count < 4) return false;
            
            return _callOrder[0] == "OnConfigure" &&
                   _callOrder[1] == "OnActivate" &&
                   _callOrder[2] == "OnUpdate" &&
                   _callOrder[^1] == "OnDeactivate";
        }
        else
        {
            // Lifecycle without configuration (CreateChild(Type) path)
            if (_callOrder.Count < 3) return false;
            
            return _callOrder[0] == "OnActivate" &&
                   _callOrder[1] == "OnUpdate" &&
                   _callOrder[^1] == "OnDeactivate";
        }
    }
}
