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

namespace Tests.GameEngine.GUI.Layout;

/// <summary>
/// Unit tests for GridLayout component.
/// Tests grid arrangement, cell sizing, alignment, and spacing behavior.
/// </summary>
public class GridLayoutTests
{
    private readonly Mock<IDescriptorManager> _descriptorManagerMock;
    private readonly Mock<IResourceManager> _resourceManagerMock;
    private readonly Mock<IPipelineManager> _pipelineManagerMock;

    public GridLayoutTests()
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
    /// Creates a mock IUserInterfaceElement with a specific size.
    /// Returns both the mock and a function to retrieve the captured SetSizeConstraints argument.
    /// </summary>
    private static (Mock<IUserInterfaceElement> mock, Func<Rectangle<int>> getConstraints) CreateMockElement(Vector2D<int> size)
    {
        var mock = new Mock<IUserInterfaceElement>();
        Rectangle<int> capturedConstraints = default;

        mock.Setup(m => m.Measure(It.IsAny<Vector2D<int>>())).Returns(size);
        mock.Setup(m => m.SetSizeConstraints(It.IsAny<Rectangle<int>>()))
            .Callback<Rectangle<int>>(c => capturedConstraints = c);

        return (mock, () => capturedConstraints);
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var layout = new GridLayout(_descriptorManagerMock.Object);

        // Assert
        Assert.Equal(1, layout.ColumnCount);
        Assert.Equal(0, layout.RowCount);
        Assert.False(layout.UniformCellSize);
        Assert.False(layout.MaintainCellAspectRatio);
        Assert.Equal(1.0f, layout.CellAspectRatio);
        Assert.Equal(HorizontalAlignment.Center, layout.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Center, layout.VerticalAlignment);
    }

    [Fact]
    public void Properties_Work()
    {
        // Arrange
        var layout = new GridLayout(_descriptorManagerMock.Object);

        // Act
        layout.SetColumnCount(3);
        layout.SetRowCount(2);
        layout.SetUniformCellSize(true);
        layout.SetMaintainCellAspectRatio(true);
        layout.SetCellAspectRatio(1.5f);
        layout.SetHorizontalAlignment(HorizontalAlignment.Left);
        layout.SetVerticalAlignment(VerticalAlignment.Top);
        layout.ApplyUpdates(0.016f);

        // Assert
        Assert.Equal(3, layout.ColumnCount);
        Assert.Equal(2, layout.RowCount);
        Assert.True(layout.UniformCellSize);
        Assert.True(layout.MaintainCellAspectRatio);
        Assert.Equal(1.5f, layout.CellAspectRatio);
        Assert.Equal(HorizontalAlignment.Left, layout.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Top, layout.VerticalAlignment);
    }

    [Fact]
    public void NoChildren_DoesNothing()
    {
        // Arrange
        var layout = new GridLayout(_descriptorManagerMock.Object);
        layout.Load(size: new Vector2D<int>(200, 100));
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - No exceptions thrown
        Assert.True(true);
    }

    [Fact]
    public void SingleChild_FillsCell()
    {
        // Arrange
        var layout = new GridLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            columnCount: 2,
            rowCount: 2
        );

        var (childMock, getConstraints) = CreateMockElement(new Vector2D<int>(50, 30));
        layout.AddChild(childMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - Child should be positioned in first cell (0,0) and sized to cell
        var constraints = getConstraints();
        Assert.Equal(0, constraints.Origin.X);   // Cell starts at 0
        Assert.Equal(0, constraints.Origin.Y);   // Cell starts at 0
        Assert.Equal(100, constraints.Size.X);     // Cell width: 200/2 = 100
        Assert.Equal(50, constraints.Size.Y);      // Cell height: 100/2 = 50
    }

    [Fact]
    public void UniformCellSize_UsesLargestChild()
    {
        // Arrange
        var layout = new GridLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            columnCount: 2,
            uniformCellSize: true
        );

        var (child1Mock, getConstraints1) = CreateMockElement(new Vector2D<int>(30, 20));
        var (child2Mock, getConstraints2) = CreateMockElement(new Vector2D<int>(50, 40)); // Larger child

        layout.AddChild(child1Mock.Object);
        layout.AddChild(child2Mock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - Both children should be sized to largest (50x40)
        var constraints1 = getConstraints1();
        var constraints2 = getConstraints2();

        Assert.Equal(50, constraints1.Size.X);
        Assert.Equal(40, constraints1.Size.Y);
        Assert.Equal(50, constraints2.Size.X);
        Assert.Equal(40, constraints2.Size.Y);
    }

