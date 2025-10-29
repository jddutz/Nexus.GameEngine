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
    /// </summary>
    public static readonly Component.Template MainMenu = new()
    {
        // Set required properties here
        Name = "MainMenu",
        Subcomponents =
        [
            new TestRunner.Template()
            {
                Name = "Test Runner"
            },
            new KeyBinding.Template()
            {
                Name = "Toggle FullScreen (F12)",
                Key = Key.F12,
                ActionId = ActionId.FromType<ToggleFullscreenAction>()
            },
            new KeyBinding.Template()
            {
                Name = "Quit (ESC)",
                Key = Key.Escape,
                ActionId = ActionId.FromType<QuitGameAction>()
            }
        ]
    };
}