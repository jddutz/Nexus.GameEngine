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
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
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
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child1, getConstraints1) = CreateMockElement(new Vector2D<int>(50, 20));
        var (child2, getConstraints2) = CreateMockElement(new Vector2D<int>(50, 30));

        layout.AddChild((IComponent)child1.Object);
        layout.AddChild((IComponent)child2.Object);

        // Set layout size
        layout.SetSize(new Vector2D<int>(100, 100));

        // Set anchor point to top-left for content area starting at 0
        layout.SetAnchorPoint(new Vector2D<float>(-1, -1));

        // Set alignment to top-left for layout positioning
        layout.SetAlignment(new Vector2D<float>(-1, -1));

        // Set AlignContent to top so children start at Y=0
        layout.SetAlignContent(Align.Top);

        // Set ItemSpacing to 10
        layout.SetItemSpacing(10);

        layout.Activate();
        layout.ApplyUpdates(0.0); // Apply deferred property changes
        layout.Update(0.016);

        // Assert
        var constraints1 = getConstraints1();
        var constraints2 = getConstraints2();

        // First child at top
        Assert.Equal(0, constraints1.Origin.Y);
        Assert.Equal(20, constraints1.Size.Y);

        // Second child below first with 10px spacing
        Assert.Equal(20 + 10, constraints2.Origin.Y);
        Assert.Equal(30, constraints2.Size.Y);
    }

    [Fact]
    public void Spacing_Justified_DistributesSpaceBetweenChildren()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child1, getConstraints1) = CreateMockElement(new Vector2D<int>(100, 20));
        var (child2, getConstraints2) = CreateMockElement(new Vector2D<int>(100, 30));

        layout.AddChild((IComponent)child1.Object);
        layout.AddChild((IComponent)child2.Object);

        layout.SetSize(new Vector2D<int>(100, 100));
        layout.SetAnchorPoint(new Vector2D<float>(-1, -1));
        layout.SetAlignment(new Vector2D<float>(0, -1));

        // Set Spacing to Justified (no ItemSpacing)
        layout.SetSpacing(SpacingMode.Justified);

        layout.Activate();
        layout.ApplyUpdates(0.0); // Apply deferred property changes
        layout.Update(0.016);

        // Assert
        var c1 = getConstraints1();
        var c2 = getConstraints2();

        // Total child height = 50, content height = 100
        // Spacing = (100 - 50) / 1 = 50
        Assert.Equal(0, c1.Origin.Y);  // First at top
        Assert.Equal(20, c1.Size.Y);
        Assert.Equal(70, c2.Origin.Y); // Second at 20 + 50 = 70
        Assert.Equal(30, c2.Size.Y);
    }

    [Fact]
    public void Spacing_Distributed_DistributesSpaceEvenly()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child1, getConstraints1) = CreateMockElement(new Vector2D<int>(100, 20));
        var (child2, getConstraints2) = CreateMockElement(new Vector2D<int>(100, 30));

        layout.AddChild((IComponent)child1.Object);
        layout.AddChild((IComponent)child2.Object);

        layout.SetSize(new Vector2D<int>(100, 100));
        layout.SetAnchorPoint(new Vector2D<float>(-1, -1));
        layout.SetAlignment(new Vector2D<float>(0, -1));

        // Set Spacing to Distributed (no ItemSpacing)
        layout.SetSpacing(SpacingMode.Distributed);

        layout.Activate();
        layout.ApplyUpdates(0.0); // Apply deferred property changes
        layout.Update(0.016);

        // Assert
        var c1 = getConstraints1();
        var c2 = getConstraints2();

        // Total child height = 50, content height = 100
        // Spacing = (100 - 50) / 3 ≈ 16.67
        Assert.Equal(16, c1.Origin.Y);  // First at spacing
        Assert.Equal(20, c1.Size.Y);
        Assert.Equal(16 + 20 + 16, c2.Origin.Y); // 52
        Assert.Equal(30, c2.Size.Y);
    }

    [Fact]
    public void Spacing_SingleChild_TopLeftAlignment()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child, getConstraints) = CreateMockElement(new Vector2D<int>(100, 50));

        // Top-left corner will be at (0,0)
        layout.Load(
            size: new Vector2D<int>(800,600),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            alignContent: Align.Top,
            spacing: SpacingMode.Justified
        );

        layout.AddChild(child.Object);

        // Act
        layout.Activate();

        // Assert
        var layoutBounds = layout.GetBounds();
        var c1 = getConstraints();
        Assert.Equal(0, c1.Origin.Y);
        Assert.Equal(50, c1.Size.Y);
    }

    [Fact]
    public void Spacing_SingleChild_MiddleCenterAlignment()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child, getConstraints) = CreateMockElement(new Vector2D<int>(100, 50));

        // Act

        // Middle-center will be at (0,0)
        layout.Load(
            size: new Vector2D<int>(800,600),
            anchorPoint: Align.MiddleCenter,
            alignment: Align.MiddleCenter,
            alignContent: Align.Middle,
            spacing: SpacingMode.Justified
        );
        layout.AddChild(child.Object);
        layout.Activate();

        // Assert
        var layoutBounds = layout.GetBounds();
        var c1 = getConstraints();
        Assert.Equal(-25, c1.Origin.Y);
        Assert.Equal(50, c1.Size.Y);
    }

    [Fact]
    public void Spacing_SingleChild_BottomRightAlignment()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child, getConstraints) = CreateMockElement(new Vector2D<int>(100, 50));

        // Bottom-right corner will be at (0,0)
        layout.Load(
            size: new Vector2D<int>(800,600),
            anchorPoint: Align.BottomRight,
            alignment: Align.BottomRight,
            alignContent: Align.Bottom,
            spacing: SpacingMode.Justified
        );

        layout.AddChild(child.Object);

        // Act
        layout.Activate();

        // Assert
        var layoutBounds = layout.GetBounds();
        var c1 = getConstraints();
        Assert.Equal(-50, c1.Origin.Y);
        Assert.Equal(50, c1.Size.Y);
    }

    [Fact]
    public void ItemHeight_OverridesChildMeasuredHeight()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child1, getC1) = CreateMockElement(new Vector2D<int>(100, 20)); // 20px measured
        var (child2, getC2) = CreateMockElement(new Vector2D<int>(100, 40)); // 40px measured

        layout.Load(
            size: new Vector2D<int>(200, 300),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            alignContent: Align.Top,
            itemHeight: 60 // Force all children to 60px
        );

        layout.AddChild(child1.Object);
        layout.AddChild(child2.Object);

        // Act
        layout.Activate();

        // Assert - both children should have 60px height regardless of measured size
        var c1 = getC1();
        var c2 = getC2();
        
        Assert.Equal(60, c1.Size.Y);
        Assert.Equal(60, c2.Size.Y);
        Assert.Equal(0, c1.Origin.Y);
        Assert.Equal(60, c2.Origin.Y); // Second child starts after first (no spacing)
    }

    [Fact]
    public void ItemHeight_WithItemSpacing_CreatesUniformLayout()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child1, getC1) = CreateMockElement(new Vector2D<int>(100, 15));
        var (child2, getC2) = CreateMockElement(new Vector2D<int>(100, 25));
        var (child3, getC3) = CreateMockElement(new Vector2D<int>(100, 35));

        layout.Load(
            size: new Vector2D<int>(200, 300),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            alignContent: Align.Top,
            itemHeight: 50,
            itemSpacing: 10
        );

        layout.AddChild(child1.Object);
        layout.AddChild(child2.Object);
        layout.AddChild(child3.Object);

        // Act
        layout.Activate();

        // Assert - uniform 50px heights with 10px spacing
        var c1 = getC1();
        var c2 = getC2();
        var c3 = getC3();
        
        Assert.Equal(50, c1.Size.Y);
        Assert.Equal(50, c2.Size.Y);
        Assert.Equal(50, c3.Size.Y);
        Assert.Equal(0, c1.Origin.Y);
        Assert.Equal(60, c2.Origin.Y);   // 50 + 10
        Assert.Equal(120, c3.Origin.Y);  // 110 + 10
    }

    [Fact]
    public void ItemHeight_VaryingChildSizes_AllForcedToSameHeight()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (small, getSmall) = CreateMockElement(new Vector2D<int>(100, 10));
        var (medium, getMedium) = CreateMockElement(new Vector2D<int>(100, 30));
        var (large, getLarge) = CreateMockElement(new Vector2D<int>(100, 80));

        layout.Load(
            size: new Vector2D<int>(200, 400),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            itemHeight: 40
        );

        layout.AddChild(small.Object);
        layout.AddChild(medium.Object);
        layout.AddChild(large.Object);

        // Act
        layout.Activate();

        // Assert - all forced to 40px despite different measured sizes
        var s = getSmall();
        var m = getMedium();
        var l = getLarge();
        
        Assert.Equal(40, s.Size.Y);
        Assert.Equal(40, m.Size.Y);
        Assert.Equal(40, l.Size.Y);
    }

    [Fact]
    public void ItemHeight_TakesPrecedenceOver_ChildMeasure()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child, getConstraints) = CreateMockElement(new Vector2D<int>(150, 100)); // Child says 100px

        layout.Load(
            size: new Vector2D<int>(200, 300),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            itemHeight: 25 // Layout overrides to 25px
        );

        layout.AddChild(child.Object);

        // Act
        layout.Activate();

        // Assert - layout's ItemHeight wins
        var c = getConstraints();
        Assert.Equal(25, c.Size.Y);
    }

    [Fact]
    public void UpdateLayout_WithEmptyChildrenCollection_DoesNotThrow()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);

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
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child, getConstraints) = CreateMockElement(new Vector2D<int>(100, 50));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            alignContent: -1.0f // Top align
        );

        layout.AddChild(child.Object);

        // Act
        layout.Activate();

        // Assert - single child positioned at top (alignContent = -1)
        var c = getConstraints();
        Assert.Equal(0, c.Origin.Y);
    }

    [Fact]
    public void Justified_WithTwoElements_OneZeroHeight_CentersRemainingElement()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (visible, getVisible) = CreateMockElement(new Vector2D<int>(100, 50));
        var (zeroHeight, getZero) = CreateMockElement(new Vector2D<int>(100, 0));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            spacing: SpacingMode.Justified
        );

        layout.AddChild(visible.Object);
        layout.AddChild(zeroHeight.Object);

        // Act
        layout.Activate();

        // Assert - zero-height child is ignored, single visible child uses AlignContent (default 0 = center)
        var v = getVisible();
        Assert.Equal(75, v.Origin.Y); // Centered: (200 - 50) / 2 = 75
    }

    [Fact]
    public void Justified_WithMultipleElements_OneZeroHeight_SpacingNotDoubled()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child1, get1) = CreateMockElement(new Vector2D<int>(100, 40));
        var (zeroHeight, getZero) = CreateMockElement(new Vector2D<int>(100, 0));
        var (child2, get2) = CreateMockElement(new Vector2D<int>(100, 40));
        var (child3, get3) = CreateMockElement(new Vector2D<int>(100, 40));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            spacing: SpacingMode.Justified
        );

        layout.AddChild(child1.Object);
        layout.AddChild(zeroHeight.Object); // Should be completely ignored
        layout.AddChild(child2.Object);
        layout.AddChild(child3.Object);

        // Act
        layout.Activate();

        // Assert - zero-height child ignored, spacing calculated for 3 visible children only
        // Total height: 3 * 40 = 120, remaining: 200 - 120 = 80
        // Justified spacing: 80 / 2 gaps = 40 per gap
        var c1 = get1();
        var c2 = get2();
        var c3 = get3();
        
        Assert.Equal(0, c1.Origin.Y);
        Assert.Equal(80, c2.Origin.Y);   // 40 (child1) + 40 (gap)
        Assert.Equal(160, c3.Origin.Y);  // 120 (child1+child2) + 40 (gap)
    }

    [Fact]
    public void Distributed_WithMultipleElements_OneZeroHeight_SpacingNotDoubled()
    {
        // Arrange
        var layout = new VerticalLayout(_descriptorManagerMock.Object);
        var (child1, get1) = CreateMockElement(new Vector2D<int>(100, 30));
        var (zeroHeight, getZero) = CreateMockElement(new Vector2D<int>(100, 0));
        var (child2, get2) = CreateMockElement(new Vector2D<int>(100, 30));

        layout.Load(
            size: new Vector2D<int>(200, 200),
            anchorPoint: Align.TopLeft,
            alignment: Align.TopLeft,
            spacing: SpacingMode.Distributed
        );

        layout.AddChild(child1.Object);
        layout.AddChild(zeroHeight.Object); // Should be completely ignored
        layout.AddChild(child2.Object);

        // Act
        layout.Activate();

        // Assert - zero-height child ignored, spacing calculated for 2 visible children only
        // Total height: 2 * 30 = 60, remaining: 200 - 60 = 140
        // Distributed spacing: 140 / 3 spaces = 46.666... per space
        var c1 = get1();
        var c2 = get2();
        
        Assert.Equal(46, c1.Origin.Y);   // First space (rounded down)
        Assert.Equal(122, c2.Origin.Y);  // 46 (space) + 30 (child1) + 46 (space, rounded down)
    }
}