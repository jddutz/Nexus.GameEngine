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
            // Vertical layout controller - try Distributed spacing for vertical centering effect
            new VerticalLayoutControllerTemplate
            {
                ItemSpacing = 15.0f,
                Alignment = 0.5f,  // Center horizontally
                Spacing = SpacingMode.Distributed  // Equal spacing before, between, and after
            },
            
            // Red box
            new TextureRectTemplate()
            {
                Name = "RedBox",
                Texture = TextureDefinitions.UniformColor,
                Color = Colors.Red,
                Size = new Vector2D<float>(200, 80),
                HorizontalSizeMode = SizeMode.Fixed,
                VerticalSizeMode = SizeMode.Fixed
            },
            
            // Green box
            new TextureRectTemplate()
            {
                Name = "GreenBox",
                Texture = TextureDefinitions.UniformColor,
                Color = Colors.Green,
                Size = new Vector2D<float>(150, 60),
                HorizontalSizeMode = SizeMode.Fixed,
                VerticalSizeMode = SizeMode.Fixed
            },
            
            // Blue box
            new TextureRectTemplate()
            {
                Name = "BlueBox",
                Texture = TextureDefinitions.UniformColor,
                Color = Colors.Blue,
                Size = new Vector2D<float>(180, 100),
                HorizontalSizeMode = SizeMode.Fixed,
                VerticalSizeMode = SizeMode.Fixed
            }
        ]
    };
}
