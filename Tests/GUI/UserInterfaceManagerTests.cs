using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using static Tests.Components.RuntimeComponentTestHelpers;

namespace Tests.GUI;

/// <summary>
/// Tests for UserInterfaceManager component covering template management, UI lifecycle, and state management.
/// Follows documentation-first TDD approach with 80%+ code coverage.
/// </summary>
public class UserInterfaceManagerTests
{
    private readonly Mock<ILogger<UserInterfaceManager>> _mockLogger;
    private readonly Mock<IComponentFactory> _mockFactory;
    private readonly Mock<IRenderer> _mockRenderer;
    private readonly UserInterfaceManager _uiManager;

    public UserInterfaceManagerTests()
    {
        _mockLogger = new Mock<ILogger<UserInterfaceManager>>();
        _mockFactory = CreateMockFactory();
        _mockRenderer = new Mock<IRenderer>();

        _uiManager = new UserInterfaceManager(
            _mockLogger.Object,
            _mockFactory.Object,
            _mockRenderer.Object);
    }

    #region Active Property Tests

    [Fact]
    public void Active_InitiallyNull_ReturnsNull()
    {
        // Arrange & Act
        var active = _uiManager.Active;

        // Assert
        Assert.Null(active);
    }

    [Fact]
    public void Active_AfterActivation_ReturnsActiveComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);

        // Act
        _uiManager.Create(template);
        _uiManager.Activate("TestUI");

        // Assert
        Assert.Equal(mockComponent.Object, _uiManager.Active);
    }

    #endregion

    #region Create Method Tests

    [Fact]
    public void Create_WithValidTemplate_CreatesAndStoresComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);

        // Act
        _uiManager.Create(template);

        // Assert
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once);
        Assert.True(_uiManager.TryGet("TestUI", out var component));
        Assert.Equal(mockComponent.Object, component);
    }

    [Fact]
    public void Create_WithEmptyName_LogsWarningAndDoesNotCreate()
    {
        // Arrange
        var template = CreateTemplate("");

        // Act
        _uiManager.Create(template);

        // Assert
        _mockFactory.Verify(f => f.Instantiate(It.IsAny<IComponentTemplate>()), Times.Never);
        Assert.False(_uiManager.TryGet("", out _));
        VerifyLogCalled(LogLevel.Warning, "Cannot create user interface with empty name");
    }

    [Fact]
    public void Create_WithNullName_LogsWarningAndDoesNotCreate()
    {
        // Arrange
        var template = CreateTemplate(null!);

        // Act
        _uiManager.Create(template);

        // Assert
        _mockFactory.Verify(f => f.Instantiate(It.IsAny<IComponentTemplate>()), Times.Never);
        Assert.False(_uiManager.TryGet("", out _));
        VerifyLogCalled(LogLevel.Warning, "Cannot create user interface with empty name");
    }

    [Fact]
    public void Create_WithDuplicateName_DoesNotRecreateComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent1 = CreateMockChild("TestUI");
        var mockComponent2 = CreateMockChild("TestUI");
        _mockFactory.SetupSequence(f => f.Instantiate(template))
                   .Returns(mockComponent1.Object)
                   .Returns(mockComponent2.Object);

        // Act
        _uiManager.Create(template);
        _uiManager.Create(template); // Second call should be ignored

        // Assert
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once);
        Assert.True(_uiManager.TryGet("TestUI", out var component));
        Assert.Equal(mockComponent1.Object, component); // Should be first component, not second
    }

    [Fact]
    public void Create_WhenFactoryReturnsNull_LogsWarningAndDoesNotStore()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns((IRuntimeComponent?)null);

        // Act
        _uiManager.Create(template);

        // Assert
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once);
        Assert.False(_uiManager.TryGet("TestUI", out _));
        VerifyLogCalled(LogLevel.Warning, "Failed to create user interface component from template 'TestUI'");
    }

    [Fact]
    public void Create_WithValidTemplate_LogsDebugMessage()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);

        // Act
        _uiManager.Create(template);

        // Assert
        VerifyLogCalled(LogLevel.Debug, "Created user interface component 'TestUI'");
    }

    #endregion

    #region Activate Method Tests

    [Fact]
    public void Activate_WithExistingComponent_ActivatesAndSetsAsActive()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);
        _uiManager.Create(template);

        // Act
        var result = _uiManager.Activate("TestUI");

        // Assert
        Assert.True(result);
        mockComponent.Verify(c => c.Activate(), Times.Once);
        Assert.Equal(mockComponent.Object, _uiManager.Active);
        _mockRenderer.VerifySet(r => r.RootComponent = mockComponent.Object, Times.Once);
        VerifyLogCalled(LogLevel.Debug, "Activated User Interface 'TestUI' and set as renderer root component");
    }

    [Fact]
    public void Activate_WithNonExistentComponent_ReturnsFalseAndLogsWarning()
    {
        // Act
        var result = _uiManager.Activate("NonExistent");

        // Assert
        Assert.False(result);
        Assert.Null(_uiManager.Active);
        _mockRenderer.VerifySet(r => r.RootComponent = It.IsAny<IRuntimeComponent>(), Times.Never);
        VerifyLogCalled(LogLevel.Warning, "User Interface 'NonExistent' not found");
    }

    [Fact]
    public void Activate_WithPreviouslyActiveComponent_DeactivatesPreviousAndActivatesNew()
    {
        // Arrange
        var template1 = CreateTemplate("UI1");
        var template2 = CreateTemplate("UI2");
        var mockComponent1 = CreateMockChild("UI1");
        var mockComponent2 = CreateMockChild("UI2");

        _mockFactory.Setup(f => f.Instantiate(template1)).Returns(mockComponent1.Object);
        _mockFactory.Setup(f => f.Instantiate(template2)).Returns(mockComponent2.Object);

        _uiManager.Create(template1);
        _uiManager.Create(template2);
        _uiManager.Activate("UI1");

        // Act
        var result = _uiManager.Activate("UI2");

        // Assert
        Assert.True(result);
        mockComponent1.Verify(c => c.Deactivate(), Times.Once);
        mockComponent2.Verify(c => c.Activate(), Times.Once);
        Assert.Equal(mockComponent2.Object, _uiManager.Active);
        _mockRenderer.VerifySet(r => r.RootComponent = mockComponent2.Object, Times.Once);
        VerifyLogCalled(LogLevel.Debug, "Deactivated User Interface");
        VerifyLogCalled(LogLevel.Debug, "Activated User Interface 'UI2' and set as renderer root component");
    }

    [Fact]
    public void Activate_WithCurrentlyActiveComponent_StillWorksCorrectly()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);
        _uiManager.Create(template);
        _uiManager.Activate("TestUI");

        // Reset mock calls from first activation
        mockComponent.Reset();
        _mockRenderer.Reset();

        // Act - Activate the same UI again
        var result = _uiManager.Activate("TestUI");

        // Assert
        Assert.True(result);
        mockComponent.Verify(c => c.Deactivate(), Times.Once); // Should deactivate itself first
        mockComponent.Verify(c => c.Activate(), Times.Once);   // Then activate again
        Assert.Equal(mockComponent.Object, _uiManager.Active);
        _mockRenderer.VerifySet(r => r.RootComponent = mockComponent.Object, Times.Once);
    }

    #endregion

    #region Update Method Tests

    [Fact]
    public void Update_WithActiveComponent_CallsUpdateOnActiveComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);
        _uiManager.Create(template);
        _uiManager.Activate("TestUI");
        var deltaTime = 0.016;

        // Act
        _uiManager.Update(deltaTime);

        // Assert
        mockComponent.Verify(c => c.Update(deltaTime), Times.Once);
    }

    [Fact]
    public void Update_WithNoActiveComponent_LogsDebugAndSkipsUpdate()
    {
        // Arrange
        var deltaTime = 0.016;

        // Act
        _uiManager.Update(deltaTime);

        // Assert
        VerifyLogCalled(LogLevel.Debug, "No active user interface available for update - skipping UI update");
    }

    [Fact]
    public void Update_AfterSwitchingToNonExistentUI_KeepsActiveAndContinuesNormally()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);
        _uiManager.Create(template);
        _uiManager.Activate("TestUI");

        // Try to activate non-existent UI (should fail and keep current active)
        var result = _uiManager.Activate("NonExistent");
        Assert.False(result); // Should fail
        Assert.Equal(mockComponent.Object, _uiManager.Active); // Should still be active

        var deltaTime = 0.016;

        // Act
        _uiManager.Update(deltaTime);

        // Assert - Update should still work on the active component
        mockComponent.Verify(c => c.Update(deltaTime), Times.Once);
    }

    #endregion

    #region TryGet Method Tests

    [Fact]
    public void TryGet_WithExistingComponent_ReturnsTrueAndComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);
        _uiManager.Create(template);

        // Act
        var result = _uiManager.TryGet("TestUI", out var component);

        // Assert
        Assert.True(result);
        Assert.Equal(mockComponent.Object, component);
    }

    [Fact]
    public void TryGet_WithNonExistentComponent_ReturnsFalseAndNull()
    {
        // Act
        var result = _uiManager.TryGet("NonExistent", out var component);

        // Assert
        Assert.False(result);
        Assert.Null(component);
    }

    [Fact]
    public void TryGet_WithEmptyName_ReturnsFalseAndNull()
    {
        // Act
        var result = _uiManager.TryGet("", out var component);

        // Assert
        Assert.False(result);
        Assert.Null(component);
    }

    [Fact]
    public void TryGet_AfterMultipleCreations_ReturnsCorrectComponents()
    {
        // Arrange
        var template1 = CreateTemplate("UI1");
        var template2 = CreateTemplate("UI2");
        var mockComponent1 = CreateMockChild("UI1");
        var mockComponent2 = CreateMockChild("UI2");

        _mockFactory.Setup(f => f.Instantiate(template1)).Returns(mockComponent1.Object);
        _mockFactory.Setup(f => f.Instantiate(template2)).Returns(mockComponent2.Object);

        _uiManager.Create(template1);
        _uiManager.Create(template2);

        // Act & Assert
        Assert.True(_uiManager.TryGet("UI1", out var component1));
        Assert.Equal(mockComponent1.Object, component1);

        Assert.True(_uiManager.TryGet("UI2", out var component2));
        Assert.Equal(mockComponent2.Object, component2);
    }

    #endregion

    #region Dispose Method Tests

    [Fact]
    public void Dispose_DisposesAllComponents()
    {
        // Arrange
        var template1 = CreateTemplate("UI1");
        var template2 = CreateTemplate("UI2");
        var mockComponent1 = CreateMockChild("UI1");
        var mockComponent2 = CreateMockChild("UI2");

        _mockFactory.Setup(f => f.Instantiate(template1)).Returns(mockComponent1.Object);
        _mockFactory.Setup(f => f.Instantiate(template2)).Returns(mockComponent2.Object);

        _uiManager.Create(template1);
        _uiManager.Create(template2);
        _uiManager.Activate("UI1");

        // Act
        _uiManager.Dispose();

        // Assert
        mockComponent1.Verify(c => c.Dispose(), Times.Once);
        mockComponent2.Verify(c => c.Dispose(), Times.Once);
        Assert.Null(_uiManager.Active);
        Assert.False(_uiManager.TryGet("UI1", out _));
        Assert.False(_uiManager.TryGet("UI2", out _));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_OnlyDisposesOnce()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);
        _uiManager.Create(template);

        // Act
        _uiManager.Dispose();
        _uiManager.Dispose(); // Second call should be ignored

        // Assert
        mockComponent.Verify(c => c.Dispose(), Times.Once); // Should only be called once
    }

    [Fact]
    public void Dispose_WithNoComponents_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _uiManager.Dispose();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullLifecycle_CreateActivateUpdateDispose_WorksCorrectly()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template)).Returns(mockComponent.Object);

        // Act - Full lifecycle
        _uiManager.Create(template);
        var activateResult = _uiManager.Activate("TestUI");
        _uiManager.Update(0.016);
        _uiManager.Dispose();

        // Assert
        Assert.True(activateResult);
        mockComponent.Verify(c => c.Activate(), Times.Once);
        mockComponent.Verify(c => c.Update(0.016), Times.Once);
        mockComponent.Verify(c => c.Dispose(), Times.Once);
        _mockRenderer.VerifySet(r => r.RootComponent = mockComponent.Object, Times.Once);
    }

    [Fact]
    public void MultipleUIWorkflow_SwitchingBetweenUIs_WorksCorrectly()
    {
        // Arrange
        var mainMenuTemplate = CreateTemplate("MainMenu");
        var settingsTemplate = CreateTemplate("Settings");
        var mockMainMenu = CreateMockChild("MainMenu");
        var mockSettings = CreateMockChild("Settings");

        _mockFactory.Setup(f => f.Instantiate(mainMenuTemplate)).Returns(mockMainMenu.Object);
        _mockFactory.Setup(f => f.Instantiate(settingsTemplate)).Returns(mockSettings.Object);

        // Act
        _uiManager.Create(mainMenuTemplate);
        _uiManager.Create(settingsTemplate);

        _uiManager.Activate("MainMenu");
        _uiManager.Update(0.016);

        _uiManager.Activate("Settings");
        _uiManager.Update(0.016);

        // Assert
        mockMainMenu.Verify(c => c.Activate(), Times.Once);
        mockMainMenu.Verify(c => c.Deactivate(), Times.Once);
        mockMainMenu.Verify(c => c.Update(0.016), Times.Once);

        mockSettings.Verify(c => c.Activate(), Times.Once);
        mockSettings.Verify(c => c.Update(0.016), Times.Once);

        Assert.Equal(mockSettings.Object, _uiManager.Active);
        _mockRenderer.VerifySet(r => r.RootComponent = mockMainMenu.Object, Times.Once);
        _mockRenderer.VerifySet(r => r.RootComponent = mockSettings.Object, Times.Once);
    }

    #endregion

    #region Helper Methods

    private void VerifyLogCalled(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetOrCreate Method Tests

    [Fact]
    public void GetOrCreate_NewTemplate_CreatesAndReturnsComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Returns(mockComponent.Object);

        // Act
        var result = _uiManager.GetOrCreate(template);

        // Assert
        Assert.Equal(mockComponent.Object, result);
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once);
    }

    [Fact]
    public void GetOrCreate_ExistingTemplate_ReturnsExistingComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Returns(mockComponent.Object);

        // Create component first time
        _uiManager.Create(template);

        // Act
        var result = _uiManager.GetOrCreate(template);

        // Assert
        Assert.Equal(mockComponent.Object, result);
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once); // Should not create again
    }

    [Fact]
    public void GetOrCreate_NewTemplateWithActivateTrue_CreatesActivatesAndReturnsComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Returns(mockComponent.Object);

        // Act
        var result = _uiManager.GetOrCreate(template, activate: true);

        // Assert
        Assert.Equal(mockComponent.Object, result);
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once);
        _mockRenderer.VerifySet(r => r.RootComponent = mockComponent.Object, Times.Once);
        VerifyLogCalled(LogLevel.Information, $"Activated UI component '{template.Name}'");
    }

    [Fact]
    public void GetOrCreate_NewTemplateWithActivateFalse_CreatesButDoesNotActivateComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Returns(mockComponent.Object);

        // Act
        var result = _uiManager.GetOrCreate(template, activate: false);

        // Assert
        Assert.Equal(mockComponent.Object, result);
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once);
        _mockRenderer.VerifySet(r => r.RootComponent = It.IsAny<IRuntimeComponent>(), Times.Never);
    }

    [Fact]
    public void GetOrCreate_ExistingTemplateWithActivateTrue_ReturnsAndActivatesExistingComponent()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Returns(mockComponent.Object);

        // Create component first time
        _uiManager.Create(template);

        // Act
        var result = _uiManager.GetOrCreate(template, activate: true);

        // Assert
        Assert.Equal(mockComponent.Object, result);
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once); // Should not create again
        _mockRenderer.VerifySet(r => r.RootComponent = mockComponent.Object, Times.Once);
        VerifyLogCalled(LogLevel.Information, $"Activated UI component '{template.Name}'");
    }

    [Fact]
    public void GetOrCreate_ExistingTemplateWithActivateFalse_ReturnsExistingComponentWithoutActivating()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Returns(mockComponent.Object);

        // Create component first time
        _uiManager.Create(template);

        // Act
        var result = _uiManager.GetOrCreate(template, activate: false);

        // Assert
        Assert.Equal(mockComponent.Object, result);
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once); // Should not create again
        _mockRenderer.VerifySet(r => r.RootComponent = It.IsAny<IRuntimeComponent>(), Times.Never);
    }

    [Fact]
    public void GetOrCreate_FactoryReturnsNull_ReturnsNull()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Returns((IRuntimeComponent?)null);

        // Act
        var result = _uiManager.GetOrCreate(template);

        // Assert
        Assert.Null(result);
        _mockFactory.Verify(f => f.Instantiate(template), Times.Once);
    }

    [Fact]
    public void GetOrCreate_FactoryThrowsException_ReturnsNull()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var exception = new InvalidOperationException("Factory error");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Throws(exception);

        // Act
        var result = _uiManager.GetOrCreate(template);

        // Assert
        Assert.Null(result);
        VerifyLogCalled(LogLevel.Error, $"Failed to create UI component from template '{template.Name}'");
    }

    [Fact]
    public void GetOrCreate_ActivationThrowsException_ReturnsComponentButLogsError()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        var exception = new InvalidOperationException("Activation error");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Returns(mockComponent.Object);
        _mockRenderer.SetupSet(r => r.RootComponent = mockComponent.Object)
            .Throws(exception);

        // Act
        var result = _uiManager.GetOrCreate(template, activate: true);

        // Assert
        Assert.Equal(mockComponent.Object, result);
        VerifyLogCalled(LogLevel.Error, $"Failed to activate UI component '{template.Name}'");
    }

    [Fact]
    public void GetOrCreate_DefaultActivateParameter_DefaultsToTrue()
    {
        // Arrange
        var template = CreateTemplate("TestUI");
        var mockComponent = CreateMockChild("TestUI");
        _mockFactory.Setup(f => f.Instantiate(template))
            .Returns(mockComponent.Object);

        // Act
        var result = _uiManager.GetOrCreate(template); // Not passing activate parameter

        // Assert
        Assert.Equal(mockComponent.Object, result);
        _mockRenderer.VerifySet(r => r.RootComponent = mockComponent.Object, Times.Once);
        VerifyLogCalled(LogLevel.Information, $"Activated UI component '{template.Name}'");
    }

    #endregion
}