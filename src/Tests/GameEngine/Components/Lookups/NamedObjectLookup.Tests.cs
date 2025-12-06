using Xunit;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Components.Lookups;

namespace Tests.GameEngine.Components.Lookups;

public class NamedObjectLookupTests
{
    [Fact]
    public void Resolve_ShouldFindSiblingByName()
    {
        // Arrange
        var root = new TestComponent { Name = "Root" };
        var target = new TestComponent { Name = "Target" };
        var sibling = new TestComponent { Name = "Sibling" };
        
        root.AddChild(target);
        root.AddChild(sibling);
        
        var lookup = new NamedObjectLookup("Sibling");
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Same(sibling, result);
    }

    [Fact]
    public void Resolve_ShouldFindChildByName()
    {
        // Arrange
        var root = new TestComponent { Name = "Root" };
        var target = new TestComponent { Name = "Target" };
        var child = new TestComponent { Name = "Child" };
        
        root.AddChild(target);
        target.AddChild(child);
        
        var lookup = new NamedObjectLookup("Child");
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Same(child, result);
    }

    [Fact]
    public void Resolve_ShouldFindParentByName()
    {
        // Arrange
        var root = new TestComponent { Name = "Root" };
        var target = new TestComponent { Name = "Target" };
        
        root.AddChild(target);
        
        var lookup = new NamedObjectLookup("Root");
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Same(root, result);
    }

    [Fact]
    public void Resolve_ShouldFindDistantRelativeByName()
    {
        // Arrange
        var root = new TestComponent { Name = "Root" };
        var branch1 = new TestComponent { Name = "Branch1" };
        var branch2 = new TestComponent { Name = "Branch2" };
        var target = new TestComponent { Name = "Target" };
        var distant = new TestComponent { Name = "Distant" };
        
        root.AddChild(branch1);
        root.AddChild(branch2);
        branch1.AddChild(target);
        branch2.AddChild(distant);
        
        var lookup = new NamedObjectLookup("Distant");
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Same(distant, result);
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var root = new TestComponent { Name = "Root" };
        var target = new TestComponent { Name = "Target" };
        
        root.AddChild(target);
        
        var lookup = new NamedObjectLookup("NonExistent");
        
        // Act
        var result = lookup.Resolve(target);
        
        // Assert
        Assert.Null(result);
    }

    private class TestComponent : Component
    {
        public new string Name 
        { 
            get => base.Name; 
            set => SetCurrentName(value); 
        }
    }
}
