using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Nexus.GameEngine.Performance;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Tests.GameEngine.Components;

/// <summary>
/// Tests for ContentManager camera tracking functionality.
/// Verifies automatic camera registration, default camera creation, and camera enumeration.
/// </summary>
public class ContentManagerCameraTests
{
    private static Mock<IComponentFactory> CreateMockFactory()
    {
        var mockCamera = new Mock<ICamera>();
        mockCamera.As<IRuntimeComponent>();
        
        var mockFactory = new Mock<IComponentFactory>();
        mockFactory.Setup(f => f.Create<StaticCamera>()).Returns(mockCamera.Object);
        
        return mockFactory;
    }

    private static Mock<IComponentFactory> CreateMockFactoryForInitialize()
    {
        var mockCamera = new Mock<ICamera>();
        mockCamera.As<IRuntimeComponent>()
            .Setup(c => c.IsActive()).Returns(true);
        mockCamera.As<IComponent>()
            .Setup(c => c.Parent).Returns((IComponent?)null);
        mockCamera.As<IComponent>()
            .Setup(c => c.IsLoaded).Returns(true);
        mockCamera.As<IComponent>()
            .Setup(c => c.Children).Returns(new List<IComponent>());
        mockCamera.Setup(c => c.RenderPriority).Returns(0);
        
        var mockFactory = new Mock<IComponentFactory>();
        // Mock the Create(Type) method which is what ContentManager.CreateInstance calls
        mockFactory.Setup(f => f.Create(typeof(StaticCamera)))
            .Returns(mockCamera.Object);
        
        return mockFactory;
    }

    private static Mock<ICamera> CreateMockCamera(string name, int renderPriority)
    {
        var mockCamera = new Mock<ICamera>();
        mockCamera.As<IRuntimeComponent>();
        mockCamera.As<IComponent>()
            .Setup(c => c.Name).Returns(name);
        mockCamera.Setup(c => c.RenderPriority).Returns(renderPriority);
        mockCamera.Setup(c => c.GetViewProjectionDescriptorSet()).Returns(default(DescriptorSet));
        return mockCamera;
    }

    [Fact]
    public void ContentManager_ActiveCameras_ReturnsEmptyBeforeInitialize()
    {
        // Arrange
        var mockFactory = CreateMockFactory();
        var mockProfiler = new Mock<IProfiler>();
        var contentManager = new ContentManager(mockFactory.Object, mockProfiler.Object);

        // Act
        var cameras = contentManager.ActiveCameras;

        // Assert
        Assert.NotNull(cameras);
        Assert.Empty(cameras);
    }

    [Fact]
    public void ContentManager_ActiveCameras_SortedByRenderPriority()
    {
        // Arrange
        var mockFactory = CreateMockFactory();
        var mockProfiler = new Mock<IProfiler>();
        var contentManager = new ContentManager(mockFactory.Object, mockProfiler.Object);
        
        // Create three mock cameras with different priorities (added in non-sorted order)
        var mockCamera1 = CreateMockCamera("Camera1", 10);
        var mockCamera2 = CreateMockCamera("Camera2", 5);
        var mockCamera3 = CreateMockCamera("Camera3", 15);
        
        // Directly add cameras to the internal SortedSet to test sorting behavior
        var camerasField = contentManager.GetType()
            .GetField("_cameras", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cameras = (System.Collections.Generic.SortedSet<ICamera>?)camerasField?.GetValue(contentManager);
        
        // Add in non-sorted order: 10, 5, 15
        cameras?.Add(mockCamera1.Object);
        cameras?.Add(mockCamera2.Object);
        cameras?.Add(mockCamera3.Object);
        
        // Act
        var activeCameras = contentManager.ActiveCameras.ToList();
        
        // Assert - SortedSet should automatically sort by priority: 5, 10, 15
        Assert.Equal(3, activeCameras.Count);
        Assert.Equal(mockCamera2.Object, activeCameras[0]); // Priority 5
        Assert.Equal(mockCamera1.Object, activeCameras[1]); // Priority 10
        Assert.Equal(mockCamera3.Object, activeCameras[2]); // Priority 15
    }

    // Note: Additional camera collection behavior is tested through integration tests.
    // These unit tests focus on the public API: Initialize() and ActiveCameras property.

    [Fact]
    public void ContentManager_ActiveCameras_IncludesLoadedCameras()
    {
        // Arrange
        var mockFactory = CreateMockFactoryForInitialize();
        var mockProfiler = new Mock<IProfiler>();
        var contentManager = new ContentManager(mockFactory.Object, mockProfiler.Object);

        // Load a camera component
        var cameraTemplate = new StaticCameraTemplate
        {
            Name = "TestCamera",
            ClearColor = new Vector4D<float>(0, 0, 0, 1),
            ScreenRegion = new Rectangle<float>(0, 0, 1, 1),
            RenderPriority = 100
        };
        var camera = contentManager.Load(cameraTemplate);
        contentManager.OnUpdate(0.016); // Trigger camera discovery

        // Act
        var cameras = contentManager.ActiveCameras;

        // Assert
        Assert.Single(cameras);
        var loadedCamera = cameras.First();
        Assert.True(loadedCamera.IsActive());
    }
}
