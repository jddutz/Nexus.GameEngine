using Moq;
using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;
using Silk.NET.Windowing;

namespace Tests.Actions;

/// <summary>
/// Unit tests for ToggleFullscreenAction behavior
/// </summary>
public class ToggleFullscreenActionTests
{
    /// <summary>
    /// Tests that ToggleFullscreenAction successfully calls window service ToggleFullscreen
    /// </summary>
    [Fact]
    public async Task ToggleFullscreenAction_ShouldCallToggleFullscreen_WhenExecuted()
    {
        // Arrange
        var mockWindowService = new Mock<IWindowService>();
        var mockWindow = new Mock<IWindow>();
        var mockContext = new Mock<IRuntimeComponent>();

        // Setup window to return windowed state after toggle
        mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);
        mockWindowService.Setup(ws => ws.GetOrCreateWindow()).Returns(mockWindow.Object);

        var action = new ToggleFullscreenAction(mockWindowService.Object);

        // Act
        var result = await action.ExecuteAsync(mockContext.Object);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Window state changed to Windowed", result.Message);
        mockWindowService.Verify(ws => ws.ToggleFullscreen(), Times.Once);
        mockWindowService.Verify(ws => ws.GetOrCreateWindow(), Times.Once);
    }

    /// <summary>
    /// Tests that ToggleFullscreenAction reports fullscreen state correctly
    /// </summary>
    [Fact]
    public async Task ToggleFullscreenAction_ShouldReportFullscreenState_WhenWindowIsFullscreen()
    {
        // Arrange
        var mockWindowService = new Mock<IWindowService>();
        var mockWindow = new Mock<IWindow>();
        var mockContext = new Mock<IRuntimeComponent>();

        // Setup window to return fullscreen state after toggle
        mockWindow.Setup(w => w.WindowState).Returns(WindowState.Fullscreen);
        mockWindowService.Setup(ws => ws.GetOrCreateWindow()).Returns(mockWindow.Object);

        var action = new ToggleFullscreenAction(mockWindowService.Object);

        // Act
        var result = await action.ExecuteAsync(mockContext.Object);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Window state changed to Fullscreen", result.Message);
        mockWindowService.Verify(ws => ws.ToggleFullscreen(), Times.Once);
    }

    /// <summary>
    /// Tests that ToggleFullscreenAction handles exceptions from window service
    /// </summary>
    [Fact]
    public async Task ToggleFullscreenAction_ShouldReturnFailure_WhenWindowServiceThrows()
    {
        // Arrange
        var mockWindowService = new Mock<IWindowService>();
        var mockContext = new Mock<IRuntimeComponent>();

        var expectedException = new InvalidOperationException("Fullscreen toggle failed");
        mockWindowService.Setup(ws => ws.ToggleFullscreen()).Throws(expectedException);

        var action = new ToggleFullscreenAction(mockWindowService.Object);

        // Act
        var result = await action.ExecuteAsync(mockContext.Object);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(expectedException, result.Exception);
        mockWindowService.Verify(ws => ws.ToggleFullscreen(), Times.Once);
    }

    /// <summary>
    /// Tests that ToggleFullscreenAction works without a context
    /// </summary>
    [Fact]
    public async Task ToggleFullscreenAction_ShouldWork_WithoutContext()
    {
        // Arrange
        var mockWindowService = new Mock<IWindowService>();
        var mockWindow = new Mock<IWindow>();

        mockWindow.Setup(w => w.WindowState).Returns(WindowState.Normal);
        mockWindowService.Setup(ws => ws.GetOrCreateWindow()).Returns(mockWindow.Object);

        var action = new ToggleFullscreenAction(mockWindowService.Object);

        // Act
        var result = await action.ExecuteAsync(null);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Window state changed to Windowed", result.Message);
        mockWindowService.Verify(ws => ws.ToggleFullscreen(), Times.Once);
    }

    /// <summary>
    /// Tests that GetActionId returns correct ActionId
    /// </summary>
    [Fact]
    public void ToggleFullscreenAction_GetActionId_ShouldReturnCorrectActionId()
    {
        // Act
        var actionId = ToggleFullscreenAction.GetActionId();

        // Assert
        Assert.Equal(typeof(ToggleFullscreenAction), actionId.ActionType);
        Assert.Equal("ToggleFullscreenAction", actionId.Identifier);
    }
}