using Nexus.GameEngine.GUI;
using Silk.NET.Maths;
using Xunit;

namespace Tests.GameEngine.GUI;

public class UserInterfaceElementTests
{
    [Fact]
    public void Inheritance_IsRectTransform()
    {
        var ui = new UserInterfaceElement();
        Assert.IsAssignableFrom<Nexus.GameEngine.Components.RectTransform>(ui);
    }

    [Fact]
    public void Bounds_CalculatedCorrectly()
    {
        var ui = new UserInterfaceElement();
        ui.SetSize(new Vector2D<float>(200, 100));
        ui.SetPosition(new Vector2D<float>(50, 50));
        
        var bounds = ui.GetBounds();
        
        Assert.Equal(50, bounds.Origin.X);
        Assert.Equal(50, bounds.Origin.Y);
        Assert.Equal(200, bounds.Size.X);
        Assert.Equal(100, bounds.Size.Y);
    }

    [Fact]
    public void TransformChanges_ReflectedInBounds()
    {
        var ui = new UserInterfaceElement();
        ui.SetSize(new Vector2D<float>(100, 100));
        
        var bounds1 = ui.GetBounds();
        Assert.Equal(100, bounds1.Size.X);
        
        ui.SetScale(new Vector2D<float>(2, 2));
        var bounds2 = ui.GetBounds();
        Assert.Equal(200, bounds2.Size.X);
    }
}
