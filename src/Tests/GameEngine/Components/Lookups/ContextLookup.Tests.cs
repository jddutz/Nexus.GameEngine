using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Components.Lookups;

namespace Tests.GameEngine.Components.Lookups;

public class ContextLookupTests
{
    [Fact]
    public void Resolve_ShouldFindParentAsContext()
    {
        // Arrange
        var context = new ContextComponent();
        var target = new TargetComponent();
        
        context.AddChild(target);
        
        var lookup = new ContextLookup<ContextComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Same(context, result);
    }

    [Fact]
    public void Resolve_ShouldFindAncestorAsContext()
    {
        // Arrange
        var context = new ContextComponent();
        var middle = new ContainerComponent();
        var target = new TargetComponent();
        
        context.AddChild(middle);
        middle.AddChild(target);
        
        var lookup = new ContextLookup<ContextComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Same(context, result);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenNoContextFound()
    {
        // Arrange
        var root = new ContainerComponent();
        var target = new TargetComponent();
        
        root.AddChild(target);
        
        var lookup = new ContextLookup<ContextComponent>();
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Null(result);
    }

    private class ContextComponent : Component { }
    private class ContainerComponent : Component { }
    private class TargetComponent : Component { }
}
