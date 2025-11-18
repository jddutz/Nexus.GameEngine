using Moq;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Silk.NET.Maths;

namespace Tests.GameEngine.GUI.Layout;

public class VerticalLayout_ItemSpacing_Tests
{
    private readonly Mock<Nexus.GameEngine.Graphics.Descriptors.IDescriptorManager> _descriptorManagerMock;

    public VerticalLayout_ItemSpacing_Tests()
    {
        _descriptorManagerMock = new Mock<Nexus.GameEngine.Graphics.Descriptors.IDescriptorManager>();
    }

    [Fact]
    public void ItemSpacing_AppliesFixedGapBetweenChildren()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child1, getC1) = CreateMockElement(new Vector2D<int>(100, 20));
        var (child2, getC2) = CreateMockElement(new Vector2D<int>(100, 20));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            alignContent: Align.Top,
            itemSpacing: 10
        );

        layout.AddChild(child1.Object);
        layout.AddChild(child2.Object);

        // Act
        layout.Activate();

        // Assert - children should be separated by exactly 10px in Y
        var c1 = getC1();
        var c2 = getC2();

        // First child at top (Y=0), second child at first.bottom + spacing
        Assert.Equal(0, c1.Origin.Y);
        Assert.Equal(c1.Origin.Y + c1.Size.Y + 10, c2.Origin.Y);
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
