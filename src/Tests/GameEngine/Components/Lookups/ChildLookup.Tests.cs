using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Components.Lookups;

namespace Tests.GameEngine.Components.Lookups;

public class ChildLookupTests
{
    [Fact]
    public void Resolve_ShouldFindChild()
    {
        // Arrange
        var target = new TargetComponent();
        var child = new ChildComponent();
        
        target.AddChild(child);
        
        var lookup = new ChildLookup<ChildComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Same(child, result);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenNoChildren()
    {
        // Arrange
        var target = new TargetComponent();
        var lookup = new ChildLookup<ChildComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenNoMatchingChild()
    {
        // Arrange
        var target = new TargetComponent();
        var other = new OtherComponent();
        
        target.AddChild(other);
        
        var lookup = new ChildLookup<ChildComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Null(result);
    }

    private class TargetComponent : Component { }
    private class ChildComponent : Component { }
    private class OtherComponent : Component { }
}
