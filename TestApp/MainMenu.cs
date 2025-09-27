using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI.Components;

namespace NexusRealms.Prelude.Shared.UI;

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
            }
        ]
    };
}