using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
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
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : RenderableTest(pixelSampler, renderer)
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
        ],
        SampleCoordinates = [
            new(0, 0),        // Top-left - red
            new(1919, 0),     // Top-right - green
            new(0, 1079),     // Bottom-left - blue
            new(1919, 1079),  // Bottom-right - yellow
            new(960, 540)     // Center - blend of all
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),              // Red (0, 0)
                new(0.001f, 0.503f, 0.000f, 1),  // Green (1919, 0)
                new(0, 0, 1, 1),              // Blue (0, 1079)
                new(1, 1, 0, 1),              // Yellow (1919, 1079)
                new(0.503f, 0.376f, 0.250f, 1)  // Gray blend at center (960, 540)
            ]
        }
    };
}
