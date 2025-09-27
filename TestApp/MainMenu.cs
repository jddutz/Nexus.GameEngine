using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Input.Components;
using Silk.NET.Input;

namespace TestApp;

public static partial class Templates
{
    public static readonly RuntimeComponent.Template MainMenu = new()
    {
        // Set required properties here
        Name = "MainMenu",
        Subcomponents =
        [
            new BackgroundLayer.Template()
            {
                Name = "BackgroundLayer",
                BackgroundColor = Colors.CornflowerBlue
            },
            new TextElement.Template()
            {
                Name = "TextElement",
                Text = "Nexus Game Engine Test App"
            },
            new KeyBinding.Template()
            {
                Key = Key.F12,
                ActionId = ActionId.FromType<ToggleFullscreenAction>()
            },
            new KeyBinding.Template()
            {
                Key = Key.Escape,
                ActionId = ActionId.FromType<QuitGameAction>()
            }
        ]
    };
}