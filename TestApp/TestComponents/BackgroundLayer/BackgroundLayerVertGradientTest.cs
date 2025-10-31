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
    public new record Template : TestComponent.Template { }

    [Test("Vertical gradient test")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new LinearGradientBackground.Template()
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

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var offset = 2;
        
        SampleCoordinates = [
            new(window.Size.X / 2, offset),                      // Top edge → Blue
            new(window.Size.X / 2, window.Size.Y / 2),          // Center → blend
            new(window.Size.X / 2, window.Size.Y - offset),     // Bottom edge → Yellow
        ];

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    Colors.Blue,                                  // Top: Blue
                    Colors.Lerp(Colors.Blue, Colors.Yellow, 0.5f), // Center: 50% blend
                    Colors.Yellow                                 // Bottom: Yellow
                }
            }
        };
    }
}