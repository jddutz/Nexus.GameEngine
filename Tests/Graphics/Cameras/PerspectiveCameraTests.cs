using Silk.NET.Maths;
using Nexus.GameEngine.Graphics.Cameras;

namespace Nexus.GameEngine.Tests.Graphics.Cameras;

public class PerspectiveCameraTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var camera = new PerspectiveCamera();

        // Assert
        Assert.Equal(MathF.PI / 4, camera.FieldOfView, 0.001f);
        Assert.Equal(0.1f, camera.NearPlane, 0.001f);
        Assert.Equal(1000f, camera.FarPlane, 0.001f);
        Assert.Equal(16f / 9f, camera.AspectRatio, 0.001f);
        Assert.Equal(Vector3D<float>.Zero, camera.Position);
        Assert.Equal(-Vector3D<float>.UnitZ, camera.Forward);
        Assert.Equal(Vector3D<float>.UnitY, camera.Up);
        Assert.Equal(Vector3D<float>.UnitX, camera.Right);
    }

    [Fact]
    public void FieldOfView_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        var newFov = MathF.PI / 3; // 60 degrees

        // Act
        camera.SetFieldOfView(newFov);
        camera.ApplyUpdates(); // Apply deferred updates

        // Assert
        Assert.Equal(newFov, camera.FieldOfView, 0.001f);
    }

    [Fact]
    public void NearPlane_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        var newNear = 0.5f;

        // Act
        camera.SetNearPlane(newNear);
        camera.ApplyUpdates(); // Apply deferred updates

        // Assert
        Assert.Equal(newNear, camera.NearPlane, 0.001f);
    }

    [Fact]
    public void FarPlane_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        var newFar = 2000f;

        // Act
        camera.SetFarPlane(newFar);
        camera.ApplyUpdates(); // Apply deferred updates

        // Assert
        Assert.Equal(newFar, camera.FarPlane, 0.001f);
    }

    [Fact]
    public void AspectRatio_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        var newAspect = 4f / 3f;

        // Act
        camera.SetAspectRatio(newAspect);
        camera.ApplyUpdates(); // Apply deferred updates

        // Assert
        Assert.Equal(newAspect, camera.AspectRatio, 0.001f);
    }

    [Fact]
    public void Position_SetValue_UpdatesProperty()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        var newPosition = new Vector3D<float>(10, 20, 30);

        // Act
        camera.SetPosition(newPosition);
        camera.ApplyUpdates(); // Apply deferred updates

        // Assert
        Assert.Equal(newPosition, camera.Position);
    }

    [Fact]
    public void Forward_SetValue_UpdatesDirectionVectors()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        var newForward = Vector3D.Normalize(new Vector3D<float>(1, 0, -1));

        // Act
        camera.SetForward(newForward);
        camera.ApplyUpdates(); // Apply deferred updates

        // Assert
        // Use tolerance-based comparison for floating point values
        Assert.True(Math.Abs(camera.Forward.X - newForward.X) < 0.0001f);
        Assert.True(Math.Abs(camera.Forward.Y - newForward.Y) < 0.0001f);
        Assert.True(Math.Abs(camera.Forward.Z - newForward.Z) < 0.0001f);
        // Right should be perpendicular to forward and up
        Assert.True(Math.Abs(Vector3D.Dot(camera.Right, camera.Forward)) < 0.001f);
        Assert.True(Math.Abs(Vector3D.Dot(camera.Right, camera.Up)) < 0.001f);
    }

    [Fact]
    public void ViewMatrix_ReturnsValidMatrix()
    {
        // Arrange
        var camera = new PerspectiveCamera();
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
        var camera = new PerspectiveCamera();

        // Act
        var projectionMatrix = camera.ProjectionMatrix;

        // Assert
        Assert.NotEqual(Matrix4X4<float>.Identity, projectionMatrix);
    }

    [Fact]
    public void IsVisible_ObjectInFrontOfCamera_ReturnsTrue()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetForward(-Vector3D<float>.UnitZ);
        camera.ApplyUpdates();

        var bounds = new Box3D<float>(
            new Vector3D<float>(-1, -1, -10),
            new Vector3D<float>(1, 1, -5)
        );

        // Act
        var isVisible = camera.IsVisible(bounds);

        // Assert
        Assert.True(isVisible);
    }

    [Fact]
    public void IsVisible_ObjectBehindCamera_ReturnsFalse()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetForward(-Vector3D<float>.UnitZ);
        camera.ApplyUpdates();

        var bounds = new Box3D<float>(
            new Vector3D<float>(-1, -1, 5),
            new Vector3D<float>(1, 1, 10)
        );

        // Act
        var isVisible = camera.IsVisible(bounds);

        // Assert
        Assert.False(isVisible);
    }

    [Fact]
    public void IsVisible_ObjectTooFar_ReturnsFalse()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetForward(-Vector3D<float>.UnitZ);
        camera.SetFarPlane(100f);
        camera.ApplyUpdates();

        var bounds = new Box3D<float>(
            new Vector3D<float>(-1, -1, -200),
            new Vector3D<float>(1, 1, -150)
        );

        // Act
        var isVisible = camera.IsVisible(bounds);

        // Assert
        Assert.False(isVisible);
    }

    [Fact]
    public void ScreenToWorldRay_CenterOfScreen_ReturnsForwardRay()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetForward(-Vector3D<float>.UnitZ);
        camera.ApplyUpdates();

        var screenWidth = 800;
        var screenHeight = 600;
        var centerPoint = new Vector2D<int>(screenWidth / 2, screenHeight / 2);

        // Act
        var ray = camera.ScreenToWorldRay(centerPoint, screenWidth, screenHeight);

        // Assert
        Assert.Equal(camera.Position, ray.Origin);
        // Ray direction should be roughly forward (allowing for some numerical precision)
        var dot = Vector3D.Dot(Vector3D.Normalize(ray.Direction), camera.Forward);
        Assert.True(dot > 0.9f, $"Expected dot product > 0.9, got {dot}");
    }

    [Fact]
    public void WorldToScreenPoint_PointInFrontOfCamera_ReturnsValidScreenCoordinates()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        camera.SetPosition(Vector3D<float>.Zero);
        camera.SetForward(-Vector3D<float>.UnitZ);
        camera.ApplyUpdates();

        var worldPoint = new Vector3D<float>(0, 0, -10);
        var screenWidth = 800;
        var screenHeight = 600;

        // Act
        var screenPoint = camera.WorldToScreenPoint(worldPoint, screenWidth, screenHeight);

        // Assert
        Assert.True(screenPoint.X >= 0 && screenPoint.X <= screenWidth);
        Assert.True(screenPoint.Y >= 0 && screenPoint.Y <= screenHeight);
        // Point directly in front should be near center
        Assert.True(Math.Abs(screenPoint.X - screenWidth / 2) < 50);
        Assert.True(Math.Abs(screenPoint.Y - screenHeight / 2) < 50);
    }

    [Fact]
    public void PropertyChanges_InvalidateMatrices()
    {
        // Arrange
        var camera = new PerspectiveCamera();
        var originalViewMatrix = camera.ViewMatrix;
        var originalProjectionMatrix = camera.ProjectionMatrix;

        // Act - Change a property that should invalidate matrices
        camera.SetFieldOfView(MathF.PI / 6); // 30 degrees
        camera.ApplyUpdates();

        // Assert - Matrices should be recalculated
        Assert.NotEqual(originalProjectionMatrix, camera.ProjectionMatrix);

        // Act - Change position
        camera.SetPosition(new Vector3D<float>(10, 0, 0));
        camera.ApplyUpdates();

        // Assert - View matrix should be recalculated
        Assert.NotEqual(originalViewMatrix, camera.ViewMatrix);
    }
}