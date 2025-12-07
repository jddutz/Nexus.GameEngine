using Moq;
using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Runtime.Systems;
using Silk.NET.Input;
using Xunit;

namespace Nexus.GameEngine.Runtime.Systems.Tests;

public class InputSystemTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        var mockWindowService = new Mock<IWindowService>();
        var mockActionFactory = new Mock<IActionFactory>();
        var mockInputContext = new Mock<IInputContext>();
        var mockKeyboard = new Mock<IKeyboard>();
        var mockMouse = new Mock<IMouse>();

        mockInputContext.Setup(x => x.Keyboards).Returns(new[] { mockKeyboard.Object });
        mockInputContext.Setup(x => x.Mice).Returns(new[] { mockMouse.Object });
        mockWindowService.Setup(x => x.InputContext).Returns(mockInputContext.Object);

        // Act
        var system = new InputSystem(mockWindowService.Object, mockActionFactory.Object);

        // Assert
        Assert.Same(mockInputContext.Object, system.InputContext);
        Assert.Same(mockKeyboard.Object, system.Keyboard);
        Assert.Same(mockMouse.Object, system.Mouse);
        Assert.Same(mockActionFactory.Object, system.ActionFactory);
    }
}
