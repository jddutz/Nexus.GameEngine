using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests BiaxialGradientBackground with 4-corner color interpolation.
/// Validates bilinear interpolation across the screen.
/// 
/// Single frame test: 
/// - Top-left: Red
/// - Top-right: Green
/// - Bottom-left: Blue
/// - Bottom-right: Yellow
/// </summary>
public partial class BiaxialGradientBackgroundTest(
    IPixelSampler pixelSampler
    ) : RenderableTest(pixelSampler)
{
    [Test("Biaxial gradient test")]
    public readonly static BiaxialGradientBackgroundTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new BiaxialGradientBackgroundTemplate()
            {
                TopLeft = Colors.Red,
                TopRight = Colors.Green,
                BottomLeft = Colors.Blue,
                BottomRight = Colors.Yellow
            }
        ]
    };
}
