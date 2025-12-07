using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime.Systems;
using Xunit;

namespace Nexus.GameEngine.Runtime.Systems.Tests;

public class ContentSystemTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        var mockContentManager = new Mock<IContentManager>();

        // Act
        var system = new ContentSystem(mockContentManager.Object);

        // Assert
        Assert.Same(mockContentManager.Object, system.ContentManager);
    }
}
