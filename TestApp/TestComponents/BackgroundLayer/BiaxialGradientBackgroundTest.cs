using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI.Components;
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
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : TestComponent.Template { }

    [Test("Biaxial gradient test")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new BiaxialGradientBackground.Template()
            {
                TopLeft = Colors.Red,
                TopRight = Colors.Green,
                BottomLeft = Colors.Blue,
                BottomRight = Colors.Yellow
            }
        ]
    };

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        var window = windowService.GetWindow();
        var offset = 2;

        SampleCoordinates = [
            new(offset, offset),                                  // Top-left → Red
            new(window.Size.X - offset, offset),                  // Top-right → Green
            new(offset, window.Size.Y - offset),                  // Bottom-left → Blue
            new(window.Size.X - offset, window.Size.Y - offset),  // Bottom-right → Yellow
            new(window.Size.X / 2, window.Size.Y / 2),           // Center → blend of all 4
        ];
        
        // Expected colors:
        // Corners should be exact, center should be average of all 4
        var topEdge = Colors.Lerp(Colors.Red, Colors.Green, 0.5f);      // Top center
        var bottomEdge = Colors.Lerp(Colors.Blue, Colors.Yellow, 0.5f); // Bottom center
        var center = Colors.Lerp(topEdge, bottomEdge, 0.5f);             // True center

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    Colors.Red,     // Top-left corner
                    Colors.Green,   // Top-right corner
                    Colors.Blue,    // Bottom-left corner
                    Colors.Yellow,  // Bottom-right corner
                    center          // Center point (blend of all 4)
                }
            }
        };
    }
}
