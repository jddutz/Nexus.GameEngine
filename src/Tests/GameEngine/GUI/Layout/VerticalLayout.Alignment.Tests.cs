using Moq;
using Nexus.GameEngine.GUI.Layout;
using Silk.NET.Maths;

namespace Tests.GameEngine.GUI.Layout;

public class VerticalLayout_Alignment_Tests
{
    private readonly Mock<Nexus.GameEngine.Graphics.Descriptors.IDescriptorManager> _descriptorManagerMock;

    public VerticalLayout_Alignment_Tests()
    {
        _descriptorManagerMock = new Mock<Nexus.GameEngine.Graphics.Descriptors.IDescriptorManager>();
    }

    [Theory]
    [InlineData(-1)] // top
    [InlineData(0)]  // center
    [InlineData(1)]  // bottom
    public void AlignmentY_PositioningBehavior(int alignmentValue)
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child1, getC1) = CreateMockElement(new Vector2D<int>(100, 20));
        var (child2, getC2) = CreateMockElement(new Vector2D<int>(100, 20));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: new Vector2D<float>(0, (float)alignmentValue),
            alignment: new Vector2D<float>(0, (float)alignmentValue)
        );

        layout.AddChild(child1.Object);
        layout.AddChild(child2.Object);

        // Act
        layout.Activate();

        // Assert: constraints were set (non-zero size means SetSizeConstraints was called)
        var c1 = getC1();
        var c2 = getC2();

        Assert.True(c1.Size.Y > 0);
        Assert.True(c2.Size.Y > 0);
    }

    private (Mock<Nexus.GameEngine.GUI.IUserInterfaceElement>, System.Func<Rectangle<int>>) CreateMockElement(Vector2D<int> size)
    {
        var mock = new Mock<Nexus.GameEngine.GUI.IUserInterfaceElement>();
        mock.Setup(m => m.Size).Returns(size);
        mock.Setup(m => m.Measure(It.IsAny<Vector2D<int>>())).Returns(size);
        mock.Setup(m => m.Measure()).Returns(size);

        Rectangle<int> captured = default;
        mock.Setup(m => m.SetSizeConstraints(It.IsAny<Rectangle<int>>()))
            .Callback<Rectangle<int>>(r => captured = r);

        return (mock, () => captured);
    }
}
