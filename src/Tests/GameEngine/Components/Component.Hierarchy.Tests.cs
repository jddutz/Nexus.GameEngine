using Xunit;
using Nexus.GameEngine.Components;
using System.Linq;

namespace Tests.GameEngine.Components;

public class ComponentHierarchyTests
{
    private class TestComponent : Component { }

    [Fact]
    public void AddChild_SetsParentAndAddsToCollection()
    {
        // Arrange
        var parent = new TestComponent();
        var child = new TestComponent();

        // Act
        parent.AddChild(child);

        // Assert
        Assert.Equal(parent, child.Parent);
        Assert.Contains(child, parent.Children);
    }

    [Fact]
    public void RemoveChild_ClearsParentAndRemovesFromCollection()
    {
        // Arrange
        var parent = new TestComponent();
        var child = new TestComponent();
        parent.AddChild(child);

        // Act
        parent.RemoveChild(child);

        // Assert
        Assert.Null(child.Parent);
        Assert.DoesNotContain(child, parent.Children);
    }

    [Fact]
    public void AddChild_RemovesFromOldParent()
    {
        // Arrange
        var oldParent = new TestComponent();
        var newParent = new TestComponent();
        var child = new TestComponent();
        oldParent.AddChild(child);

        // Act
        newParent.AddChild(child);

        // Assert
        Assert.DoesNotContain(child, oldParent.Children);
        Assert.Contains(child, newParent.Children);
        Assert.Equal(newParent, child.Parent);
    }
}
