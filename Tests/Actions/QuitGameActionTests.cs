using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;

namespace Tests.Actions;

/// <summary>
/// Unit tests for QuitGameAction behavior
/// </summary>
public class QuitGameActionTests
{
    /// <summary>
    /// Tests that QuitGameAction successfully calls window service Close method
    /// </summary>
    [Fact]
    public async Task QuitGameAction_ShouldCallWindowServiceClose_WhenExecuted()
    {
        // Arrange
        var mockWindowService = new Mock<IWindowService>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var mockContext = new Mock<IRuntimeComponent>();

        var action = new QuitGameAction(mockWindowService.Object, mockLoggerFactory.Object);

        // Act
        var result = await action.ExecuteAsync(mockContext.Object);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Application quit requested", result.Message);
        mockWindowService.Verify(ws => ws.Close(), Times.Once);
    }

    /// <summary>
    /// Tests that QuitGameAction handles exceptions from window service
    /// </summary>
    [Fact]
    public async Task QuitGameAction_ShouldReturnFailure_WhenWindowServiceThrows()
    {
        // Arrange
        var mockWindowService = new Mock<IWindowService>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        var mockContext = new Mock<IRuntimeComponent>();

        var expectedException = new InvalidOperationException("Window close failed");
        mockWindowService.Setup(ws => ws.Close()).Throws(expectedException);

        var action = new QuitGameAction(mockWindowService.Object, mockLoggerFactory.Object);

        // Act
        var result = await action.ExecuteAsync(mockContext.Object);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(expectedException, result.Exception);
        mockWindowService.Verify(ws => ws.Close(), Times.Once);
    }

    /// <summary>
    /// Tests that QuitGameAction works without a context
    /// </summary>
    [Fact]
    public async Task QuitGameAction_ShouldWork_WithoutContext()
    {
        // Arrange
        var mockWindowService = new Mock<IWindowService>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

        var action = new QuitGameAction(mockWindowService.Object, mockLoggerFactory.Object);

        // Act
        var result = await action.ExecuteAsync(null);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Application quit requested", result.Message);
        mockWindowService.Verify(ws => ws.Close(), Times.Once);
    }

    /// <summary>
    /// Tests that GetActionId returns correct ActionId
    /// </summary>
    [Fact]
    public void QuitGameAction_GetActionId_ShouldReturnCorrectActionId()
    {
        // Act
        var actionId = QuitGameAction.GetActionId();

        // Assert
        Assert.Equal(typeof(QuitGameAction), actionId.ActionType);
        Assert.Equal("QuitGameAction", actionId.Identifier);
    }
}