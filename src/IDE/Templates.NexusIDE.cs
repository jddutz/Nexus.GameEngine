using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
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
            new ElementTemplate()
            {
                Name = "WelcomeText",
                // Center the text in the window
                LayoutHorizontal = HorizontalAlignment.Center,
                HorizontalSizeMode = SizeMode.Stretch,
                LayoutVertical = VerticalAlignment.Center,
                VerticalSizeMode = SizeMode.Stretch,
                AnchorPoint = AnchorPoint.Center
            },
            new TextElementTemplate()
            {
                Name = "WelcomeText",
                Text = "Welcome to the Nexus",
                // Center the text in the window
                LayoutHorizontal = HorizontalAlignment.Center,
                LayoutVertical = VerticalAlignment.Center,
                AnchorPoint = AnchorPoint.Center
            },
            new TextElementTemplate()
            {
                Name = "TopLeftLabel",
                Text = "Top Left",
                // Top-Left corner of the window
                LayoutHorizontal = HorizontalAlignment.Left,
                LayoutVertical = VerticalAlignment.Top,
                AnchorPoint = new Vector2D<float>(-1f, -1f)
            },
            new TextElementTemplate()
            {
                Name = "TopRightLabel",
                Text = "Top Right",
                // Top-Right corner of the window
                LayoutHorizontal = HorizontalAlignment.Right,
                LayoutVertical = VerticalAlignment.Top,
                AnchorPoint = new Vector2D<float>(1f, -1f)
            },
            new TextElementTemplate()
            {
                Name = "BottomLeftLabel",
                Text = "Bottom Left",
                // Bottom-Left corner of the window
                LayoutHorizontal = HorizontalAlignment.Left,
                LayoutVertical = VerticalAlignment.Bottom,
                AnchorPoint = new Vector2D<float>(-1f, 1f)
            },
            new TextElementTemplate()
            {
                Name = "BottomRightLabel",
                Text = "Bottom Right",
                // Bottom-Right corner of the window
                LayoutHorizontal = HorizontalAlignment.Right,
                LayoutVertical = VerticalAlignment.Bottom,
                AnchorPoint = new Vector2D<float>(1f, 1f)
            },
        ]
    };
}
