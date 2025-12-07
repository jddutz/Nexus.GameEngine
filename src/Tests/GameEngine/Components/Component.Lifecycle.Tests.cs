using Xunit;
using Nexus.GameEngine.Components;

namespace Tests.GameEngine.Components;

public class ComponentLifecycleTests
{
    private class TestComponent : Component
    {
        public bool OnActivateCalled { get; private set; }
        public bool OnDeactivateCalled { get; private set; }
        public bool OnUpdateCalled { get; private set; }

        protected override void OnActivate()
        {
            base.OnActivate();
            OnActivateCalled = true;
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            OnDeactivateCalled = true;
        }

        protected override void OnUpdate(double deltaTime)
        {
            base.OnUpdate(deltaTime);
            OnUpdateCalled = true;
        }
    }

    [Fact]
    public void Activate_SetsActiveAndCallsOnActivate()
    {
        // Arrange
        var component = new TestComponent();
        // Must be loaded to be active
        component.IsLoaded = true; 

        // Act
        component.Activate();

        // Assert
        Assert.True(component.IsActive());
        Assert.True(component.OnActivateCalled);
    }

    [Fact]
    public void Deactivate_ClearsActiveAndCallsOnDeactivate()
    {
        // Arrange
        var component = new TestComponent();
        component.IsLoaded = true;
        component.Activate();

        // Act
        component.Deactivate();

        // Assert
        Assert.False(component.IsActive());
        Assert.True(component.OnDeactivateCalled);
    }

    [Fact]
    public void Update_CallsOnUpdate()
    {
        // Arrange
        var component = new TestComponent();
        component.IsLoaded = true;
        component.Activate();
        
        // Act
        component.Update(0.1);

        // Assert
        Assert.True(component.OnUpdateCalled);
    }
}
