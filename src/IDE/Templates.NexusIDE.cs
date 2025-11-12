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
            new TextElementTemplate()
            {
                Name = "WelcomeText",
                Text = "Welcome to the Nexus",
                TextAlign = Align.MiddleCenter,
                // Center the text in the window
                LayoutHorizontal = HorizontalAlignment.Center,
                LayoutVertical = VerticalAlignment.Center
            },
            new TextElementTemplate()
            {
                Name = "TopLeftLabel",
                Text = "Top Left",
                TextAlign = Align.TopLeft,
                // Top-Left corner of the window
                LayoutHorizontal = HorizontalAlignment.Left,
                LayoutVertical = VerticalAlignment.Top
            },
            new TextElementTemplate()
            {
                Name = "TopRightLabel",
                Text = "Top Right",
                TextAlign = Align.TopRight,
                // Top-Right corner of the window
                LayoutHorizontal = HorizontalAlignment.Right,
                LayoutVertical = VerticalAlignment.Top
            },
            new TextElementTemplate()
            {
                Name = "BottomLeftLabel",
                Text = "Bottom Left",
                TextAlign = Align.BottomLeft,
                // Bottom-Left corner of the window
                LayoutHorizontal = HorizontalAlignment.Left,
                LayoutVertical = VerticalAlignment.Bottom
            },
            new TextElementTemplate()
            {
                Name = "BottomRightLabel",
                Text = "Bottom Right",
                TextAlign = Align.BottomRight,
                // Bottom-Right corner of the window
                LayoutHorizontal = HorizontalAlignment.Right,
                LayoutVertical = VerticalAlignment.Bottom
            },
        ]
    };
}
