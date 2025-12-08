using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Performance;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Textures.Definitions;
using Nexus.GameEngine.Templates;
using Silk.NET.Maths;

namespace Nexus.IDE;

/// <summary>
/// Main template used by the Nexus IDE test app.
/// </summary>
public static partial class Templates
{
    public static readonly UserInterfaceElementTemplate NexusIDE = new()
    {
        Name = "NexusIDE Container",
        // Fill the entire window
        HorizontalSizeMode = SizeMode.Relative,
        VerticalSizeMode = SizeMode.Relative,
        RelativeSize = new Vector2D<float>(1.0f, 1.0f),  // 100% of parent (window)
        Padding = Padding.All(20),
        Subcomponents =
        [
            new LayoutControllerTemplate(),
            
            // Simple test - one box top-left
            new TextureRectTemplate()
            {
                Name = "RedBox",
                Texture = TextureDefinitions.UniformColor,
                Color = Colors.Red,
                Size = new Vector2D<float>(140, 100),
                HorizontalSizeMode = SizeMode.Fixed,
                VerticalSizeMode = SizeMode.Fixed,
                Pivot = Align.TopLeft,  // Explicit
                Alignment = Align.TopLeft
            },
            
            // Simple test - one box at center
            new TextureRectTemplate()
            {
                Name = "GreenBox",
                Texture = TextureDefinitions.UniformColor,
                Color = Colors.Green,
                Size = new Vector2D<float>(120, 80),
                HorizontalSizeMode = SizeMode.Fixed,
                VerticalSizeMode = SizeMode.Fixed,
                Pivot = Align.MiddleCenter,
                Alignment = Align.MiddleCenter
            },
            
            // Simple test - one box middle-left
            new TextureRectTemplate()
            {
                Name = "BlueBox",
                Texture = TextureDefinitions.UniformColor,
                Color = Colors.Blue,
                Size = new Vector2D<float>(100, 60),
                HorizontalSizeMode = SizeMode.Fixed,
                VerticalSizeMode = SizeMode.Fixed,
                Pivot = Align.TopLeft,
                Alignment = Align.MiddleLeft
            }
        ]
    };
}
