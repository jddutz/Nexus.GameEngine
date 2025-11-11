using Moq;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Graphics.Descriptors;
using Silk.NET.Maths;

namespace Tests.GameEngine.GUI;

public class ElementTests
{
    [Fact]
    public void GetBounds_CalculatesFromPositionAnchorAndSize()
    {
        var descriptorMock = new Mock<IDescriptorManager>();
        var elem = new TestElement(descriptorMock.Object);

        elem.SetPosition(new Vector3D<float>(100f, 200f, 0f));
        elem.SetSize(new Vector2D<int>(40, 20));
        elem.SetAnchorPoint(new Vector2D<float>(0f, 0f)); // center
        elem.ApplyUpdates(0.016);

        var bounds = elem.GetBounds();

        // center anchor should place origin at position - size/2
        Assert.Equal(80, bounds.Origin.X);
        Assert.Equal(190, bounds.Origin.Y);
        Assert.Equal(40, bounds.Size.X);
        Assert.Equal(20, bounds.Size.Y);
    }

    [Fact]
    public void Measure_Fixed_ReturnsTargetSize()
    {
        var descriptorMock = new Mock<IDescriptorManager>();
        var elem = new TestElement(descriptorMock.Object);

        elem.SetSize(new Vector2D<int>(120, 80));
        elem.ApplyUpdates(0.016);

        var measured = elem.Measure(new Vector2D<int>(500, 500));
        Assert.Equal(120, measured.X);
        Assert.Equal(80, measured.Y);
    }

    [Fact]
    public void Measure_Percentage_ComputesFromAvailable()
    {
        var descriptorMock = new Mock<IDescriptorManager>();
        var elem = new TestElement(descriptorMock.Object);

        elem.SetSizeMode(SizeMode.Percent);
        elem.SetRelativeWidth(0.5f);
        elem.SetRelativeHeight(0.25f);
        elem.ApplyUpdates(0.016);

        var measured = elem.Measure(new Vector2D<int>(200, 100));
        Assert.Equal(100, measured.X); // 50% of 200
        Assert.Equal(25, measured.Y);  // 25% of 100
    }

    [Fact]
    public void Measure_Stretch_ReturnsAvailable_And_MinMaxAreEnforced()
    {
        var descriptorMock = new Mock<IDescriptorManager>();
        var elem = new TestElement(descriptorMock.Object);

        elem.SetSizeMode(SizeMode.Stretch);
        elem.SetMinSize(new Vector2D<int>(50, 60));
        elem.SetMaxSize(new Vector2D<int>(80, 0)); // 0 means no limit for height
        elem.ApplyUpdates(0.016);

        // Available smaller than min -> clamps to min
        var measured1 = elem.Measure(new Vector2D<int>(30, 40));
        Assert.Equal(50, measured1.X);
        Assert.Equal(60, measured1.Y);

        // Available larger than max on X -> clamps to max X
        var measured2 = elem.Measure(new Vector2D<int>(200, 200));
        Assert.Equal(80, measured2.X);
        Assert.Equal(200, measured2.Y); // height has no max
    }

    // Minimal concrete Element for testing
    private class TestElement : Element
    {
        public TestElement(IDescriptorManager descriptorManager) : base(descriptorManager) { }
    }
}
