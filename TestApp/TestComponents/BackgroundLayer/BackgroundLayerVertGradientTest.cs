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
    IPixelSampler pixelSampler,
    IWindowService windowService
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
        ]
    };

    private IWindow window => windowService.GetWindow();
}
