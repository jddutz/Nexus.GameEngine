using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Textures;
using Silk.NET.Maths;

namespace Tests.GameEngine.GUI.Layout;

public class ContainerLayoutTests
{
    private readonly Mock<IDescriptorManager> _descriptorManagerMock;
    private readonly Mock<IResourceManager> _resourceManagerMock;

    public ContainerLayoutTests()
    {
    _descriptorManagerMock = new Mock<IDescriptorManager>();
    _resourceManagerMock = new Mock<IResourceManager>();

        // Provide minimal resource manager pieces to satisfy activation code paths
        var mockGeometryManager = new Mock<IGeometryResourceManager>();
        mockGeometryManager.Setup(g => g.GetOrCreate(It.IsAny<GeometryDefinition>())).Returns((IGeometryResource?)null);
    _resourceManagerMock.Setup(r => r.Geometry).Returns(mockGeometryManager.Object);

        var mockTextureManager = new Mock<ITextureResourceManager>();
        mockTextureManager.Setup(t => t.GetOrCreate(It.IsAny<TextureDefinition>())).Returns((TextureResource?)null!);
    _resourceManagerMock.Setup(r => r.Textures).Returns(mockTextureManager.Object);
    }

    [Fact]
    public void Container_PassesFullContentArea_ToChild_WhenNoPadding()
    {
        // Arrange
        var container = new Container(_descriptorManagerMock.Object);
        var childMock = new Mock<IUserInterfaceElement>();
        
        Rectangle<int> captured = default;
        childMock.Setup(c => c.SetSizeConstraints(It.IsAny<Rectangle<int>>()))
            .Callback<Rectangle<int>>(r => captured = r);

        container.Load(
            size: new Vector2D<int>(200, 100),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft
        );

        container.AddChild(childMock.Object);

        // Act
        container.Activate();

        // Assert - child should receive the full content area (no padding)
        Assert.Equal(0, captured.Origin.X);
        Assert.Equal(0, captured.Origin.Y);
        Assert.Equal(200, captured.Size.X);
        Assert.Equal(100, captured.Size.Y);
    }

    [Fact]
    public void ContainerWithNoPadding_PassesFullContentAreaToChild_WhenActivated()
    {
        // Arrange
        var container = new Container(_descriptorManagerMock.Object);
        var child = new Mock<IUserInterfaceElement>();

        Rectangle<int> captured = default;
        child.Setup(m => m.SetSizeConstraints(It.IsAny<Rectangle<int>>()))
             .Callback<Rectangle<int>>(c => captured = c);

        container.Load(
            size: new Vector2D<int>(200,400),
            anchorPoint: Align.MiddleCenter,
            alignment: Align.MiddleCenter
        );

        container.AddChild(child.Object);

        // Act
        container.Activate();

        // Assert
        Assert.Equal(-100, captured.Origin.X);
        Assert.Equal(-200, captured.Origin.Y);
        Assert.Equal(200, captured.Size.X);
        Assert.Equal(400, captured.Size.Y);
    }
}
