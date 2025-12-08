using Xunit;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Components;
using System.Collections.Generic;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Layout.Tests;

public class HorizontalLayoutControllerTests
{
    [Fact]
    public void UpdateLayout_Stacked_PositionsChildrenCorrectly()
    {
        // Arrange
        var controller = new HorizontalLayoutController();
        controller.SetCurrentItemSpacing(10.0f);
        controller.SetCurrentSpacing(SpacingMode.Stacked);
        controller.SetCurrentAlignment(-1.0f); // Top align

        var container = new UserInterfaceElement();
        
        var child1 = new UserInterfaceElement();
        child1.SetCurrentSize(new Vector2D<float>(100, 50));
        
        var child2 = new UserInterfaceElement();
        child2.SetCurrentSize(new Vector2D<float>(100, 50));

        container.AddChild(child1);
        container.AddChild(child2);

        // Act
        controller.UpdateLayout(container);

        // Assert
        // Child 1: X=0
        // Child 2: X=100 + 10 = 110
        
        Assert.Equal(0, child1.Position.X);
        Assert.Equal(110, child2.Position.X);
    }

    [Fact]
    public void UpdateLayout_Justified_PositionsChildrenCorrectly()
    {
        // Arrange
        var controller = new HorizontalLayoutController();
        controller.SetCurrentSpacing(SpacingMode.Justified);
        controller.SetCurrentAlignment(0.0f);

        var container = new UserInterfaceElement();
        container.SetCurrentSize(new Vector2D<float>(200, 100));

        var child1 = new UserInterfaceElement();
        child1.SetCurrentSize(new Vector2D<float>(50, 100));
        
        var child2 = new UserInterfaceElement();
        child2.SetCurrentSize(new Vector2D<float>(50, 100));
        
        var child3 = new UserInterfaceElement();
        child3.SetCurrentSize(new Vector2D<float>(50, 100));

        container.AddChild(child1);
        container.AddChild(child2);
        container.AddChild(child3);

        // Act
        controller.UpdateLayout(container);

        // Assert
        // Total width = 150. Available = 200. Gap = 50 / 2 = 25.
        // Child 1: 0
        // Child 2: 50 + 25 = 75
        // Child 3: 75 + 50 + 25 = 150
        
        Assert.Equal(0, child1.Position.X);
        Assert.Equal(75, child2.Position.X);
        Assert.Equal(150, child3.Position.X);
    }

    [Fact]
    public void UpdateLayout_Distributed_PositionsChildrenCorrectly()
    {
        // Arrange
        var controller = new HorizontalLayoutController();
        controller.SetCurrentSpacing(SpacingMode.Distributed);
        controller.SetCurrentAlignment(0.0f);

        var container = new UserInterfaceElement();
        container.SetCurrentSize(new Vector2D<float>(200, 100));

        var child1 = new UserInterfaceElement();
        child1.SetCurrentSize(new Vector2D<float>(50, 100));
        
        var child2 = new UserInterfaceElement();
        child2.SetCurrentSize(new Vector2D<float>(50, 100));

        container.AddChild(child1);
        container.AddChild(child2);

        // Act
        controller.UpdateLayout(container);

        // Assert
        // Total width = 100. Available = 200. Gap = 100 / 3 = 33.33
        // Child 1: 33.33
        // Child 2: 33.33 + 50 + 33.33 = 116.66
        
        Assert.Equal(33.333f, child1.Position.X, 2);
        Assert.Equal(116.666f, child2.Position.X, 2);
    }

    [Fact]
    public void UpdateLayout_IgnoresZeroWidthChildren()
    {
        // Arrange
        var controller = new HorizontalLayoutController();
        controller.SetCurrentItemSpacing(10.0f);
        controller.SetCurrentSpacing(SpacingMode.Stacked);

        var container = new UserInterfaceElement();
        
        var child1 = new UserInterfaceElement();
        child1.SetCurrentSize(new Vector2D<float>(50, 100));
        
        var child2 = new UserInterfaceElement();
        child2.SetCurrentSize(new Vector2D<float>(0, 100)); // Zero width
        
        var child3 = new UserInterfaceElement();
        child3.SetCurrentSize(new Vector2D<float>(50, 100));

        container.AddChild(child1);
        container.AddChild(child2);
        container.AddChild(child3);

        // Act
        controller.UpdateLayout(container);

        // Assert
        // Child 1: 0
        // Child 3: 50 + 10 = 60
        
        Assert.Equal(0, child1.Position.X);
        Assert.Equal(60, child3.Position.X);
    }
}
