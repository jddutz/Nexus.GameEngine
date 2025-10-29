using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
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
    public new record Template : TestComponent.Template { }

    [Test("Radial gradient test")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new RadialGradientBackground.Template()
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

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var offset = 2;
        
        SampleCoordinates = [
            new(window.Size.X / 2, window.Size.Y / 2),           // Center → White
            new(window.Size.X * 5 / 8, window.Size.Y / 2),       // Quarter-way → light gray
            new(window.Size.X - offset, window.Size.Y / 2),      // Right edge → Black
        ];

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    Colors.White,                                  // Center: t = 0.0
                    Colors.Lerp(Colors.White, Colors.Black, 0.44f),  // Quarter: t ≈ 0.44
                    Colors.Black                                   // Edge: t = 1.0 (clamped)
                }
            }
        };
    }
}