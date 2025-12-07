using Xunit;
using Moq;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using System;

namespace Tests.GameEngine.Resources;

public class ResourceManagerTests
{
    [Fact]
    public void Constructor_InitializesWithServiceProvider()
    {
        // Arrange
        var mockProvider = new Mock<IServiceProvider>();
        var mockGeometry = new Mock<IGeometryResourceManager>();
        
        mockProvider.Setup(x => x.GetService(typeof(IGeometryResourceManager)))
            .Returns(mockGeometry.Object);

        // Act
        // This will fail to compile until ResourceManager is refactored
        var manager = new ResourceManager(mockProvider.Object);

        // Assert
        Assert.NotNull(manager);
    }
}
