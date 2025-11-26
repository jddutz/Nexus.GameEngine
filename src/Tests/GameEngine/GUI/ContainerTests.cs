using System.Drawing;
using System.Runtime.CompilerServices;
using Nexus.GameEngine.GUI;
using Silk.NET.Maths;
using Xunit;

namespace Tests.GameEngine.GUI;

public class ContainerTests
{
    [Fact]
    public void UserInterfaceElement_CanContainChildren()
    {
        var container = new UserInterfaceElement();
        var child1 = new UserInterfaceElement();
        var child2 = new UserInterfaceElement();

        container.AddChild(child1);
        container.AddChild(child2);

        Assert.Contains(child1, container.Children);
        Assert.Contains(child2, container.Children);
        Assert.Equal(container, child1.Parent);
        Assert.Equal(container, child2.Parent);
    }

    [Fact]
    public void UserInterfaceElement_PropagatesWorldMatrix()
    {
        var containerPosition = new Vector2D<float>(100, 100);
        var containerSize = new Vector2D<float>(100,100);

        var container = new UserInterfaceElement();
        container.SetPosition(containerPosition);
        container.SetSize(containerSize);
        
        var childPosition = new Vector2D<float>(10, 10);
        var childSize = new Vector2D<float>(10, 10);
        var child = new UserInterfaceElement();
        child.SetPosition(childPosition);
        child.SetSize(childSize);
        
        container.AddChild(child);
        
        var worldMatrix = child.WorldMatrix;
        Assert.Equal(110, worldMatrix.M41);
        Assert.Equal(110, worldMatrix.M42);
    }
}
