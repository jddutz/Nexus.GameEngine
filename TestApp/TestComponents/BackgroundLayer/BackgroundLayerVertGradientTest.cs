using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests LinearGradientBackground with vertical gradient.
/// Validates that gradient shaders work correctly with angle = 90°.
/// 
/// Single frame test: Blue (top) → Yellow (bottom) at angle 90°
/// </summary>
public partial class BackgroundLayerVertGradientTest(
    IPixelSampler pixelSampler
    ) : RenderableTest(pixelSampler)
{
    [Test("Vertical gradient test")]
    public readonly static BackgroundLayerVertGradientTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new LinearGradientBackgroundTemplate()
            {
                Gradient = GradientDefinition.TwoColor(
                    Colors.Blue,   // Position 0.0 (top when angle = 90°)
                    Colors.Yellow  // Position 1.0 (bottom when angle = 90°)
                ),
                Angle = MathF.PI / 2f  // Vertical (90 degrees = π/2 radians)
            }
        ],
        SampleCoordinates = [
            new(960, 0),      // Top - blue
            new(960, 270),    // 25% down - blue-cyan blend
            new(960, 540),    // Center - cyan
            new(960, 810),    // 75% down - cyan-yellow blend
            new(960, 1079)    // Bottom - yellow
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0, 0, 1, 1),              // Blue (960, 0)
                new(0.250f, 0.250f, 0.745f, 1),  // Blue-cyan blend (960, 270)
                new(0.503f, 0.503f, 0.497f, 1),  // Cyan midpoint (960, 540)
                new(0.753f, 0.753f, 0.250f, 1),  // Cyan-yellow blend (960, 810)
                new(1, 1, 0, 1)               // Yellow (960, 1079)
            ]
        }
    };
}
