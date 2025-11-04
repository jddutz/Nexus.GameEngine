using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Input.Components;
using Silk.NET.Input;

namespace TestApp;

/// <summary>
/// Provides declarative templates for application components used in TestApp.
/// </summary>
public static partial class Templates
{
    /// <summary>
    /// MainMenu template defines the root component tree for the TestApp main menu.
    /// Includes the TestRunner and key bindings for toggling fullscreen and quitting the application.
    /// Uses RuntimeComponentTemplate (non-drawable) since it's just a container for test infrastructure.
    /// </summary>
    public static readonly RuntimeComponentTemplate MainMenu = new()
    {
        // Set required properties here
        Name = "MainMenu",
        Subcomponents =
        [
            new TestRunnerTemplate()
            {
                Name = "Test Runner"
            },
            new KeyBindingTemplate()
            {
                Name = "Toggle FullScreen (F12)",
                Key = Key.F12,
                ActionId = ActionId.FromType<ToggleFullscreenAction>()
            },
            new KeyBindingTemplate()
            {
                Name = "Quit (ESC)",
                Key = Key.Escape,
                ActionId = ActionId.FromType<QuitGameAction>()
            }
        ]
    };

    /// <summary>
    /// Creates a MainMenu template with a test filter applied to the TestRunner.
    /// </summary>
    /// <param name="testFilter">Pattern to filter test names (case-insensitive substring match).</param>
    /// <returns>RuntimeComponentTemplate configured with the test filter.</returns>
    public static RuntimeComponentTemplate CreateMainMenuWithFilter(string testFilter)
    {
        return new RuntimeComponentTemplate()
        {
            Name = "MainMenu",
            Subcomponents =
            [
                new TestRunnerTemplate()
                {
                    Name = "Test Runner",
                    TestFilter = testFilter
                },
                new KeyBindingTemplate()
                {
                    Name = "Toggle FullScreen (F12)",
                    Key = Key.F12,
                    ActionId = ActionId.FromType<ToggleFullscreenAction>()
                },
                new KeyBindingTemplate()
                {
                    Name = "Quit (ESC)",
                    Key = Key.Escape,
                    ActionId = ActionId.FromType<QuitGameAction>()
                }
            ]
        };
    }
}
