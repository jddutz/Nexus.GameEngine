using Moq;
using Nexus.GameEngine.Graphics.Buffers;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime.Systems;
using Xunit;

namespace Nexus.GameEngine.Runtime.Systems.Tests;

public class ResourceSystemTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        var mockResourceManager = new Mock<IResourceManager>();
        var mockBufferManager = new Mock<IBufferManager>();

        // Act
        var system = new ResourceSystem(mockResourceManager.Object, mockBufferManager.Object);

        // Assert
        Assert.Same(mockResourceManager.Object, system.ResourceManager);
        Assert.Same(mockBufferManager.Object, system.BufferManager);
    }
}
