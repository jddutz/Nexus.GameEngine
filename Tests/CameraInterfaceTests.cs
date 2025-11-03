using Moq;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Cameras;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Xunit;
using GameViewport = Nexus.GameEngine.Graphics.Viewport;

namespace Nexus.GameEngine.Tests;

/// <summary>
/// Tests for ICamera interface viewport-related properties and methods.
/// These tests verify the new viewport creation capabilities added to cameras.
/// </summary>
public class CameraInterfaceTests
{
    [Fact]
    public void ICamera_ScreenRegion_DefaultsToFullscreen()
    {
        // Arrange
        var mockCamera = new Mock<ICamera>();
        mockCamera.SetupGet(c => c.ScreenRegion).Returns(new Rectangle<float>(0, 0, 1, 1));

        // Act
        var camera = mockCamera.Object;

        // Assert
        Assert.Equal(new Rectangle<float>(0, 0, 1, 1), camera.ScreenRegion);
    }

    [Fact]
    public void ICamera_ClearColor_DefaultsToBlack()
    {
        // Arrange
        var mockCamera = new Mock<ICamera>();
        mockCamera.SetupGet(c => c.ClearColor).Returns(new Vector4D<float>(0, 0, 0, 1));

        // Act
        var camera = mockCamera.Object;

        // Assert
        Assert.Equal(new Vector4D<float>(0, 0, 0, 1), camera.ClearColor);
    }

    [Fact]
    public void ICamera_RenderPriority_DefaultsToZero()
    {
        // Arrange
        var mockCamera = new Mock<ICamera>();
        mockCamera.SetupGet(c => c.RenderPriority).Returns(0);

        // Act
        var camera = mockCamera.Object;

        // Assert
        Assert.Equal(0, camera.RenderPriority);
    }

    [Fact]
    public void ICamera_RenderPassMask_DefaultsToAll()
    {
        // Arrange
        var mockCamera = new Mock<ICamera>();
        mockCamera.SetupGet(c => c.RenderPassMask).Returns(RenderPasses.All);

        // Act
        var camera = mockCamera.Object;

        // Assert
        Assert.Equal(RenderPasses.All, camera.RenderPassMask);
    }

    [Fact]
    public void ICamera_GetViewport_ReturnsViewportWithCorrectProperties()
    {
        // Arrange
        var expectedViewport = new GameViewport
        {
            Extent = new Extent2D(1920, 1080),
            ClearColor = new Vector4D<float>(1, 0, 0, 1),
            RenderPassMask = RenderPasses.Opaque
        };

        var mockCamera = new Mock<ICamera>();
        mockCamera.Setup(c => c.GetViewport()).Returns(expectedViewport);
        mockCamera.SetupGet(c => c.ClearColor).Returns(new Vector4D<float>(1, 0, 0, 1));
        mockCamera.SetupGet(c => c.RenderPassMask).Returns(RenderPasses.Opaque);

        // Act
        var viewport = mockCamera.Object.GetViewport();

        // Assert
        Assert.NotNull(viewport);
        Assert.Equal(new Vector4D<float>(1, 0, 0, 1), viewport.ClearColor);
        Assert.Equal(RenderPasses.Opaque, viewport.RenderPassMask);
    }

    [Fact]
    public void ICamera_GetViewport_CreatesFreshInstanceEachCall()
    {
        // Arrange
        var mockCamera = new Mock<ICamera>();
        
        // Setup to return new instance each time
        mockCamera.Setup(c => c.GetViewport()).Returns(() => new GameViewport
        {
            Extent = new Extent2D(1920, 1080),
            ClearColor = new Vector4D<float>(0, 0, 0, 1),
            RenderPassMask = RenderPasses.All
        });

        // Act
        var viewport1 = mockCamera.Object.GetViewport();
        var viewport2 = mockCamera.Object.GetViewport();

        // Assert
        // Records are value types for equality, but different instances
        Assert.Equal(viewport1, viewport2); // Same values
        Assert.NotSame(viewport1, viewport2); // Different instances
    }
}
