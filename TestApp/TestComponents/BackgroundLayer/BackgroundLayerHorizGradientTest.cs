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
    public new record Template : RenderableTest.Template { }

    [Test("Horizontal gradient test")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new LinearGradientBackground.Template()
            {
                Gradient = GradientDefinition.TwoColor(
                    Colors.Blue,   // Position 0.0 (left)
                    Colors.Yellow  // Position 1.0 (right)
                )
            }
        ]
    };

    private IWindow window => windowService.GetWindow();

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var offset = 2;

        SampleCoordinates = [
            new(offset, window.Size.Y / 2),                  // Left edge → Red
            new(window.Size.X / 2, window.Size.Y / 2),       // Center → blend
            new(window.Size.X - offset, window.Size.Y / 2),  // Right edge → Green
        ];

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    Colors.Blue,
                    Colors.Lerp(Colors.Blue, Colors.Yellow, 0.5f),
                    Colors.Yellow
                }
            }
        };
    }
}