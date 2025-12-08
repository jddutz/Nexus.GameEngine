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
        controller.SetCurrentAlignment(0.0f); // Left align

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
        // Content area is centered, so origin is at (0, 0) but we position at content origin
        // Container has no size set, so content area spans from 0,0
        // Child 1 should be at (0, 0) (relative to content area origin)
        // Child 2 should be at (0, 50 + 10) = (0, 60)
        
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
        // Container size 100x200, content area: origin=(-50, -100), size=(100, 200)
        // Total child height = 150. Available space = 200.
        // Justified: First at start, Last at end.
        // Space remaining = 200 - 150 = 50.
        // Gaps = count - 1 = 2.
        // Gap size = 50 / 2 = 25.
        
        // Child 1: -100 (content origin Y)
        // Child 2: -100 + 50 + 25 = -25
        // Child 3: -25 + 50 + 25 = 50
        
        Assert.Equal(-100, child1.Position.Y);
        Assert.Equal(-25, child2.Position.Y);
        Assert.Equal(50, child3.Position.Y);
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
        // Container size 100x200, content area: origin=(-50, -100), size=(100, 200)
        // Total child height = 100. Available space = 200.
        // Distributed: Equal spacing before, between, after.
        // Gaps = count + 1 = 3.
        // Space remaining = 200 - 100 = 100.
        // Gap size = 100 / 3 = 33.333...
        
        // Child 1: -100 + 33.33 = -66.67
        // Child 2: -66.67 + 50 + 33.33 = 16.66
        
        Assert.Equal(-66.666f, child1.Position.Y, 2);
        Assert.Equal(16.666f, child2.Position.Y, 2);
    }

    [Fact]
    public void UpdateLayout_ClampsAlignment()
    {
        // Arrange
        var controller = new VerticalLayoutController();
        controller.SetCurrentAlignment(-2.0f); // Should be clamped to 0.0

        var container = new UserInterfaceElement();
        container.SetCurrentSize(new Vector2D<float>(100, 100));
        var child = new UserInterfaceElement();
        child.SetCurrentSize(new Vector2D<float>(50, 50));
        container.AddChild(child);

        // Act
        controller.UpdateLayout(container);

        // Assert
        // Container size 100x100, content area: origin=(-50, -50), size=(100, 100)
        // Alignment 0.0 (clamped from -2.0) -> X = -50 (left edge of content area)
        Assert.Equal(-50, child.Position.X);
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
