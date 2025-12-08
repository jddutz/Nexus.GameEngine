using Xunit;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Components;
using System.Collections.Generic;
using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI.Layout.Tests;

public class VerticalLayoutControllerTests
{
    [Fact]
    public void UpdateLayout_Stacked_PositionsChildrenCorrectly()
    {
        // Arrange
        var controller = new VerticalLayoutController();
        controller.SetCurrentItemSpacing(10.0f);
        controller.SetCurrentSpacing(SpacingMode.Stacked);
        controller.SetCurrentAlignment(-1.0f);

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
        // Child 1 should be at (0, 0) (relative to container content area)
        // Child 2 should be at (0, 50 + 10) = (0, 60)
        
        // Note: Alignment -1.0 means Left aligned. 
        // If container has padding, it should be accounted for, but here we assume 0 padding.
        
        Assert.Equal(0, child1.Position.Y);
        Assert.Equal(60, child2.Position.Y);
    }

    [Fact]
    public void UpdateLayout_Justified_PositionsChildrenCorrectly()
    {
        // Arrange
        var controller = new VerticalLayoutController();
        controller.SetCurrentSpacing(SpacingMode.Justified);
        controller.SetCurrentAlignment(0.0f); // Center align

        var container = new UserInterfaceElement();
        container.SetCurrentSize(new Vector2D<float>(100, 200)); // Height 200

        var child1 = new UserInterfaceElement();
        child1.SetCurrentSize(new Vector2D<float>(100, 50));
        
        var child2 = new UserInterfaceElement();
        child2.SetCurrentSize(new Vector2D<float>(100, 50));
        
        var child3 = new UserInterfaceElement();
        child3.SetCurrentSize(new Vector2D<float>(100, 50));

        container.AddChild(child1);
        container.AddChild(child2);
        container.AddChild(child3);

        // Act
        controller.UpdateLayout(container);

        // Assert
        // Total child height = 150. Available space = 200.
        // Justified: First at start, Last at end.
        // Space remaining = 200 - 150 = 50.
        // Gaps = count - 1 = 2.
        // Gap size = 50 / 2 = 25.
        
        // Child 1: 0
        // Child 2: 50 + 25 = 75
        // Child 3: 75 + 50 + 25 = 150 (or 200 - 50 = 150)
        
        Assert.Equal(0, child1.Position.Y);
        Assert.Equal(75, child2.Position.Y);
        Assert.Equal(150, child3.Position.Y);
    }

    [Fact]
    public void UpdateLayout_Distributed_PositionsChildrenCorrectly()
    {
        // Arrange
        var controller = new VerticalLayoutController();
        controller.SetCurrentSpacing(SpacingMode.Distributed);
        controller.SetCurrentAlignment(0.0f);

        var container = new UserInterfaceElement();
        container.SetCurrentSize(new Vector2D<float>(100, 200));

        var child1 = new UserInterfaceElement();
        child1.SetCurrentSize(new Vector2D<float>(100, 50));
        
        var child2 = new UserInterfaceElement();
        child2.SetCurrentSize(new Vector2D<float>(100, 50));

        container.AddChild(child1);
        container.AddChild(child2);

        // Act
        controller.UpdateLayout(container);

        // Assert
        // Total child height = 100. Available space = 200.
        // Distributed: Equal spacing before, between, after.
        // Gaps = count + 1 = 3.
        // Space remaining = 200 - 100 = 100.
        // Gap size = 100 / 3 = 33.333...
        
        // Child 1: 33.33
        // Child 2: 33.33 + 50 + 33.33 = 116.66
        
        Assert.Equal(33.333f, child1.Position.Y, 2);
        Assert.Equal(116.666f, child2.Position.Y, 2);
    }

    [Fact]
    public void UpdateLayout_ClampsAlignment()
    {
        // Arrange
        var controller = new VerticalLayoutController();
        controller.SetCurrentAlignment(-2.0f); // Should be clamped to -1.0

        var container = new UserInterfaceElement();
        container.SetCurrentSize(new Vector2D<float>(100, 100));
        var child = new UserInterfaceElement();
        child.SetCurrentSize(new Vector2D<float>(50, 50));
        container.AddChild(child);

        // Act
        controller.UpdateLayout(container);

        // Assert
        // Alignment -1.0 -> X = 0
        Assert.Equal(0, child.Position.X);
    }

    [Fact]
    public void UpdateLayout_IgnoresZeroHeightChildren()
    {
        // Arrange
        var controller = new VerticalLayoutController();
        controller.SetCurrentItemSpacing(10.0f);
        controller.SetCurrentSpacing(SpacingMode.Stacked);

        var container = new UserInterfaceElement();
        
        var child1 = new UserInterfaceElement();
        child1.SetCurrentSize(new Vector2D<float>(100, 50));
        
        var child2 = new UserInterfaceElement();
        child2.SetCurrentSize(new Vector2D<float>(100, 0)); // Zero height
        
        var child3 = new UserInterfaceElement();
        child3.SetCurrentSize(new Vector2D<float>(100, 50));

        container.AddChild(child1);
        container.AddChild(child2);
        container.AddChild(child3);

        // Act
        controller.UpdateLayout(container);

        // Assert
        // Child 1: 0
        // Child 2: Should be ignored (or placed at currentY but not advance Y?)
        // The spec says "excluded from layout calculations".
        // So Child 3 should be at 50 + 10 = 60.
        
        Assert.Equal(0, child1.Position.Y);
        Assert.Equal(60, child3.Position.Y);
    }
}
