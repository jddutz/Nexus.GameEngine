using Moq;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Textures;
using Nexus.GameEngine.Components;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Tests.GameEngine.GUI.Layout;

/// <summary>
/// Unit tests for HorizontalLayout component.
/// Tests horizontal arrangement, alignment, and spacing behavior.
/// </summary>
public class HorizontalLayoutTests
{
    private readonly Mock<IDescriptorManager> _descriptorManagerMock;
    private readonly Mock<IResourceManager> _resourceManagerMock;
    private readonly Mock<IPipelineManager> _pipelineManagerMock;

    public HorizontalLayoutTests()
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
        mock.Setup(m => m.Measure()).Returns(size);

        Rectangle<int> capturedConstraints = default;
        mock.Setup(m => m.SetSizeConstraints(It.IsAny<Rectangle<int>>()))
            .Callback<Rectangle<int>>(c => capturedConstraints = c);

        return (mock, () => capturedConstraints);
    }

    [Fact]
    public void NoChildren_DoesNothing()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - No exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void ItemSpacing_SetsSpacingBetweenChildren()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (child1, getConstraints1) = CreateMockElement(new Vector2D<int>(20, 50));
        var (child2, getConstraints2) = CreateMockElement(new Vector2D<int>(30, 50));

        layout.AddChild((IComponent)child1.Object);
        layout.AddChild((IComponent)child2.Object);

        layout.SetSize(new Vector2D<int>(100, 100));
        layout.SetAnchorPoint(new Vector2D<float>(-1, -1));
        layout.SetAlignment(new Vector2D<float>(-1, 0)); // Left align

        // Set AlignContent to left so children start at X=0
        layout.SetAlignContent(Align.Left);

        // Set ItemSpacing to 10
        layout.SetItemSpacing(10);

        layout.Activate();
        layout.ApplyUpdates(0.0); // Apply deferred property changes
        layout.Update(0.016);

        // Assert
        var c1 = getConstraints1();
        var c2 = getConstraints2();

        Assert.Equal(0, c1.Origin.X);  // First at left
        Assert.Equal(20, c1.Size.X);
        Assert.Equal(20 + 10, c2.Origin.X); // Second after first + spacing
        Assert.Equal(30, c2.Size.X);
    }

    [Fact]
    public void Spacing_Justified_DistributesSpaceBetweenChildren()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (child1, getConstraints1) = CreateMockElement(new Vector2D<int>(20, 100));
        var (child2, getConstraints2) = CreateMockElement(new Vector2D<int>(30, 100));

        layout.AddChild((IComponent)child1.Object);
        layout.AddChild((IComponent)child2.Object);

        layout.SetSize(new Vector2D<int>(100, 100));
        layout.SetAnchorPoint(new Vector2D<float>(-1, -1));
        layout.SetAlignment(new Vector2D<float>(-1, 0)); // Left align

        // Set Spacing to Justified
        layout.SetSpacing(SpacingMode.Justified);

        layout.Activate();
        layout.ApplyUpdates(0.0); // Apply deferred property changes
        layout.Update(0.016);

        // Assert
        var c1 = getConstraints1();
        var c2 = getConstraints2();

        // Total width = 50, content width = 100
        // Spacing = (100 - 50) / 1 = 50
        Assert.Equal(0, c1.Origin.X);
        Assert.Equal(20, c1.Size.X);
        Assert.Equal(70, c2.Origin.X); // 20 + 50
        Assert.Equal(30, c2.Size.X);
    }

    [Fact]
    public void Spacing_Distributed_DistributesSpaceEvenly()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (child1, getConstraints1) = CreateMockElement(new Vector2D<int>(20, 100));
        var (child2, getConstraints2) = CreateMockElement(new Vector2D<int>(30, 100));

        layout.AddChild((IComponent)child1.Object);
        layout.AddChild((IComponent)child2.Object);

        layout.SetSize(new Vector2D<int>(100, 100));
        layout.SetAnchorPoint(new Vector2D<float>(-1, -1));
        layout.SetAlignment(new Vector2D<float>(-1, 0));

        // Set Spacing to Distributed
        layout.SetSpacing(SpacingMode.Distributed);

        layout.Activate();
        layout.ApplyUpdates(0.0); // Apply deferred property changes
        layout.Update(0.016);

        // Assert
        var c1 = getConstraints1();
        var c2 = getConstraints2();

        // Total width = 50, content width = 100
        // Spacing = (100 - 50) / 3 ≈ 16.67
        Assert.Equal(16, c1.Origin.X);
        Assert.Equal(20, c1.Size.X);
        Assert.Equal(16 + 20 + 16, c2.Origin.X); // 52
        Assert.Equal(30, c2.Size.X);
    }

    [Fact]
    public void ItemWidth_OverridesChildMeasuredWidth()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (child1, getC1) = CreateMockElement(new Vector2D<int>(30, 100)); // 30px measured
        var (child2, getC2) = CreateMockElement(new Vector2D<int>(50, 100)); // 50px measured

        layout.Load(
            size: new Vector2D<int>(400, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            alignContent: Align.Left,
            itemWidth: 80 // Force all children to 80px
        );

        layout.AddChild(child1.Object);
        layout.AddChild(child2.Object);

        // Act
        layout.Activate();

        // Assert - both children should have 80px width regardless of measured size
        var c1 = getC1();
        var c2 = getC2();
        
        Assert.Equal(80, c1.Size.X);
        Assert.Equal(80, c2.Size.X);
        Assert.Equal(0, c1.Origin.X);
        Assert.Equal(80, c2.Origin.X); // Second child starts after first (no spacing)
    }

    [Fact]
    public void ItemWidth_WithItemSpacing_CreatesUniformLayout()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (child1, getC1) = CreateMockElement(new Vector2D<int>(20, 100));
        var (child2, getC2) = CreateMockElement(new Vector2D<int>(40, 100));
        var (child3, getC3) = CreateMockElement(new Vector2D<int>(60, 100));

        layout.Load(
            size: new Vector2D<int>(400, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            alignContent: Align.Left,
            itemWidth: 70,
            itemSpacing: 15
        );

        layout.AddChild(child1.Object);
        layout.AddChild(child2.Object);
        layout.AddChild(child3.Object);

        // Act
        layout.Activate();

        // Assert - uniform 70px widths with 15px spacing
        var c1 = getC1();
        var c2 = getC2();
        var c3 = getC3();
        
        Assert.Equal(70, c1.Size.X);
        Assert.Equal(70, c2.Size.X);
        Assert.Equal(70, c3.Size.X);
        Assert.Equal(0, c1.Origin.X);
        Assert.Equal(85, c2.Origin.X);   // 70 + 15
        Assert.Equal(170, c3.Origin.X);  // 155 + 15
    }

    [Fact]
    public void ItemWidth_VaryingChildSizes_AllForcedToSameWidth()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (small, getSmall) = CreateMockElement(new Vector2D<int>(15, 100));
        var (medium, getMedium) = CreateMockElement(new Vector2D<int>(45, 100));
        var (large, getLarge) = CreateMockElement(new Vector2D<int>(120, 100));

        layout.Load(
            size: new Vector2D<int>(500, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            itemWidth: 55
        );

        layout.AddChild(small.Object);
        layout.AddChild(medium.Object);
        layout.AddChild(large.Object);

        // Act
        layout.Activate();

        // Assert - all forced to 55px despite different measured sizes
        var s = getSmall();
        var m = getMedium();
        var l = getLarge();
        
        Assert.Equal(55, s.Size.X);
        Assert.Equal(55, m.Size.X);
        Assert.Equal(55, l.Size.X);
    }

    [Fact]
    public void ItemWidth_TakesPrecedenceOver_ChildMeasure()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (child, getConstraints) = CreateMockElement(new Vector2D<int>(150, 100)); // Child says 150px

        layout.Load(
            size: new Vector2D<int>(400, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            itemWidth: 35 // Layout overrides to 35px
        );

        layout.AddChild(child.Object);

        // Act
        layout.Activate();

        // Assert - layout's ItemWidth wins
        var c = getConstraints();
        Assert.Equal(35, c.Size.X);
    }

    [Fact]
    public void UpdateLayout_WithEmptyChildrenCollection_DoesNotThrow()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft
        );

        // Act & Assert - should not throw with no children
        var exception = Record.Exception(() => layout.Activate());
        Assert.Null(exception);
    }

    [Fact]
    public void UpdateLayout_WithSingleChild_UsesAlignContent()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (child, getConstraints) = CreateMockElement(new Vector2D<int>(50, 100));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            alignContent: -1.0f // Left align
        );

        layout.AddChild(child.Object);

        // Act
        layout.Activate();

        // Assert - single child positioned at left (alignContent = -1)
        var c = getConstraints();
        Assert.Equal(0, c.Origin.X);
    }

    [Fact]
    public void Justified_WithTwoElements_OneZeroWidth_CentersRemainingElement()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (visible, getVisible) = CreateMockElement(new Vector2D<int>(50, 100));
        var (zeroWidth, getZero) = CreateMockElement(new Vector2D<int>(0, 100));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            spacing: SpacingMode.Justified
        );

        layout.AddChild(visible.Object);
        layout.AddChild(zeroWidth.Object);

        // Act
        layout.Activate();

        // Assert - zero-width child is ignored, single visible child uses AlignContent (default 0 = center)
        var v = getVisible();
        Assert.Equal(75, v.Origin.X); // Centered: (200 - 50) / 2 = 75
    }

    [Fact]
    public void Justified_WithMultipleElements_OneZeroWidth_SpacingNotDoubled()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (child1, get1) = CreateMockElement(new Vector2D<int>(40, 100));
        var (zeroWidth, getZero) = CreateMockElement(new Vector2D<int>(0, 100));
        var (child2, get2) = CreateMockElement(new Vector2D<int>(40, 100));
        var (child3, get3) = CreateMockElement(new Vector2D<int>(40, 100));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            spacing: SpacingMode.Justified
        );

        layout.AddChild(child1.Object);
        layout.AddChild(zeroWidth.Object); // Should be completely ignored
        layout.AddChild(child2.Object);
        layout.AddChild(child3.Object);

        // Act
        layout.Activate();

        // Assert - zero-width child ignored, spacing calculated for 3 visible children only
        // Total width: 3 * 40 = 120, remaining: 200 - 120 = 80
        // Justified spacing: 80 / 2 gaps = 40 per gap
        var c1 = get1();
        var c2 = get2();
        var c3 = get3();
        
        Assert.Equal(0, c1.Origin.X);
        Assert.Equal(80, c2.Origin.X);   // 40 (child1) + 40 (gap)
        Assert.Equal(160, c3.Origin.X);  // 120 (child1+child2) + 40 (gap)
    }

    [Fact]
    public void Distributed_WithMultipleElements_OneZeroWidth_SpacingNotDoubled()
    {
        // Arrange
        var layout = new HorizontalLayout(_descriptorManagerMock.Object);
        var (child1, get1) = CreateMockElement(new Vector2D<int>(30, 100));
        var (zeroWidth, getZero) = CreateMockElement(new Vector2D<int>(0, 100));
        var (child2, get2) = CreateMockElement(new Vector2D<int>(30, 100));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            spacing: SpacingMode.Distributed
        );

        layout.AddChild(child1.Object);
        layout.AddChild(zeroWidth.Object); // Should be completely ignored
        layout.AddChild(child2.Object);

        // Act
        layout.Activate();

        // Assert - zero-width child ignored, spacing calculated for 2 visible children only
        // Total width: 2 * 30 = 60, remaining: 200 - 60 = 140
        // Distributed spacing: 140 / 3 spaces = 46.666... per space
        var c1 = get1();
        var c2 = get2();
        
        Assert.Equal(46, c1.Origin.X);   // First space (rounded down)
        Assert.Equal(122, c2.Origin.X);  // 46 (space) + 30 (child1) + 46 (space, rounded down)
    }

    /// <summary>
    /// Test implementation of HorizontalLayout for testing.
    /// </summary>
    private class TestHorizontalLayout(IDescriptorManager descriptorManager) : HorizontalLayout(descriptorManager)
    {
    }
}