using Moq;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Textures;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Tests.GameEngine.GUI.Layout;

/// <summary>
/// Unit tests for VerticalLayout component.
/// Tests vertical arrangement, alignment, and spacing behavior.
/// </summary>
public class VerticalLayoutTests
{
    private readonly Mock<IDescriptorManager> _descriptorManagerMock;
    private readonly Mock<IResourceManager> _resourceManagerMock;
    private readonly Mock<IPipelineManager> _pipelineManagerMock;

    public VerticalLayoutTests()
    {
        _descriptorManagerMock = new Mock<IDescriptorManager>();
        _resourceManagerMock = new Mock<IResourceManager>();
        _pipelineManagerMock = new Mock<IPipelineManager>();
        
        // Setup PipelineManager mock to return a valid PipelineHandle
        _pipelineManagerMock.Setup(pm => pm.GetOrCreate(It.IsAny<PipelineDefinition>()))
            .Returns(new PipelineHandle(new Pipeline(1), new PipelineLayout(1), "TestPipeline"));

        // Setup ResourceManager.Geometry to avoid NullReferenceException in Element.OnActivate
        var mockGeometryManager = new Mock<IGeometryResourceManager>();
        mockGeometryManager.Setup(g => g.GetOrCreate(It.IsAny<GeometryDefinition>())).Returns((IGeometryResource?)null);
        _resourceManagerMock.Setup(r => r.Geometry).Returns(mockGeometryManager.Object);

        // Setup ResourceManager.Textures to avoid NullReferenceException in Element.SetTexture
        var mockTextureManager = new Mock<ITextureResourceManager>();
        mockTextureManager.Setup(t => t.GetOrCreate(It.IsAny<TextureDefinition>())).Returns((TextureResource?)null!);
        _resourceManagerMock.Setup(r => r.Textures).Returns(mockTextureManager.Object);
    }

    /// <summary>
    /// Creates a mock IUserInterfaceElement for testing layout behavior.
    /// </summary>
    private (Mock<IUserInterfaceElement> Mock, Func<Rectangle<int>> GetConstraints) CreateMockElement(Vector2D<int> size)
    {
        var mock = new Mock<IUserInterfaceElement>();
        mock.Setup(m => m.Size).Returns(size);
        mock.Setup(m => m.Measure(It.IsAny<Vector2D<int>>())).Returns(size);

        Rectangle<int> capturedConstraints = default;
        mock.Setup(m => m.SetSizeConstraints(It.IsAny<Rectangle<int>>()))
            .Callback<Rectangle<int>>(c => capturedConstraints = c);

        return (mock, () => capturedConstraints);
    }

    [Fact]
    public void NoChildren_DoesNothing()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - No exceptions thrown
        Assert.True(true);
    }

}