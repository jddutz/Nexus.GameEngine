using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Components.Lookups;

namespace Tests.GameEngine.Components.Lookups;

public class SiblingLookupTests
{
    [Fact]
    public void Resolve_ShouldFindSibling()
    {
        // Arrange
        var parent = new ContainerComponent();
        var target = new TargetComponent();
        var sibling = new SiblingComponent();
        
        parent.AddChild(target);
        parent.AddChild(sibling);
        
        var lookup = new SiblingLookup<SiblingComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Same(sibling, result);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenNoParent()
    {
        // Arrange
        var target = new TargetComponent();
        var lookup = new SiblingLookup<SiblingComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenNoMatchingSibling()
    {
        // Arrange
        var parent = new ContainerComponent();
        var target = new TargetComponent();
        var other = new OtherComponent();
        
        parent.AddChild(target);
        parent.AddChild(other);
        
        var lookup = new SiblingLookup<SiblingComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ShouldNotReturnSelf()
    {
        // Arrange
        var parent = new ContainerComponent();
        var target = new SiblingComponent(); // Target is also of type SiblingComponent
        
        parent.AddChild(target);
        
        var lookup = new SiblingLookup<SiblingComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Null(result); // Should not find itself
    }

    private class ContainerComponent : Component { }
    private class TargetComponent : Component { }
    private class SiblingComponent : Component { }
    private class OtherComponent : Component { }
}
