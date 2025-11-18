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
        Name = "NexusIDE Container",
        Padding = Padding.All(5),
        Subcomponents =
        [
            new VerticalLayoutTemplate()
            {
                Name = "VerticalLayout",
                Width = 300,
                VerticalSizeMode = SizeMode.Absolute,
                RelativeHeight = -200,
                Alignment = Align.MiddleLeft,
                AnchorPoint = Align.MiddleLeft,
                OffsetLeft = 100,
                Subcomponents = [
                    new DrawableElementTemplate()
                    {
                        Name = "Item1",
                        TintColor = Colors.Red
                    },
                    new DrawableElementTemplate()
                    {
                        Name = "Item2",
                        TintColor = Colors.Green
                    },
                    new DrawableElementTemplate()
                    {
                        Name = "Item3",
                        TintColor = Colors.Blue
                    },
                    new DrawableElementTemplate()
                    {
                        Name = "Item4",
                        TintColor = Colors.Yellow
                    },
                    new DrawableElementTemplate()
                    {
                        Name = "Item5",
                        TintColor = Colors.Cyan
                    },
                ]
            },
            new DrawableElementTemplate()
            {
                Name = "ContainerArea",
                TintColor = Colors.Cyan,
                Size = new Vector2D<int>(500,0),
            },
            new TextElementTemplate()
            {
                Name = "TopLeftLabel",
                Text = "Top Left",
                Alignment = Align.TopLeft,
                // Top-Left corner of the window
                AnchorPoint = Align.TopLeft
            },
            new TextElementTemplate()
            {
                Name = "TopRightLabel",
                Text = "Top Right",
                Alignment = Align.TopRight,
                // Top-Right corner of the window
                AnchorPoint = Align.TopRight
            },
            new TextElementTemplate()
            {
                Name = "BottomLeftLabel",
                Text = "Bottom Left",
                Alignment = Align.BottomLeft,
                // Bottom-Left corner of the window
                AnchorPoint = Align.BottomLeft
            },
            new TextElementTemplate()
            {
                Name = "BottomRightLabel",
                Text = "Bottom Right",
                Alignment = Align.BottomRight,
                // Bottom-Right corner of the window
                AnchorPoint = Align.BottomRight
            },
        ]
    };
}
