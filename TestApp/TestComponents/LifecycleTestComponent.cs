using Nexus.GameEngine.Components;

namespace TestApp.TestComponents;

/// <summary>
/// Tests that all RuntimeComponent lifecycle methods are called in the correct order.
/// </summary>
public class LifecycleTestComponent : RuntimeComponent, ITestComponent
{
    // Track which lifecycle methods were called
    private bool _onConfigureCalled = false;
    private bool _onActivateCalled = false;
    private bool _onUpdateCalled = false;
    private bool _onDeactivateCalled = false;
    
    // Track the order of calls
    private readonly List<string> _callOrder = new();
    
    public int FrameCount { get; set; } = 5;
    private int _updateCount = 0;

    protected override void OnConfigure(IComponentTemplate componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        _onConfigureCalled = true;
        _callOrder.Add("OnConfigure");
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        _onActivateCalled = true;
        _callOrder.Add("OnActivate");
    }

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);
        
        if (!_onUpdateCalled)
        {
            _onUpdateCalled = true;
            _callOrder.Add("OnUpdate");
        }
        
        _updateCount++;
        
        if (_updateCount >= FrameCount)
        {
            Deactivate();
        }
    }

    protected override void OnDeactivate()
    {
        _onDeactivateCalled = true;
        _callOrder.Add("OnDeactivate");
        base.OnDeactivate();
    }

    public IEnumerable<TestResult> GetTestResults()
    {
        // Note: OnConfigure is only called when using CreateChild(template), not CreateChild(type)
        // TestRunner uses CreateChild(type), so OnConfigure won't be called
        
        yield return new TestResult
        {
            TestName = "OnActivate should be called",
            Passed = _onActivateCalled,
            ErrorMessage = _onActivateCalled ? "" : "OnActivate was never called"
        };

        yield return new TestResult
        {
            TestName = "OnUpdate should be called",
            Passed = _onUpdateCalled,
            ErrorMessage = _onUpdateCalled ? "" : "OnUpdate was never called"
        };

        yield return new TestResult
        {
            TestName = "OnDeactivate should be called",
            Passed = _onDeactivateCalled,
            ErrorMessage = _onDeactivateCalled ? "" : "OnDeactivate was never called"
        };

        yield return new TestResult
        {
            TestName = "Lifecycle methods should be called in correct order",
            Passed = ValidateCallOrder(),
            ErrorMessage = ValidateCallOrder() ? "" : $"Incorrect call order: {string.Join(" → ", _callOrder)}"
        };

        yield return new TestResult
        {
            TestName = "Component should update for expected number of frames",
            Passed = _updateCount >= FrameCount,
            ErrorMessage = _updateCount >= FrameCount 
                ? "" 
                : $"Expected {FrameCount} updates, got {_updateCount}"
        };
    }

    private bool ValidateCallOrder()
    {
        // Expected order without Configure: OnActivate → OnUpdate → OnDeactivate
        // Expected order with Configure: OnConfigure → OnActivate → OnUpdate → OnDeactivate
        
        if (_onConfigureCalled)
        {
            // Full lifecycle with configuration
            if (_callOrder.Count < 4) return false;
            
            return _callOrder[0] == "OnConfigure" &&
                   _callOrder[1] == "OnActivate" &&
                   _callOrder[2] == "OnUpdate" &&
                   _callOrder[3] == "OnDeactivate";
        }
        else
        {
            // Lifecycle without configuration (CreateChild(Type) path)
            if (_callOrder.Count < 3) return false;
            
            return _callOrder[0] == "OnActivate" &&
                   _callOrder[1] == "OnUpdate" &&
                   _callOrder[2] == "OnDeactivate";
        }
    }
}
