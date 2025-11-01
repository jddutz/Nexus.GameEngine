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
/// Tests LinearGradientBackground with horizontal gradient.
/// Validates that gradient shaders and UBO/descriptor system work correctly.
/// 
/// Single frame test: Red (left) → Green (right) at angle 0°
/// </summary>
public partial class BackgroundLayerHorizGradientTest(
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
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
        ]
    };

    private IWindow window => windowService.GetWindow();
}
