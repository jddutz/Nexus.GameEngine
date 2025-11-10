using Moq;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Textures;
using Silk.NET.Maths;

namespace Tests.GameEngine.GUI;

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
        var container = new Container(_descriptorManagerMock.Object);

        // Create a mock child that captures the constraints set by the container
        var childMock = new Mock<IUserInterfaceElement>();
        Rectangle<int> captured = default;
        childMock.Setup(c => c.SetSizeConstraints(It.IsAny<Rectangle<int>>()))
            .Callback<Rectangle<int>>(r => captured = r);

        container.AddChild(childMock.Object);
        container.Activate();

        // Give the container a viewport-sized constraint
        var constraints = new Rectangle<int>(0, 0, 200, 100);
        container.SetSizeConstraints(constraints);

        // Run update (triggers layout)
        container.Update(0.016);

        // The child should receive the content area (no padding) which equals the full constraints
        Assert.Equal(0, captured.Origin.X);
        Assert.Equal(0, captured.Origin.Y);
        Assert.Equal(200, captured.Size.X);
        Assert.Equal(100, captured.Size.Y);
    }
}
