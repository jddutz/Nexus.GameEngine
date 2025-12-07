using Moq;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.Synchronization;
using Nexus.GameEngine.Runtime.Systems;
using Xunit;
using System;

namespace Nexus.GameEngine.Runtime.Systems.Tests;

public class GraphicsSystemTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        var mockContext = new Mock<IGraphicsContext>();
        var mockPipelineManager = new Mock<IPipelineManager>();
        var mockDescriptorManager = new Mock<IDescriptorManager>();
        var mockSwapChain = new Mock<ISwapChain>();
        var mockSyncManager = new Mock<ISyncManager>();
        var mockCommandPoolManager = new Mock<ICommandPoolManager>();
        
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(typeof(IPipelineManager))).Returns(mockPipelineManager.Object);

        // Act
        var system = new GraphicsSystem(
            mockContext.Object, 
            mockServiceProvider.Object, 
            mockPipelineManager.Object,
            mockDescriptorManager.Object,
            mockSwapChain.Object,
            mockSyncManager.Object,
            mockCommandPoolManager.Object);

        // Assert
        Assert.Same(mockContext.Object, system.Context);
        Assert.Same(mockPipelineManager.Object, system.PipelineManager);
        Assert.Same(mockDescriptorManager.Object, system.DescriptorManager);
        Assert.Same(mockSwapChain.Object, system.SwapChain);
        Assert.Same(mockSyncManager.Object, system.SyncManager);
        Assert.Same(mockCommandPoolManager.Object, system.CommandPoolManager);
    }
}
