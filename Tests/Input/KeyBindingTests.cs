using Microsoft.Extensions.Logging;
using Moq;
using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Input.Components;
using Silk.NET.Input;

namespace Tests.Input;

/// <summary>
/// Unit tests for KeyBinding behavior
/// </summary>
public class KeyBindingTests
{
    /// <summary>
    /// Tests that KeyBinding configures correctly from template
    /// </summary>
    [Fact]
    public void KeyBinding_ShouldConfigure_FromTemplate()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();

        var template = new KeyBinding.Template
        {
            Key = Key.F12,
            ModifierKeys = [Key.ControlLeft, Key.ShiftLeft],
            ActionId = ActionId.FromType(typeof(QuitGameAction)),
            Name = "Test Key Binding"
        };

        var keyBinding = new KeyBinding(mockInputContext.Object, mockActionFactory.Object)
        {
            Logger = mockLogger.Object
        };

        // Act
        keyBinding.Configure(template);

        // Assert
        Assert.Equal(Key.F12, keyBinding.Key);
        Assert.Equal([Key.ControlLeft, Key.ShiftLeft], keyBinding.ModifierKeys);
        Assert.Equal(ActionId.FromType(typeof(QuitGameAction)), keyBinding.ActionId);
        Assert.Equal("Test Key Binding", keyBinding.Name);
    }

    /// <summary>
    /// Tests that KeyBinding validation fails when ActionId is None
    /// </summary>
    [Fact]
    public void KeyBinding_ShouldFailValidation_WhenActionIdIsNone()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();

        var template = new KeyBinding.Template
        {
            Key = Key.F12,
            ActionId = ActionId.None
        };

        var keyBinding = new KeyBinding(mockInputContext.Object, mockActionFactory.Object)
        {
            Logger = mockLogger.Object
        };
        keyBinding.Configure(template);

        // Act
        var result = keyBinding.Validate();

        // Assert
        Assert.False(result);
        Assert.Contains(keyBinding.ValidationErrors,
            e => e.Severity == ValidationSeverityEnum.Error &&
            e.Message.Contains("ActionId is required"));
    }

    /// <summary>
    /// Tests that KeyBinding validation fails when ActionFactory is null
    /// </summary>
    [Fact]
    public void KeyBinding_ShouldFailValidation_WhenActionFactoryIsNull()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();

        var template = new KeyBinding.Template
        {
            Key = Key.F12,
            ActionId = ActionId.FromType(typeof(ToggleFullscreenAction))
        };

        var keyBinding = new KeyBinding(mockInputContext.Object, null!)
        {
            Logger = mockLogger.Object
        };
        keyBinding.Configure(template);

        // Act
        var result = keyBinding.Validate();

        // Assert
        Assert.False(result);
        Assert.Contains(keyBinding.ValidationErrors,
            e => e.Severity == ValidationSeverityEnum.Error &&
            e.Message.Contains("ActionFactory is required"));
    }

    /// <summary>
    /// Tests that KeyBinding subscribes to keyboard events on activation
    /// </summary>
    [Fact]
    public void KeyBinding_ShouldSubscribeToKeyboardEvents_OnActivation()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();
        var mockKeyboard = new Mock<IKeyboard>();

        // Setup input context to return mock keyboards
        mockInputContext.Setup(ic => ic.Keyboards).Returns([mockKeyboard.Object]);

        var template = new KeyBinding.Template
        {
            Key = Key.F12,
            ActionId = ActionId.FromType(typeof(QuitGameAction))
        };

        var keyBinding = new KeyBinding(mockInputContext.Object, mockActionFactory.Object)
        {
            Logger = mockLogger.Object
        };
        keyBinding.Configure(template);

        // Act
        keyBinding.Activate();

        // Assert
        // Verify that event handlers were subscribed (we can't directly verify event subscription,
        // but we can verify the keyboard was accessed and the component is active)
        Assert.True(keyBinding.IsEnabled);
        mockInputContext.Verify(ic => ic.Keyboards, Times.AtLeastOnce);
    }

    /// <summary>
    /// Tests that KeyBinding executes action when correct key is pressed
    /// </summary>
    [Fact]
    public async Task KeyBinding_ShouldExecuteAction_WhenCorrectKeyPressed()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();
        var mockKeyboard = new Mock<IKeyboard>();

        // Setup successful action execution
        var expectedResult = ActionResult.Successful("Action executed");
        mockActionFactory.Setup(af => af.ExecuteAsync(It.IsAny<ActionId>(), It.IsAny<IRuntimeComponent>()))
                        .ReturnsAsync(expectedResult);

        mockInputContext.Setup(ic => ic.Keyboards).Returns([mockKeyboard.Object]);

        var template = new KeyBinding.Template
        {
            Key = Key.F12,
            ActionId = ActionId.FromType(typeof(QuitGameAction)),
            Enabled = true
        };

        var keyBinding = new KeyBinding(mockInputContext.Object, mockActionFactory.Object)
        {
            Logger = mockLogger.Object
        };
        keyBinding.Configure(template);
        keyBinding.Activate();

        // Act - simulate key press by directly calling the protected method through reflection
        // Since we can't directly trigger Silk.NET events, we'll test the execution logic
        var method = typeof(InputBinding).GetMethod("ExecuteActionAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            var result = method.Invoke(keyBinding, ["F12 key press"]);
            if (result is Task task)
            {
                await task;
            }
        }

        // Assert
        mockActionFactory.Verify(af => af.ExecuteAsync(
            It.Is<ActionId>(id => id.ActionType == typeof(QuitGameAction)),
            keyBinding), Times.Once);
    }

    /// <summary>
    /// Tests that KeyBinding does not execute action when disabled
    /// </summary>
    [Fact]
    public async Task KeyBinding_ShouldNotExecuteAction_WhenDisabled()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var mockInputContext = new Mock<IInputContext>();
        var mockActionFactory = new Mock<IActionFactory>();
        var mockKeyboard = new Mock<IKeyboard>();

        mockInputContext.Setup(ic => ic.Keyboards).Returns([mockKeyboard.Object]);

        var template = new KeyBinding.Template
        {
            Key = Key.F12,
            ActionId = ActionId.FromType<ToggleFullscreenAction>(),
            Enabled = false // Disabled
        };

        var keyBinding = new KeyBinding(mockInputContext.Object, mockActionFactory.Object)
        {
            Logger = mockLogger.Object
        };
        keyBinding.Configure(template);
        keyBinding.Activate();

        // Act
        var method = typeof(InputBinding).GetMethod("ExecuteActionAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            var result = method.Invoke(keyBinding, ["F12 key press"]);
            if (result is Task task)
            {
                await task;
            }
        }

        // Assert
        mockActionFactory.Verify(af => af.ExecuteAsync(It.IsAny<ActionId>(), It.IsAny<IRuntimeComponent>()),
                                Times.Never);
    }
}