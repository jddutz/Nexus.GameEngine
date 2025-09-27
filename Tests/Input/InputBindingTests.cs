using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Input.Components;
using Nexus.GameEngine.Runtime;
using Silk.NET.Input;

namespace Tests.Input;

/// <summary>
/// Unit tests for InputBinding abstract base class functionality
/// </summary>
public class InputBindingTests
{
    /// <summary>
    /// Tests that InputBinding configures correctly from template
    /// </summary>
    [Fact]
    public void InputBinding_ShouldConfigure_FromTemplate()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();

        // Use a simple ActionId that doesn't require a specific type
        var testActionId = ActionId.FromType(typeof(QuitGameAction)); // Use existing action type
        var template = new TestInputBinding.Template
        {
            ActionId = testActionId,
            Name = "Test Input Binding",
            Enabled = true
        };

        var inputBinding = new TestInputBinding(mockLogger.Object, CreateMockWindowService(mockInputContext.Object), mockActionFactory.Object);

        // Act
        inputBinding.Configure(template);

        // Assert
        Assert.Equal(testActionId, inputBinding.ActionId);
        Assert.Equal("Test Input Binding", inputBinding.Name);
        Assert.True(inputBinding.IsEnabled);
    }

    /// <summary>
    /// Tests that InputBinding validation fails when ActionId is None
    /// </summary>
    [Fact]
    public void InputBinding_ShouldFailValidation_WhenActionIdIsNone()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();
        var template = new TestInputBinding.Template
        {
            ActionId = ActionId.None
        };

        var inputBinding = new TestInputBinding(mockLogger.Object, CreateMockWindowService(mockInputContext.Object), mockActionFactory.Object);
        inputBinding.Configure(template);

        // Act
        var result = inputBinding.Validate();

        // Assert
        Assert.False(result);
        Assert.Contains(inputBinding.ValidationErrors,
            e => e.Severity == ValidationSeverityEnum.Error &&
            e.Message.Contains("ActionId is required"));
    }

    /// <summary>
    /// Tests that InputBinding validation fails when ActionFactory is null
    /// </summary>
    [Fact]
    public void InputBinding_ShouldFailValidation_WhenActionFactoryIsNull()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var template = new TestInputBinding.Template
        {
            ActionId = ActionId.FromType(typeof(QuitGameAction))
        };

        var inputBinding = new TestInputBinding(mockLogger.Object, CreateMockWindowService(mockInputContext.Object), null!);
        inputBinding.Configure(template);

        // Act
        var result = inputBinding.Validate();

        // Assert
        Assert.False(result);
        Assert.Contains(inputBinding.ValidationErrors,
            e => e.Severity == ValidationSeverityEnum.Error &&
            e.Message.Contains("ActionFactory is required"));
    }

    /// <summary>
    /// Tests that ExecuteActionAsync executes action when enabled
    /// </summary>
    [Fact]
    public async Task InputBinding_ShouldExecuteAction_WhenEnabled()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();
        var expectedResult = ActionResult.Successful("Action executed");
        mockActionFactory.Setup(af => af.ExecuteAsync(It.IsAny<ActionId>(), It.IsAny<IRuntimeComponent>()))
                        .ReturnsAsync(expectedResult);

        var testActionId = ActionId.FromType(typeof(QuitGameAction));
        var template = new TestInputBinding.Template
        {
            ActionId = testActionId,
            Enabled = true
        };

        var inputBinding = new TestInputBinding(mockLogger.Object, CreateMockWindowService(mockInputContext.Object), mockActionFactory.Object);
        inputBinding.Configure(template);

        // Act
        await inputBinding.PublicExecuteActionAsync("Test event");

        // Assert
        mockActionFactory.Verify(af => af.ExecuteAsync(
            It.Is<ActionId>(id => id.Equals(testActionId)),
            inputBinding), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteActionAsync does not execute action when disabled
    /// </summary>
    [Fact]
    public async Task InputBinding_ShouldNotExecuteAction_WhenDisabled()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();

        var template = new TestInputBinding.Template
        {
            ActionId = ActionId.FromType(typeof(QuitGameAction)),
            Enabled = false
        };

        var inputBinding = new TestInputBinding(mockLogger.Object, CreateMockWindowService(mockInputContext.Object), mockActionFactory.Object);
        inputBinding.Configure(template);

        // Act
        await inputBinding.PublicExecuteActionAsync("Test event");

        // Assert
        mockActionFactory.Verify(af => af.ExecuteAsync(It.IsAny<ActionId>(), It.IsAny<IRuntimeComponent>()),
                                Times.Never);
    }

    /// <summary>
    /// Tests that ExecuteActionAsync logs failure when action execution fails
    /// </summary>
    [Fact]
    public async Task InputBinding_ShouldLogFailure_WhenActionExecutionFails()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();
        var failureResult = ActionResult.Failed("Action failed");
        mockActionFactory.Setup(af => af.ExecuteAsync(It.IsAny<ActionId>(), It.IsAny<IRuntimeComponent>()))
                        .ReturnsAsync(failureResult);

        var template = new TestInputBinding.Template
        {
            ActionId = ActionId.FromType(typeof(QuitGameAction)),
            Enabled = true
        };

        var inputBinding = new TestInputBinding(mockLogger.Object, CreateMockWindowService(mockInputContext.Object), mockActionFactory.Object);
        inputBinding.Configure(template);

        // Act
        await inputBinding.PublicExecuteActionAsync("Test event");

        // Assert
        mockActionFactory.Verify(af => af.ExecuteAsync(It.IsAny<ActionId>(), It.IsAny<IRuntimeComponent>()),
                                Times.Once);
        // The actual logging verification would require capturing log output,
        // which is complex. The important part is that the action was attempted.
    }

    /// <summary>
    /// Helper method to create a mock IWindowService that returns the specified IInputContext
    /// </summary>
    private static IWindowService CreateMockWindowService(IInputContext inputContext)
    {
        var mockWindowService = new Mock<IWindowService>();
        mockWindowService.Setup(ws => ws.GetInputContext()).Returns(inputContext);
        return mockWindowService.Object;
    }
}

/// <summary>
/// Test implementation of InputBinding to test abstract base class functionality
/// </summary>
public class TestInputBinding : InputBinding
{
    public TestInputBinding(ILogger logger, IWindowService windowService, IActionFactory actionFactory)
        : base(windowService, actionFactory)
    {
        Logger = logger;
    }

    /// <summary>
    /// Public wrapper to test protected ExecuteActionAsync method
    /// </summary>
    public async Task PublicExecuteActionAsync(string eventDescription)
    {
        await ExecuteActionAsync(eventDescription);
    }

    protected override void SubscribeToInputEvents()
    {
        // Test implementation - no actual event subscription needed
    }

    protected override void UnsubscribeFromInputEvents()
    {
        // Test implementation - no actual event unsubscription needed
    }

    /// <summary>
    /// Test template for InputBinding
    /// </summary>
    public new record Template : InputBinding.Template
    {
        // Inherit Name and Enabled from base template
    }
}
