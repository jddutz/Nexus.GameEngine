using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Runtime;
using Xunit;

namespace OpenGLTests;

/// <summary>
/// Integration tests for the Renderer class using a real OpenGL context.
/// These tests verify that the Renderer works correctly with actual GL calls.
/// </summary>
public class RendererIntegrationTests : OpenGLTestBase, IDisposable
{
    private readonly Renderer _renderer;

    public RendererIntegrationTests(OpenGLContextFixture fixture) : base(fixture)
    {
        // Create a mocked logger for testing
        var mockLogger = new Mock<ILogger<Renderer>>();

        // Create mock window service that returns the test window
        var mockWindowService = new Mock<IWindowService>();
        mockWindowService.Setup(x => x.GetOrCreateWindow()).Returns(Window);

        // Create our renderer with the mock window service
        _renderer = new Renderer(
            mockWindowService.Object,
            mockLogger.Object,
            new DefaultBatchStrategy());
    }

    [Fact]
    public void Renderer_ShouldInitializeWithRealGLContext()
    {
        // Assert
        Assert.NotNull(GL);
        Assert.NotNull(_renderer);
        Assert.IsType<Renderer>(_renderer);
        AssertNoGLErrors();
    }

    [Fact]
    public void RenderFrame_ShouldExecuteWithoutErrors()
    {
        // Act - Should not throw any GL errors
        _renderer.RenderFrame(16.67);

        // Assert - Verify no GL errors occurred
        AssertNoGLErrors();
    }

    [Fact]
    public void CaptureFrame_ShouldReturnValidPixelData()
    {
        // Act
        _renderer.RenderFrame(16.67);
        var pixels = CaptureFrame();

        // Assert
        Assert.NotNull(pixels);
        Assert.True(pixels.Length > 0);

        // The captured frame should contain pixel data (width * height * 4 bytes for RGBA)
        Assert.True(pixels.Length % 4 == 0); // Should be RGBA format

        AssertNoGLErrors();
    }

    public override void Dispose()
    {
        _renderer?.Dispose();
        base.Dispose();
    }
}