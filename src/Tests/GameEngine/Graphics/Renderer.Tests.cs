using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Graphics.Cameras;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.Synchronization;
using Nexus.GameEngine.Performance;
using Nexus.GameEngine.Runtime.Systems;
using Nexus.GameEngine.Runtime;
using Microsoft.Extensions.Options;
using Xunit;
using System;

namespace Tests.GameEngine.Graphics;

public class RendererTests
{
    [Fact]
    public void Constructor_InitializesWithDependencies()
    {
        // Arrange
        var mockContext = new Mock<IGraphicsContext>();
        var mockSwapChain = new Mock<ISwapChain>();
        var mockSync = new Mock<ISyncManager>();
        var mockCommandPool = new Mock<ICommandPoolManager>();
        var mockContentManager = new Mock<IContentManager>();
        var mockWindowService = new Mock<IWindowService>();
        var mockFactory = new Mock<IComponentFactory>();
        var mockOptions = new Mock<IOptions<GraphicsSettings>>();
        var mockProfiler = new Mock<IProfiler>();

        mockOptions.Setup(x => x.Value).Returns(new GraphicsSettings());
        mockWindowService.Setup(x => x.GetWindow()).Returns(Mock.Of<Silk.NET.Windowing.IWindow>());

        var mockCamera = new Mock<ICamera>();
        var mockComponent = mockCamera.As<IComponent>();
        mockFactory.Setup(x => x.CreateInstance(It.IsAny<Template>())).Returns(mockComponent.Object);

        // Act
        var renderer = new Renderer(
            mockContext.Object,
            mockSwapChain.Object,
            mockSync.Object,
            mockCommandPool.Object,
            mockContentManager.Object,
            mockWindowService.Object,
            mockFactory.Object,
            mockOptions.Object,
            mockProfiler.Object
        );

        // Assert
        Assert.NotNull(renderer);
    }
}
