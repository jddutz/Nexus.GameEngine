using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Silk.NET.Maths;

namespace Nexus.IDE;

/// <summary>
/// Minimal templates used by the Nexus IDE test app.
/// Provides a single `NexusIDE` runtime template that displays a centered welcome message.
/// </summary>
public static partial class Templates
{
    public static readonly RuntimeComponentTemplate NexusIDE = new()
    {
        Name = "NexusIDE",
        Subcomponents =
        [
            new TextElementTemplate()
            {
                Name = "WelcomeText",
                Text = "Welcome to the Nexus",
                // Center the text in the window
                AnchorPoint = new Vector2D<float>(0.5f, 0.5f)
            }
        ]
    };
}
