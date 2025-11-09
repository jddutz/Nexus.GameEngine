using Nexus.GameEngine.Graphics;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using GameViewport = Nexus.GameEngine.Graphics.Viewport;

namespace Tests;

public class ViewportTests
{
    [Fact]
    public void Viewport_Constructor_SetsAllProperties()
    {
        // Arrange
        var extent = new Extent2D(1920, 1080);
        var clearColor = new Vector4D<float>(0.1f, 0.2f, 0.3f, 1.0f);
        var renderPassMask = RenderPasses.Opaque;

        // Act
        var viewport = new GameViewport
        {
            Extent = extent,
            ClearColor = clearColor,
            RenderPassMask = renderPassMask
        };

        // Assert
        Assert.Equal(extent, viewport.Extent);
        Assert.Equal(clearColor, viewport.ClearColor);
        Assert.Equal(renderPassMask, viewport.RenderPassMask);
    }

    [Fact]
    public void Viewport_Equality_ComparesValues()
    {
        // Arrange
        var extent = new Extent2D(1920, 1080);
        var clearColor = new Vector4D<float>(0.1f, 0.2f, 0.3f, 1.0f);
        
        var viewport1 = new GameViewport
        {
            Extent = extent,
            ClearColor = clearColor,
            RenderPassMask = RenderPasses.All
        };
        
        var viewport2 = new GameViewport
        {
            Extent = extent,
            ClearColor = clearColor,
            RenderPassMask = RenderPasses.All
        };

        // Act & Assert - Records should have value equality
        Assert.Equal(viewport1, viewport2);
    }

    [Fact]
    public void Viewport_With_CreatesNewInstance()
    {
        // Arrange
        var original = new GameViewport
        {
            Extent = new Extent2D(1920, 1080),
            ClearColor = new Vector4D<float>(0.1f, 0.2f, 0.3f, 1.0f),
            RenderPassMask = RenderPasses.All
        };

        // Act - Use 'with' expression to create modified copy
        var modified = original with { ClearColor = new Vector4D<float>(1.0f, 0.0f, 0.0f, 1.0f) };

        // Assert
        Assert.NotEqual(original, modified);
        Assert.Equal(original.Extent, modified.Extent);
        Assert.Equal(original.RenderPassMask, modified.RenderPassMask);
        Assert.NotEqual(original.ClearColor, modified.ClearColor);
        Assert.Equal(new Vector4D<float>(1.0f, 0.0f, 0.0f, 1.0f), modified.ClearColor);
    }

    [Fact]
    public void Viewport_DefaultRenderPassMask_IsAll()
    {
        // Arrange & Act
        var viewport = new GameViewport
        {
            Extent = new Extent2D(800, 600),
            ClearColor = Vector4D<float>.Zero
        };

        // Assert
        Assert.Equal(RenderPasses.All, viewport.RenderPassMask);
    }
}
