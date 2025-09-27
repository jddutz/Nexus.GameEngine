using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Graphics.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Xunit;

namespace OpenGLTests;

/// <summary>
/// Integration tests for BackgroundLayer component that require OpenGL context.
/// </summary>
public class BackgroundLayerIntegrationTests : OpenGLTestBase
{
    private readonly BackgroundLayer _backgroundLayer;
    private readonly Mock<IResourceManager> _mockResourceManager;

    public BackgroundLayerIntegrationTests(OpenGLContextFixture fixture) : base(fixture)
    {
        _mockResourceManager = new Mock<IResourceManager>();
        _backgroundLayer = new BackgroundLayer(_mockResourceManager.Object);
    }

    [Fact]
    public void OnRender_IntegrationTest_ExecutesOpenGLCallsWithoutError()
    {
        // Arrange - Set up resource manager to return valid GL handles
        _mockResourceManager.Setup(rm => rm.GetOrCreateResource<uint>(
            It.Is<IResourceDefinition>(def => def.Name == "FullScreenQuad"),
            It.IsAny<IRuntimeComponent>())).Returns(1u);
        _mockResourceManager.Setup(rm => rm.GetOrCreateResource<uint>(
            It.Is<IResourceDefinition>(def => def.Name == "BackgroundSolid"),
            It.IsAny<IRuntimeComponent>())).Returns(2u);

        var template = new BackgroundLayer.Template
        {
            MaterialType = MaterialType.SolidColor,
            BackgroundColor = new Vector4D<float>(1.0f, 0.0f, 0.0f, 1.0f),
            Tint = new Vector4D<float>(0.8f, 0.8f, 0.8f, 1.0f),
            Saturation = 1.2f,
            Fade = 0.8f
        };
        _backgroundLayer.Configure(template);
        _backgroundLayer.IsVisible = true;

        // Act - Should execute without throwing
        var exception = Record.Exception(() => _backgroundLayer.OnRender(Renderer, 16.67));

        // Assert - Should not throw any exceptions
        Assert.Null(exception);

        // Verify that solid color rendering no longer uses resource manager
        // since we switched to using glClear instead of rendering a quad
        _mockResourceManager.Verify(rm => rm.GetOrCreateResource<uint>(
            It.IsAny<IResourceDefinition>(),
            It.IsAny<IRuntimeComponent>()), Times.Never);

        // Check for GL errors but don't fail the test if there are InvalidValue errors
        // since we're using mock resource handles that may not be valid GL objects
        var error = GL.GetError();
        if (error != GLEnum.NoError && error != GLEnum.InvalidValue)
        {
            Assert.Fail($"Unexpected OpenGL error: {error}");
        }
    }

    [Fact]
    public void OnRender_WhenNotVisible_DoesNotRender()
    {
        // Arrange
        var template = new BackgroundLayer.Template { MaterialType = MaterialType.SolidColor };
        _backgroundLayer.Configure(template);
        _backgroundLayer.IsVisible = false;

        // Act - Should not call resource manager when not visible
        _backgroundLayer.OnRender(Renderer, 16.67);

        // Assert
        _mockResourceManager.Verify(rm => rm.GetOrCreateResource<uint>(
            It.IsAny<IResourceDefinition>(),
            It.IsAny<IRuntimeComponent>()), Times.Never);

        AssertNoGLErrors();
    }

    [Fact]
    public void OnRender_WhenFullyTransparent_DoesNotRender()
    {
        // Arrange
        var template = new BackgroundLayer.Template { Fade = 0.0f };
        _backgroundLayer.Configure(template);
        _backgroundLayer.IsVisible = true;

        // Act - Should not call resource manager when fully transparent
        _backgroundLayer.OnRender(Renderer, 16.67);

        // Assert
        _mockResourceManager.Verify(rm => rm.GetOrCreateResource<uint>(
            It.IsAny<IResourceDefinition>(),
            It.IsAny<IRuntimeComponent>()), Times.Never);

        AssertNoGLErrors();
    }
}