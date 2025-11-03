using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests RadialGradientBackground component.
/// Validates that radial gradient shaders work correctly.
/// 
/// Single frame test: White (center) → Black (edges) from center point (0.5, 0.5)
/// </summary>
public partial class BackgroundLayerRadialGradientTest(
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Radial gradient test")]
    public readonly static BackgroundLayerRadialGradientTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new RadialGradientBackgroundTemplate()
            {
                Gradient = GradientDefinition.TwoColor(
                    Colors.White,  // Position 0.0 (center)
                    Colors.Black   // Position 1.0 (edges)
                ),
                // Center of screen in normalized [0,1] coordinates
                Center = new Vector2D<float>(0.5f, 0.5f),
                // Radius that reaches edges
                Radius = 0.5f
            }
        ],
        SampleCoordinates = [
            new(960, 540),    // Center - white
            new(720, 540),    // 25% from center - light gray
            new(480, 540),    // 50% from center - medium gray
            new(240, 540),    // 75% from center - dark gray
            new(0, 540)       // Edge - black
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 1, 1, 1),              // White center (960, 540)
                new(0.558f, 0.558f, 0.558f, 1),  // Light gray (720, 540)
                new(0.112f, 0.112f, 0.112f, 1),  // Medium gray (480, 540)
                new(0.000f, 0.000f, 0.000f, 1),  // Dark gray (240, 540)
                new(0, 0, 0, 1)               // Black edge (0, 540)
            ]
        }
    };
}
