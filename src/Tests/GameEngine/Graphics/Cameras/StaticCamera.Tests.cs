using Moq;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Buffers;
using Nexus.GameEngine.Graphics.Cameras;
using Nexus.GameEngine.Graphics.Descriptors;
using Silk.NET.Maths;

namespace Tests.GameEngine.Graphics.Cameras;

public class StaticCameraTests
{
    [Fact]
    public void GetViewProjectionMatrix_WithIdentityView_ReturnsProjection()
    {
        // Arrange - create mocks for dependencies; StaticCamera won't use them for matrix math
        var camera = new StaticCamera();

        // Act - initialize matrices (sets View=Identity and builds Projection)
        camera.SetViewportSize(800f, 600f);

        var projection = camera.ProjectionMatrix;
        var vp = camera.GetViewProjectionMatrix();

        // Assert - with identity view, viewProjection should equal projection
        Assert.Equal(projection, vp);
    }


    [Fact]
    public void ViewMatrix_IsIdentity_AfterInitialize()
    {
        // Arrange
        var camera = new StaticCamera();

        // Act
        camera.SetViewportSize(800f, 600f);

        // Assert - ViewMatrix should be identity for StaticCamera
        Assert.Equal(Matrix4X4<float>.Identity, camera.ViewMatrix);
    }

    [Fact]
    public void SetViewportSize_UpdatesProjection_And_ViewProjection()
    {
        // Arrange
        var mockBufferManager = new Mock<IBufferManager>();
        var mockDescriptorManager = new Mock<IDescriptorManager>();
        var mockGraphicsContext = new Mock<IGraphicsContext>();

        var camera = new StaticCamera();
        camera.SetViewportSize(800f, 600f);

        var initialProjection = camera.ProjectionMatrix;
        var initialVP = camera.GetViewProjectionMatrix();

        // Act - change viewport size
        camera.SetViewportSize(1024f, 768f);
        var updatedProjection = camera.ProjectionMatrix;
        var updatedVP = camera.GetViewProjectionMatrix();

        // Assert - projection and viewProjection should update
        Assert.NotEqual(initialProjection, updatedProjection);
        Assert.NotEqual(initialVP, updatedVP);
        // With identity view, viewProjection should equal projection
        Assert.Equal(updatedProjection, updatedVP);
    }

    [Fact]
    public void IsVisible_ReturnsTrue_WhenBoxOverlapsViewport()
    {
        // Arrange
        var camera = new StaticCamera();
        camera.SetViewportSize(100f, 100f);

        // Build a Box3D<float> that lies within the viewport (10..20 in both X and Y)
        var box = new Box3D<float>(10f, 10f, 0f, 20f, 20f, 1f);

        // Act
        bool visible = camera.IsVisible(box);

        // Assert
        Assert.True(visible);
    }

    [Fact]
    public void IsVisible_ReturnsFalse_WhenBoxOutsideViewport()
    {
        // Arrange
        var camera = new StaticCamera();
        camera.SetViewportSize(100f, 100f);

        // Build a Box3D<float> that lies completely to the right of the viewport (200..300 in X)
        var box = new Box3D<float>(200f, 10f, 0f, 300f, 20f, 1f);

        // Act
        bool visible = camera.IsVisible(box);

        // Assert
        Assert.False(visible);
    }
}