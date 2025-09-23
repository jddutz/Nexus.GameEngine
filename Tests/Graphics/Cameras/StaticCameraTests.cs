using Silk.NET.Maths;
using Nexus.GameEngine.Graphics.Cameras;

namespace Tests.Graphics.Cameras;

public class StaticCameraTests
{
    [Fact]
    public void Constructor_SetsFixedValues()
    {
        // Arrange & Act
        var camera = new StaticCamera();

        // Assert
        Assert.Equal(new Vector3D<float>(0, 0, 100000f), camera.Position);
        Assert.Equal(-Vector3D<float>.UnitZ, camera.Forward);
        Assert.Equal(Vector3D<float>.UnitY, camera.Up);
        Assert.Equal(Vector3D<float>.UnitX, camera.Right);
    }

    [Fact]
    public void Position_IsReadOnly()
    {
        // Arrange
        var camera = new StaticCamera();
        var expectedPosition = new Vector3D<float>(0, 0, 100000f);

        // Act & Assert
        Assert.Equal(expectedPosition, camera.Position);
        // Position should be read-only (no setter available)
    }

    [Fact]
    public void OrientationVectors_AreFixed()
    {
        // Arrange
        var camera = new StaticCamera();

        // Act & Assert
        Assert.Equal(-Vector3D<float>.UnitZ, camera.Forward);
        Assert.Equal(Vector3D<float>.UnitY, camera.Up);
        Assert.Equal(Vector3D<float>.UnitX, camera.Right);
    }

    [Fact]
    public void ViewMatrix_IsInitialized()
    {
        // Arrange
        var camera = new StaticCamera();

        // Act
        var viewMatrix = camera.ViewMatrix;

        // Assert
        Assert.NotEqual(Matrix4X4<float>.Identity, viewMatrix);
        Assert.NotEqual(default(Matrix4X4<float>), viewMatrix);
    }

    [Fact]
    public void ProjectionMatrix_IsInitialized()
    {
        // Arrange
        var camera = new StaticCamera();

        // Act
        var projectionMatrix = camera.ProjectionMatrix;

        // Assert
        Assert.NotEqual(Matrix4X4<float>.Identity, projectionMatrix);
        Assert.NotEqual(default(Matrix4X4<float>), projectionMatrix);
    }

    [Fact]
    public void IsVisible_AlwaysReturnsTrue()
    {
        // Arrange
        var camera = new StaticCamera();

        var bounds1 = new Box3D<float>(
            new Vector3D<float>(-1000, -1000, -1000),
            new Vector3D<float>(1000, 1000, 1000)
        );

        var bounds2 = new Box3D<float>(
            new Vector3D<float>(0, 0, 0),
            new Vector3D<float>(1, 1, 1)
        );

        var bounds3 = new Box3D<float>(
            new Vector3D<float>(-50000, -50000, -50000),
            new Vector3D<float>(50000, 50000, 50000)
        );

        // Act & Assert
        Assert.True(camera.IsVisible(bounds1));
        Assert.True(camera.IsVisible(bounds2));
        Assert.True(camera.IsVisible(bounds3));
    }

    [Fact]
    public void ScreenToWorldRay_CenterOfScreen_ReturnsValidRay()
    {
        // Arrange
        var camera = new StaticCamera();
        var screenWidth = 1920;
        var screenHeight = 1080;
        var centerPoint = new Vector2D<int>(screenWidth / 2, screenHeight / 2);

        // Act
        var ray = camera.ScreenToWorldRay(centerPoint, screenWidth, screenHeight);

        // Assert
        // Ray direction should be forward
        Assert.Equal(camera.Forward, ray.Direction);
        // Ray origin should be at camera position for center point
        Assert.True(Vector3D.Distance(ray.Origin, camera.Position) < 1f);
    }

    [Fact]
    public void ScreenToWorldRay_TopLeftCorner_ReturnsValidRay()
    {
        // Arrange
        var camera = new StaticCamera();
        var screenWidth = 1920;
        var screenHeight = 1080;
        var topLeftPoint = new Vector2D<int>(0, 0);

        // Act
        var ray = camera.ScreenToWorldRay(topLeftPoint, screenWidth, screenHeight);

        // Assert
        // Ray direction should always be forward for UI camera
        Assert.Equal(camera.Forward, ray.Direction);
        // Ray origin should be offset from camera position
        Assert.NotEqual(camera.Position, ray.Origin);
    }

