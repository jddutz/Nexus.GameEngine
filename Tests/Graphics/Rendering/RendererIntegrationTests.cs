using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Graphics.Rendering;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Nexus.GameEngine.Graphics;
using Silk.NET.Windowing;
using Xunit;

namespace Tests.Graphics.Rendering;

/// <summary>
/// Integration tests for the Renderer class using a real OpenGL context.
/// These tests verify that the Renderer works correctly with actual GL calls.
/// </summary>
public class RendererIntegrationTests : IDisposable
{
    private readonly OpenGLTestApp _testApp;
    private readonly Renderer _renderer;

    public RendererIntegrationTests()
    {
        // Create test app first to get GL context
        _testApp = new OpenGLTestApp();

        // Create a mocked logger for testing
        var mockLogger = new Mock<ILogger<Renderer>>();

        // Create mock window service that returns the test app's window
        var mockWindowService = new Mock<IWindowService>();
        mockWindowService.Setup(x => x.GetOrCreateWindow()).Returns(_testApp.GetWindow());

        // Create our renderer with the mock window service
        _renderer = new Renderer(mockWindowService.Object, mockLogger.Object);
    }

    [Fact]
    public void Renderer_ShouldInitializeWithRealGLContext()
    {
        // Assert
        Assert.NotNull(_testApp.GL);
        Assert.NotNull(_renderer);  // Test our own renderer, not the test app's renderer
        Assert.IsType<Renderer>(_renderer);
    }

    [Fact]
    public void RenderFrame_ShouldExecuteWithoutErrors()
    {
        // Act & Assert - Should not throw any GL errors
        _renderer.RenderFrame();

        // Verify no GL errors occurred
        var error = _testApp.GL.GetError();
        Assert.Equal(Silk.NET.OpenGL.GLEnum.NoError, error);
    }

    [Fact]
    public void SharedResources_ShouldStoreAndRetrieveValues()
    {
        // Arrange
        const string resourceName = "TestResource";
        const uint resourceValue = 42u;

        // Act
        _renderer.SetSharedResource(resourceName, resourceValue);
        var retrieved = _renderer.GetSharedResource<uint>(resourceName);

        // Assert
        Assert.Equal(resourceValue, retrieved);
    }

    [Fact]
    public void SharedResources_ShouldWorkWithRealGL()
    {
        // Arrange
        const string resourceName = "TestTexture";
        var textureId = _testApp.GL.GenTexture();

        // Act
        _renderer.SetSharedResource(resourceName, textureId);
        var retrieved = _renderer.GetSharedResource<uint>(resourceName);

        // Assert
        Assert.Equal(textureId, retrieved);

        // Cleanup
        _testApp.GL.DeleteTexture(textureId);
    }

    [Fact]
    public void RenderPasses_ShouldConfigureGLStateCorrectly()
    {
        // Arrange - Clear any existing GL state
        _testApp.GL.Disable(Silk.NET.OpenGL.EnableCap.DepthTest);
        _testApp.GL.Disable(Silk.NET.OpenGL.EnableCap.Blend);

        // Create a real component tree with a camera
        var rootComponent = new RuntimeComponent();
        var camera = new StaticCamera();

        // Configure the camera with the required render pass
        camera.RenderPasses.Clear();
        camera.RenderPasses.Add(new RenderPassConfiguration
        {
            Id = 0,
            Name = "Test",
            DepthTestEnabled = true,
            BlendingMode = BlendingMode.Alpha
        });

        // Activate the camera so it participates in rendering
        camera.IsActive = true;

        // Add camera as child of root component
        rootComponent.AddChild(camera);

        _renderer.RootComponent = rootComponent;

        // Debug: Check if cameras were discovered
        var discoveredCameras = _renderer.Cameras;

        // More detailed debugging
        if (discoveredCameras.Count == 0)
        {
            // Let's check if the camera implements ICamera
            var isCamera = camera is ICamera;
            var isRuntimeComponent = camera is IRuntimeComponent;
            var children = rootComponent.Children;

            Assert.True(isCamera, "StaticCamera should implement ICamera");
            Assert.True(isRuntimeComponent, "StaticCamera should implement IRuntimeComponent");
            Assert.Single(children, "Root component should have one child");
            Assert.Same(camera, children.First()); // Child should be our camera
        }

        Assert.Single(discoveredCameras); // Should have found our camera

        // Act
        _renderer.RenderFrame();

        // Assert - Check that the expected GL state was set
        var depthTestEnabled = _testApp.GL.IsEnabled(Silk.NET.OpenGL.EnableCap.DepthTest);
        var blendEnabled = _testApp.GL.IsEnabled(Silk.NET.OpenGL.EnableCap.Blend);

        Assert.True(depthTestEnabled); // Our renderer should enable depth testing
        Assert.True(blendEnabled); // Our renderer should enable blending for alpha
    }

    [Fact]
    public void CaptureFrame_ShouldReturnValidPixelData()
    {
        // Act
        _renderer.RenderFrame();
        var pixels = _testApp.CaptureFrame();

        // Assert
        Assert.NotNull(pixels);
        Assert.True(pixels.Length > 0);

        // The captured frame should contain pixel data (width * height * 4 bytes for RGBA)
        // Since we have a hidden window, we should get some pixel data back
        Assert.True(pixels.Length % 4 == 0); // Should be RGBA format
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _testApp?.Dispose();
    }
}