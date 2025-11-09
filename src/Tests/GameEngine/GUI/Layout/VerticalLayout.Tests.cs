using Moq;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Resources.Textures;
using Nexus.GameEngine.Resources.Textures.Definitions;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Xunit;

namespace Tests;

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
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var layout = new VerticalLayout(_descriptorManagerMock.Object);

        // Assert
        Assert.Equal(HorizontalAlignment.Center, layout.Alignment);
    }

    [Fact]
    public void Alignment_Property_Works()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);

        // Act
        layout.SetAlignment(HorizontalAlignment.Left);
        layout.ApplyUpdates(0.016f);

        // Assert
        Assert.Equal(HorizontalAlignment.Left, layout.Alignment);
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

    [Fact]
    public void SingleChild_CentersHorizontally()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        layout.Load(size: new Vector2D<int>(200, 100));
        
        var (childMock, getConstraints) = CreateMockElement(new Vector2D<int>(100, 50));
        layout.AddChild(childMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert
        var childConstraints = getConstraints();
        Assert.Equal(50, childConstraints.Origin.X); // Centered horizontally: (200-100)/2 = 50
        Assert.Equal(0, childConstraints.Origin.Y);  // Starts at top edge
        Assert.Equal(100, childConstraints.Size.X);
        Assert.Equal(50, childConstraints.Size.Y);
    }

    [Fact]
    public void MultipleChildren_WithSpacing()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            spacing: new Vector2D<float>(0, 15) // 15px vertical spacing
        );
        
        var (child1Mock, getConstraints1) = CreateMockElement(new Vector2D<int>(50, 30));
        var (child2Mock, getConstraints2) = CreateMockElement(new Vector2D<int>(75, 40));
        layout.AddChild(child1Mock.Object);
        layout.AddChild(child2Mock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert
        var constraints1 = getConstraints1();
        var constraints2 = getConstraints2();

        Assert.Equal(75, constraints1.Origin.X);   // Centered: (200-50)/2 = 75
        Assert.Equal(0, constraints1.Origin.Y);    // First child at top

        Assert.Equal(62, constraints2.Origin.X);   // Centered: (200-75)/2 = 62.5 -> 62
        Assert.Equal(45, constraints2.Origin.Y);   // Second child after first + spacing: 30 + 15 = 45
    }

    [Fact]
    public void LeftAlignment_PositionsCorrectly()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            alignment: HorizontalAlignment.Left
        );

        var (childMock, getConstraints) = CreateMockElement(new Vector2D<int>(50, 30));
        layout.AddChild(childMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert
        var constraints = getConstraints();
        Assert.Equal(0, constraints.Origin.X);  // Left edge
        Assert.Equal(0, constraints.Origin.Y);  // Top edge
    }

    [Fact]
    public void RightAlignment_PositionsCorrectly()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            alignment: HorizontalAlignment.Right
        );

        var (childMock, getConstraints) = CreateMockElement(new Vector2D<int>(50, 30));
        layout.AddChild(childMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert
        var constraints = getConstraints();
        Assert.Equal(150, constraints.Origin.X); // Right: 200 - 50 = 150
        Assert.Equal(0, constraints.Origin.Y);   // Top edge
    }

    [Fact]
    public void StretchAlignment_FillsWidth()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            alignment: HorizontalAlignment.Stretch
        );

        var (childMock, getConstraints) = CreateMockElement(new Vector2D<int>(50, 30));
        layout.AddChild(childMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert
        var constraints = getConstraints();
        Assert.Equal(0, constraints.Origin.X);   // Left edge
        Assert.Equal(0, constraints.Origin.Y);   // Top edge
        Assert.Equal(200, constraints.Size.X);     // Stretches to full width
        Assert.Equal(30, constraints.Size.Y);      // Maintains original height
    }
}