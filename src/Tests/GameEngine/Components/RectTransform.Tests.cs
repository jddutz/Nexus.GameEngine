using Nexus.GameEngine.Components;
using Silk.NET.Maths;
using Xunit;

namespace Tests.GameEngine.Components;

public class RectTransformTests
{
    [Fact]
    public void GetBounds_Default_ReturnsZero()
    {
        var rect = new RectTransform();
        var bounds = rect.GetBounds();
        
        Assert.Equal(0, bounds.Origin.X);
        Assert.Equal(0, bounds.Origin.Y);
        Assert.Equal(0, bounds.Size.X);
        Assert.Equal(0, bounds.Size.Y);
    }

    [Fact]
    public void GetBounds_WithSize_ReturnsCorrectBounds()
    {
        var rect = new RectTransform();
        rect.SetSize(new Vector2D<float>(100, 50));
        
        var bounds = rect.GetBounds();
        
        // Default Pivot is (0,0) (Top-Left)
        // Position is (0,0)
        // So bounds should be (0,0, 100, 50)
        
        Assert.Equal(0, bounds.Origin.X);
        Assert.Equal(0, bounds.Origin.Y);
        Assert.Equal(100, bounds.Size.X);
        Assert.Equal(50, bounds.Size.Y);
    }

    [Fact]
    public void GetBounds_WithPivotCenter_ReturnsCenteredBounds()
    {
        var rect = new RectTransform();
        rect.SetSize(new Vector2D<float>(100, 100));
        rect.SetPivot(new Vector2D<float>(0.5f, 0.5f));
        rect.SetPosition(new Vector2D<float>(100, 100));
        
        var bounds = rect.GetBounds();
        
        // Pivot (0.5, 0.5) means Position is at center.
        // Size 100x100.
        // Top-Left should be (100-50, 100-50) = (50, 50)
        
        Assert.Equal(50, bounds.Origin.X);
        Assert.Equal(50, bounds.Origin.Y);
        Assert.Equal(100, bounds.Size.X);
        Assert.Equal(100, bounds.Size.Y);
    }

    [Fact]
    public void GetBounds_WithRotation_ReturnsEnclosingAABB()
    {
        var rect = new RectTransform();
        rect.SetSize(new Vector2D<float>(100, 100));
        rect.SetPivot(new Vector2D<float>(0.5f, 0.5f));
        rect.SetPosition(new Vector2D<float>(100, 100));
        rect.SetRotation(MathF.PI / 4); // 45 degrees
        
        var bounds = rect.GetBounds();
        
        // Rotated 45 degrees, the AABB should be larger.
        // Diagonal of 100x100 square is sqrt(100^2 + 100^2) = 141.42
        // New width/height should be ~141
        // Origin should be 100 - 141/2 = 100 - 70.7 = 29.3
        
        Assert.True(bounds.Size.X > 140 && bounds.Size.X < 142);
        Assert.True(bounds.Size.Y > 140 && bounds.Size.Y < 142);
    }
    
    [Fact]
    public void GetBounds_WithScale_ReturnsScaledBounds()
    {
        var rect = new RectTransform();
        rect.SetSize(new Vector2D<float>(100, 100));
        rect.SetScale(new Vector2D<float>(2, 2));
        
        var bounds = rect.GetBounds();
        
        Assert.Equal(200, bounds.Size.X);
        Assert.Equal(200, bounds.Size.Y);
    }

    [Fact]
    public void DynamicUpdates_InvalidateMatrix()
    {
        var rect = new RectTransform();
        var initialMatrix = rect.LocalMatrix;
        
        rect.SetPosition(new Vector2D<float>(10, 10));
        
        Assert.NotEqual(initialMatrix, rect.LocalMatrix);
        Assert.Equal(10, rect.LocalMatrix.M41);
        Assert.Equal(10, rect.LocalMatrix.M42);
    }

    [Fact]
    public void DynamicUpdates_InvalidateBounds()
    {
        var rect = new RectTransform();
        var initialBounds = rect.GetBounds();
        
        rect.SetSize(new Vector2D<float>(100, 100));
        
        var newBounds = rect.GetBounds();
        Assert.NotEqual(initialBounds, newBounds);
        Assert.Equal(100, newBounds.Size.X);
    }

    private class NonTransformComponent : Component { }

    [Fact]
    public void WorldMatrix_SkipsNonTransformParents()
    {
        var root = new RectTransform();
        root.SetPosition(new Vector2D<float>(100, 100));
        
        var container = new NonTransformComponent();
        root.AddChild(container);
        
        var child = new RectTransform();
        child.SetPosition(new Vector2D<float>(10, 10));
        container.AddChild(child);
        
        // Child world position should be Root + Child = (110, 110)
        var worldMatrix = child.WorldMatrix;
        
        Assert.Equal(110, worldMatrix.M41);
        Assert.Equal(110, worldMatrix.M42);
    }
}
