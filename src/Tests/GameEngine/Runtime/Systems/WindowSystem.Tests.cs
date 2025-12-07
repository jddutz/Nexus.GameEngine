using Moq;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Runtime.Systems;
using Silk.NET.Windowing;
using Xunit;

namespace Nexus.GameEngine.Runtime.Systems.Tests;

public class WindowSystemTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        var mockWindowService = new Mock<IWindowService>();
        var mockWindow = new Mock<IWindow>();
        mockWindowService.Setup(x => x.GetWindow()).Returns(mockWindow.Object);

        // Act
        var system = new WindowSystem(mockWindowService.Object);

        // Assert
        Assert.Same(mockWindow.Object, system.Window);
    }
}
