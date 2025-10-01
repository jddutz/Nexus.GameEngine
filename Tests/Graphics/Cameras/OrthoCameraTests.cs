using Silk.NET.Maths;
using Nexus.GameEngine.Graphics.Cameras;

namespace Tests.Graphics.Cameras;

public class OrthoCameraTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var camera = new OrthoCamera();

        // Assert
        Assert.Equal(10f, camera.Width, 0.001f);
        Assert.Equal(10f, camera.Height, 0.001f);
        Assert.Equal(-1000f, camera.NearPlane, 0.001f);
        Assert.Equal(1000f, camera.FarPlane, 0.001f);
        Assert.Equal(Vector3D<float>.Zero, camera.Position);
        Assert.Equal(-Vector3D<float>.UnitZ, camera.Forward);
        Assert.Equal(Vector3D<float>.UnitY, camera.Up);
        Assert.Equal(Vector3D<float>.UnitX, camera.Right);
    }

    [Fact]
    public void Width_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new OrthoCamera();
        var newWidth = 20f;

        // Act
        camera.SetWidth(newWidth);
        camera.ApplyUpdates();

        // Assert
        Assert.Equal(newWidth, camera.Width, 0.001f);
    }

    [Fact]
    public void Height_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new OrthoCamera();
        var newHeight = 15f;

        // Act
        camera.SetHeight(newHeight);
        camera.ApplyUpdates();

        // Assert
        Assert.Equal(newHeight, camera.Height, 0.001f);
    }

    [Fact]
    public void NearPlane_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new OrthoCamera();
        var newNear = -500f;

        // Act
        camera.SetNearPlane(newNear);
        camera.ApplyUpdates();

        // Assert
        Assert.Equal(newNear, camera.NearPlane, 0.001f);
    }

    [Fact]
    public void FarPlane_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new OrthoCamera();
        var newFar = 2000f;

        // Act
        camera.SetFarPlane(newFar);
        camera.ApplyUpdates();

        // Assert
        Assert.Equal(newFar, camera.FarPlane, 0.001f);
    }

    [Fact]
    public void Position_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new OrthoCamera();
        var newPosition = new Vector3D<float>(5, 10, 15);

        // Act
        camera.SetPosition(newPosition);
        camera.ApplyUpdates();

        // Assert
        Assert.Equal(newPosition, camera.Position);
    }

    [Fact]
    public void OrientationVectors_AreFixed()
    {
        // Arrange
        var camera = new OrthoCamera();

        // Act & Assert
        Assert.Equal(-Vector3D<float>.UnitZ, camera.Forward);
        Assert.Equal(Vector3D<float>.UnitY, camera.Up);
        Assert.Equal(Vector3D<float>.UnitX, camera.Right);
    }

    [Fact]
    public void StaticAxialDirections_ReturnCorrectValues()
    {
        // Act & Assert
        Assert.Equal(-Vector3D<float>.UnitZ, OrthoCamera.AxialForward);
        Assert.Equal(Vector3D<float>.UnitY, OrthoCamera.AxialUp);
        Assert.Equal(Vector3D<float>.UnitX, OrthoCamera.AxialRight);
    }

    [Fact]
    public void ViewMatrix_ReturnsValidMatrix()
    {
        // Arrange
        var camera = new OrthoCamera();
        camera.SetPosition(new Vector3D<float>(0, 0, 5));
        camera.ApplyUpdates();

        // Act
        var viewMatrix = camera.ViewMatrix;

        // Assert
        Assert.NotEqual(Matrix4X4<float>.Identity, viewMatrix);
    }

    [Fact]
    public void ProjectionMatrix_ReturnsValidMatrix()
    {
        // Arrange
        var camera = new OrthoCamera();

        // Act
        var projectionMatrix = camera.ProjectionMatrix;

        // Assert
        Assert.NotEqual(Matrix4X4<float>.Identity, projectionMatrix);
    }

    [Fact]
    public void IsVisible_ObjectWithinBounds_ReturnsTrue()
    {
        // Arrange
        var camera = new OrthoCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetWidth(10f);
        camera.SetHeight(10f);
        camera.ApplyUpdates();

        var bounds = new Box3D<float>(
            new Vector3D<float>(-2, -2, -5),
            new Vector3D<float>(2, 2, 5)
        );

        // Act
        var isVisible = camera.IsVisible(bounds);

        // Assert
        Assert.True(isVisible);
    }

    [Fact]
    public void IsVisible_ObjectOutsideHorizontalBounds_ReturnsFalse()
    {
        // Arrange
        var camera = new OrthoCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetWidth(10f);
        camera.SetHeight(10f);
        camera.ApplyUpdates();

        var bounds = new Box3D<float>(
            new Vector3D<float>(20, -2, -5),
            new Vector3D<float>(25, 2, 5)
        );

        // Act
        var isVisible = camera.IsVisible(bounds);

        // Assert
        Assert.False(isVisible);
    }

    [Fact]
    public void IsVisible_ObjectOutsideVerticalBounds_ReturnsFalse()
    {
        // Arrange
        var camera = new OrthoCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetWidth(10f);
        camera.SetHeight(10f);
        camera.ApplyUpdates();

        var bounds = new Box3D<float>(
            new Vector3D<float>(-2, 20, -5),
            new Vector3D<float>(2, 25, 5)
        );

        // Act
        var isVisible = camera.IsVisible(bounds);

        // Assert
        Assert.False(isVisible);
    }

    [Fact]
    public void IsVisible_ObjectBeyondFarPlane_ReturnsFalse()
    {
        // Arrange
        var camera = new OrthoCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetFarPlane(100f);
        camera.ApplyUpdates();

        var bounds = new Box3D<float>(
            new Vector3D<float>(-2, -2, -200),
            new Vector3D<float>(2, 2, -150)
        );

        // Act
        var isVisible = camera.IsVisible(bounds);

        // Assert
        Assert.False(isVisible);
    }

    [Fact]
    public void ScreenToWorldRay_CenterOfScreen_ReturnsRayAtCameraCenter()
    {
        // Arrange
        var camera = new OrthoCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetWidth(10f);
        camera.SetHeight(10f);
        camera.ApplyUpdates();

        var screenWidth = 800;
        var screenHeight = 600;
        var centerPoint = new Vector2D<int>(screenWidth / 2, screenHeight / 2);

        // Act
        var ray = camera.ScreenToWorldRay(centerPoint, screenWidth, screenHeight);

        // Assert
        // For orthographic projection, ray origin should be at camera position for center screen point
        Assert.True(Vector3D.Distance(ray.Origin, camera.Position) < 0.1f);
        // Ray direction should be forward
        Assert.Equal(camera.Forward, ray.Direction);
    }

    [Fact]
    public void ScreenToWorldRay_CornerOfScreen_ReturnsRayAtCorrectPosition()
    {
        // Arrange
        var camera = new OrthoCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetWidth(10f);
        camera.SetHeight(10f);
        camera.ApplyUpdates();

        var screenWidth = 800;
        var screenHeight = 600;
        var topLeftPoint = new Vector2D<int>(0, 0);

        // Act
        var ray = camera.ScreenToWorldRay(topLeftPoint, screenWidth, screenHeight);

        // Assert
        // Ray direction should always be forward for orthographic projection
        Assert.Equal(camera.Forward, ray.Direction);
        // Ray origin should be offset from camera position
        Assert.NotEqual(camera.Position, ray.Origin);
    }

    [Fact]
    public void WorldToScreenPoint_PointAtCameraPosition_ReturnsCenterScreen()
    {
        // Arrange
        var camera = new OrthoCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetWidth(10f);
        camera.SetHeight(10f);
        camera.ApplyUpdates();

        var worldPoint = camera.Position;
        var screenWidth = 800;
        var screenHeight = 600;

        // Act
        var screenPoint = camera.WorldToScreenPoint(worldPoint, screenWidth, screenHeight);

        // Assert
        // Point at camera position should project to center of screen
        Assert.True(Math.Abs(screenPoint.X - screenWidth / 2) < 10);
        Assert.True(Math.Abs(screenPoint.Y - screenHeight / 2) < 10);
    }

    [Fact]
    public void WorldToScreenPoint_PointToTheRight_ReturnsRightSideOfScreen()
    {
        // Arrange
        var camera = new OrthoCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetWidth(10f);
        camera.SetHeight(10f);
        camera.ApplyUpdates();

        var worldPoint = new Vector3D<float>(2.5f, 0, 0); // Quarter width to the right
        var screenWidth = 800;
        var screenHeight = 600;

        // Act
        var screenPoint = camera.WorldToScreenPoint(worldPoint, screenWidth, screenHeight);

        // Assert
        // Point to the right should be on the right side of screen
        Assert.True(screenPoint.X > screenWidth / 2);
        Assert.True(Math.Abs(screenPoint.Y - screenHeight / 2) < 10);
    }

    [Fact]
    public void PropertyChanges_InvalidateMatrices()
    {
        // Arrange
        var camera = new OrthoCamera();
        var originalViewMatrix = camera.ViewMatrix;
        var originalProjectionMatrix = camera.ProjectionMatrix;

        // Act - Change a property that should invalidate matrices
        camera.SetWidth(20f);
        camera.ApplyUpdates();

        // Assert - Projection matrix should be recalculated
        Assert.NotEqual(originalProjectionMatrix, camera.ProjectionMatrix);

        // Act - Change position
        camera.SetPosition(new Vector3D<float>(5, 10, 15));
        camera.ApplyUpdates();

        // Assert - View matrix should be recalculated
        Assert.NotEqual(originalViewMatrix, camera.ViewMatrix);
    }
}