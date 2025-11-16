using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Resources;
using Silk.NET.Maths;

namespace Nexus.IDE;

/// <summary>
/// Main template used by the Nexus IDE test app.
/// </summary>
public static partial class Templates
{
    public static readonly ContainerTemplate NexusIDE = new()
    {
        Name = "NexusIDE",
        SizeMode = SizeMode.Stretch,
        Padding = Padding.All(5),
        Subcomponents =
        [
            new VerticalLayoutTemplate()
            {
                Name = "VerticalLayout",
                Alignment = Align.MiddleCenter,
                Subcomponents = [
                    new TextElementTemplate()
                    {
                        Name = "Label1",
                        Text = "Welcome",
                        TextAlign = Align.MiddleCenter
                    },
                ]
            },
            new TextElementTemplate()
            {
                Name = "TopLeftLabel",
                Text = "Top Left",
                TextAlign = Align.TopLeft,
                // Top-Left corner of the window
                Alignment = Align.TopLeft
            },
            new TextElementTemplate()
            {
                Name = "TopRightLabel",
                Text = "Top Right",
                TextAlign = Align.TopRight,
                // Top-Right corner of the window
                Alignment = Align.TopRight
            },
            new TextElementTemplate()
            {
                Name = "BottomLeftLabel",
                Text = "Bottom Left",
                TextAlign = Align.BottomLeft,
                // Bottom-Left corner of the window
                Alignment = Align.BottomLeft
            },
            new TextElementTemplate()
            {
                Name = "BottomRightLabel",
                Text = "Bottom Right",
                TextAlign = Align.BottomRight,
                // Bottom-Right corner of the window
                Alignment = Align.BottomRight
            },
        ]
    };
}
