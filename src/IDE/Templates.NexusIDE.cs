using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Resources;

namespace Nexus.IDE;

/// <summary>
/// Main template used by the Nexus IDE test app.
/// </summary>
public static partial class Templates
{
    public static readonly UserInterfaceElementTemplate NexusIDE = new()
    {
        Name = "NexusIDE Container",
        Padding = Padding.All(5),
        Subcomponents =
        [
            // Layout system is being refactored.
            // Old templates removed.
        ]
    };
}
