using Xunit;
using Nexus.GameEngine.Components;

namespace Tests.GameEngine.Components;

public class ComponentIdentityTests
{
    private class TestComponent : Component
    {
        public TestComponent()
        {
            _name = "TestComponent";
        }
    }

    [Fact]
    public void NewComponent_HasDefaultId_None()
    {
        // Arrange
        var component = new TestComponent();

        // Assert
        Assert.Equal(ComponentId.None, component.Id);
    }

    [Fact]
    public void Component_CanSetAndGetId()
    {
        // Arrange
        var component = new TestComponent();
        var id = new ComponentId(123);

        // Act
        component.Id = id;

        // Assert
        Assert.Equal(id, component.Id);
    }

    [Fact]
    public void Component_HasDefaultName_BasedOnType()
    {
        // Arrange
        var component = new TestComponent();

        // Assert
        Assert.Equal("TestComponent", component.Name);
    }

    [Fact]
    public void Component_CanSetName()
    {
        // Arrange
        var component = new TestComponent();
        var newName = "CustomName";

        // Act
        component.SetCurrentName(newName);

        // Assert
        Assert.Equal(newName, component.Name);
    }
}
