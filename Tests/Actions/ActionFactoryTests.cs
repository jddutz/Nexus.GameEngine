using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;

namespace Tests.Actions;

/// <summary>
/// Unit tests for ActionFactory behavior, focusing on action resolution, execution, and error handling
/// </summary>
public class ActionFactoryTests
{
    /// <summary>
    /// Tests that ActionFactory successfully executes a valid action
    /// </summary>
    [Fact]
    public async Task ActionFactory_ShouldExecuteAction_WhenValidActionIdProvided()
    {
        // Arrange
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<ActionFactory>>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockAction = new Mock<IAction>();
        var mockContext = new Mock<IRuntimeComponent>();

        var expectedResult = ActionResult.Successful(null, "Test action executed");
        mockAction.Setup(a => a.ExecuteAsync(mockContext.Object))
                 .ReturnsAsync(expectedResult);

        var actionId = ActionId.FromType<QuitGameAction>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(QuitGameAction)))
                          .Returns(mockAction.Object);

        var actionFactory = new ActionFactory(mockServiceProvider.Object, mockLoggerFactory.Object);

        // Act
        var result = await actionFactory.ExecuteAsync(actionId, mockContext.Object);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Test action executed", result.Message);
        mockAction.Verify(a => a.ExecuteAsync(mockContext.Object), Times.Once);
    }

    /// <summary>
    /// Tests that ActionFactory returns failure when action is not registered in service provider
    /// </summary>
    [Fact]
    public async Task ActionFactory_ShouldReturnFailure_WhenActionNotRegistered()
    {
        // Arrange
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<ActionFactory>>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockContext = new Mock<IRuntimeComponent>();

        var actionId = ActionId.FromType<ToggleFullscreenAction>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ToggleFullscreenAction)))
                          .Returns((object?)null);

        var actionFactory = new ActionFactory(mockServiceProvider.Object, mockLoggerFactory.Object);

        // Act
        var result = await actionFactory.ExecuteAsync(actionId, mockContext.Object);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not registered in the service container", result.Message);
    }

    /// <summary>
    /// Tests that ActionFactory returns failure when ActionId is None
    /// </summary>
    [Fact]
    public async Task ActionFactory_ShouldReturnFailure_WhenActionIdIsNone()
    {
        // Arrange
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<ActionFactory>>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockContext = new Mock<IRuntimeComponent>();

        var actionFactory = new ActionFactory(mockServiceProvider.Object, mockLoggerFactory.Object);

        // Act
        var result = await actionFactory.ExecuteAsync(ActionId.None, mockContext.Object);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("ActionId is None or invalid", result.Message);
    }

    /// <summary>
    /// Tests that ActionFactory handles exceptions thrown by actions
    /// </summary>
    [Fact]
    public async Task ActionFactory_ShouldHandleExceptions_WhenActionThrows()
    {
        // Arrange
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger<ActionFactory>>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockAction = new Mock<IAction>();
        var mockContext = new Mock<IRuntimeComponent>();

        var expectedException = new InvalidOperationException("Test exception");
        mockAction.Setup(a => a.ExecuteAsync(mockContext.Object))
                 .ThrowsAsync(expectedException);

        var actionId = ActionId.FromType<QuitGameAction>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(QuitGameAction)))
                          .Returns(mockAction.Object);

        var actionFactory = new ActionFactory(mockServiceProvider.Object, mockLoggerFactory.Object);

        // Act
        var result = await actionFactory.ExecuteAsync(actionId, mockContext.Object);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Test exception", result.Message);
        Assert.Equal(expectedException, result.Exception);
    }
}