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
/// Tests RadialGradientBackground component.
/// Validates that radial gradient shaders work correctly.
/// 
/// Single frame test: White (center) → Black (edges) from center point (0.5, 0.5)
/// </summary>
public partial class BackgroundLayerRadialGradientTest(
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
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
        ]
    };

    private IWindow window => windowService.GetWindow();
}
