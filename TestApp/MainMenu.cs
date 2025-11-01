using Nexus.GameEngine.Actions;
using Nexus.GameEngine.GUI;
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
    /// </summary>
    public static readonly ElementTemplate MainMenu = new()
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
}
