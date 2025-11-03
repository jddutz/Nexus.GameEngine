using Nexus.GameEngine;
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
/// Tests LinearGradientBackground with horizontal gradient.
/// Validates that gradient shaders and UBO/descriptor system work correctly.
/// 
/// Single frame test: Red (left) → Green (right) at angle 0°
/// </summary>
public partial class BackgroundLayerHorizGradientTest(
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Horizontal gradient test")]
    public readonly static BackgroundLayerHorizGradientTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new LinearGradientBackgroundTemplate()
            {
                Gradient = GradientDefinition.TwoColor(
                    Colors.Blue,   // Position 0.0 (left)
                    Colors.Yellow  // Position 1.0 (right)
                )
            }
        ],

        SampleCoordinates = [
            new(0, 540),      // Far left - blue
            new(480, 540),    // 25% - blue-green blend
            new(960, 540),    // Center - green
            new(1440, 540),   // 75% - green-yellow blend
            new(1919, 540)    // Far right - yellow
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0, 0, 1, 1),              // Blue (0, 540)
                new(0.250f, 0.250f, 0.745f, 1),  // Blue-cyan blend (480, 540)
                new(0.503f, 0.503f, 0.497f, 1),  // Cyan midpoint (960, 540)
                new(0.753f, 0.753f, 0.250f, 1),  // Cyan-yellow blend (1440, 540)
                new(1, 1, 0, 1)               // Yellow (1919, 540)
            ]
        }
    };
}