    [Fact]
    public void MaintainCellAspectRatio_CalculatesCorrectSize()
    {
        // Arrange
        var layout = new GridLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            columnCount: 2,
            maintainCellAspectRatio: true,
            cellAspectRatio: 2.0f // Width = 2 * Height
        );

        var (childMock, getConstraints) = CreateMockElement(new Vector2D<int>(50, 30));
        layout.AddChild(childMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - Cell width should be 200/2 = 100, height = 100/2.0 = 50
        var constraints = getConstraints();
        Assert.Equal(0, constraints.Origin.X);
        Assert.Equal(0, constraints.Origin.Y);
        Assert.Equal(100, constraints.Size.X); // Cell width
        Assert.Equal(50, constraints.Size.Y);  // Cell height based on aspect ratio
    }

    [Fact]
    public void AutoRowCount_CalculatesCorrectly()
    {
        // Arrange
        var layout = new GridLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(300, 200),
            columnCount: 3
            // RowCount = 0 means auto-calculate
        );

        // Add 7 children - should create 3 rows (3*2 = 6, need 3rd row for 7th child)
        var childMocks = new List<(Mock<IUserInterfaceElement>, Func<Rectangle<int>>)>();
        for (int i = 0; i < 7; i++)
        {
            var childMock = CreateMockElement(new Vector2D<int>(20, 20));
            childMocks.Add(childMock);
            layout.AddChild(childMock.mock.Object);
        }

        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - Check that children are positioned correctly across 3 rows
        var constraints = childMocks[6].Item2(); // Last child (index 6)

        // Should be in row 2 (0-indexed), column 0
        Assert.Equal(0, constraints.Origin.X);     // Column 0
        Assert.Equal(133, constraints.Origin.Y);   // Row 2: 2 * (66 + 1) ≈ 133 (200/3 ≈ 66.67)
    }

    [Fact]
    public void LeftTopAlignment_PositionsCorrectly()
    {
        // Arrange
        var layout = new GridLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            columnCount: 2,
            horizontalAlignment: HorizontalAlignment.Left,
            verticalAlignment: VerticalAlignment.Top
        );

        var (childMock, getConstraints) = CreateMockElement(new Vector2D<int>(30, 20)); // Smaller than cell
        layout.AddChild(childMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - Child should be positioned at top-left of its cell
        var constraints = getConstraints();
        Assert.Equal(0, constraints.Origin.X);   // Left of cell
        Assert.Equal(0, constraints.Origin.Y);   // Top of cell
        Assert.Equal(30, constraints.Size.X);      // Original size
        Assert.Equal(20, constraints.Size.Y);
    }

    [Fact]
    public void RightBottomAlignment_PositionsCorrectly()
    {
        // Arrange
        var layout = new GridLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            columnCount: 2,
            horizontalAlignment: HorizontalAlignment.Right,
            verticalAlignment: VerticalAlignment.Bottom
        );

        var (childMock, getConstraints) = CreateMockElement(new Vector2D<int>(30, 20)); // Smaller than cell
        layout.AddChild(childMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - Child should be positioned at bottom-right of its cell
        var constraints = getConstraints();
        Assert.Equal(70, constraints.Origin.X);  // Right of cell: 100 - 30 = 70
        Assert.Equal(30, constraints.Origin.Y);  // Bottom of cell: 50 - 20 = 30
        Assert.Equal(30, constraints.Size.X);
        Assert.Equal(20, constraints.Size.Y);
    }

    [Fact]
    public void StretchAlignment_FillsCell()
    {
        // Arrange
        var layout = new GridLayout(_descriptorManagerMock.Object);
        layout.Load(
            size: new Vector2D<int>(200, 100),
            columnCount: 2,
            horizontalAlignment: HorizontalAlignment.Stretch,
            verticalAlignment: VerticalAlignment.Stretch
        );

        var (childMock, getConstraints) = CreateMockElement(new Vector2D<int>(30, 20)); // Size shouldn't matter
        layout.AddChild(childMock.Object);
        layout.Activate();

        // Act
        layout.Update(0.016);

        // Assert - Child should fill entire cell
        var constraints = getConstraints();
        Assert.Equal(0, constraints.Origin.X);
        Assert.Equal(0, constraints.Origin.Y);
        Assert.Equal(100, constraints.Size.X); // Full cell width
        Assert.Equal(50, constraints.Size.Y);  // Full cell height
    }
}