    [Fact]
    public void ScreenToWorldRay_BottomRightCorner_ReturnsValidRay()
    {
        // Arrange
        var camera = new StaticCamera();
        var screenWidth = 1920;
        var screenHeight = 1080;
        var bottomRightPoint = new Vector2D<int>(screenWidth - 1, screenHeight - 1);

        // Act
        var ray = camera.ScreenToWorldRay(bottomRightPoint, screenWidth, screenHeight);

        // Assert
        // Ray direction should always be forward for UI camera
        Assert.Equal(camera.Forward, ray.Direction);
        // Ray origin should be offset from camera position
        Assert.NotEqual(camera.Position, ray.Origin);
    }

    [Fact]
    public void WorldToScreenPoint_PointAtCameraPosition_ReturnsCenterScreen()
    {
        // Arrange
        var camera = new StaticCamera();
        var worldPoint = camera.Position;
        var screenWidth = 1920;
        var screenHeight = 1080;

        // Act
        var screenPoint = camera.WorldToScreenPoint(worldPoint, screenWidth, screenHeight);

        // Assert
        // Point at camera position should project to center of screen
        Assert.True(Math.Abs(screenPoint.X - screenWidth / 2) < 50);
        Assert.True(Math.Abs(screenPoint.Y - screenHeight / 2) < 50);
    }

    [Fact]
    public void WorldToScreenPoint_PointToTheRight_ReturnsRightSideOfScreen()
    {
        // Arrange
        var camera = new StaticCamera();
        var worldPoint = camera.Position + camera.Right * 50000f; // Move right
        var screenWidth = 1920;
        var screenHeight = 1080;

        // Act
        var screenPoint = camera.WorldToScreenPoint(worldPoint, screenWidth, screenHeight);

        // Assert
        // Point to the right should be on the right side of screen
        Assert.True(screenPoint.X > screenWidth / 2);
    }

    [Fact]
    public void WorldToScreenPoint_PointAbove_ReturnsTopOfScreen()
    {
        // Arrange
        var camera = new StaticCamera();
        var worldPoint = camera.Position + camera.Up * 50000f; // Move up
        var screenWidth = 1920;
        var screenHeight = 1080;

        // Act
        var screenPoint = camera.WorldToScreenPoint(worldPoint, screenWidth, screenHeight);

        // Assert
        // Point above should be on the top side of screen (lower Y coordinate)
        Assert.True(screenPoint.Y < screenHeight / 2);
    }

    [Fact]
    public void Matrices_AreConsistent()
    {
        // Arrange
        var camera = new StaticCamera();

        // Act
        var viewMatrix1 = camera.ViewMatrix;
        var projectionMatrix1 = camera.ProjectionMatrix;

        // Call again to ensure no unexpected changes
        var viewMatrix2 = camera.ViewMatrix;
        var projectionMatrix2 = camera.ProjectionMatrix;

        // Assert
        Assert.Equal(viewMatrix1, viewMatrix2);
        Assert.Equal(projectionMatrix1, projectionMatrix2);
    }

    [Fact]
    public void ScreenToWorldRay_DifferentScreenSizes_ReturnsConsistentResults()
    {
        // Arrange
        var camera = new StaticCamera();

        // Test different screen sizes
        var smallScreen = new { Width = 800, Height = 600 };
        var largeScreen = new { Width = 3840, Height = 2160 };

        var centerPointSmall = new Vector2D<int>(smallScreen.Width / 2, smallScreen.Height / 2);
        var centerPointLarge = new Vector2D<int>(largeScreen.Width / 2, largeScreen.Height / 2);

        // Act
        var raySmall = camera.ScreenToWorldRay(centerPointSmall, smallScreen.Width, smallScreen.Height);
        var rayLarge = camera.ScreenToWorldRay(centerPointLarge, largeScreen.Width, largeScreen.Height);

        // Assert
        // Center rays should have the same direction regardless of screen size
        Assert.Equal(raySmall.Direction, rayLarge.Direction);
        // Origins should be very close (at camera position for center points)
        Assert.True(Vector3D.Distance(raySmall.Origin, rayLarge.Origin) < 1f);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(960, 540)]
    [InlineData(1920, 1080)]
    public void ScreenToWorldRay_ValidScreenCoordinates_NeverThrows(int x, int y)
    {
        // Arrange
        var camera = new StaticCamera();
        var screenPoint = new Vector2D<int>(x, y);
        var screenWidth = 1920;
        var screenHeight = 1080;

        // Act & Assert
        var ray = camera.ScreenToWorldRay(screenPoint, screenWidth, screenHeight);

        // Should not throw and should return a valid ray
        Assert.NotEqual(Vector3D<float>.Zero, ray.Direction);
        Assert.NotEqual(default(Vector3D<float>), ray.Origin);
    }
}