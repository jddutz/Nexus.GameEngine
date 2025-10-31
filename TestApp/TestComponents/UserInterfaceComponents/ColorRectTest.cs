using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.UserInterfaceComponents;

/// <summary>
/// Tests ColorRect component with basic rendering.
/// Validates that the component renders a colored rectangle at the correct pixel position.
/// 
/// Test: Red rectangle near top-left with margin (20% margin, 20% size in pixels)
/// This leaves space at the top and left edges to sample the background.
/// </summary>
public partial class ColorRectSimpleTest(
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : RenderableTest.Template { }

    [Test("Simple colored rectangle test")]
    public readonly static Template ColorRectTest = new()
    {
        Subcomponents = [
            new Element.Template()
            {
                TintColor = Colors.Red,
                PreferredSize = new Vector2D<int>(200, 100),
                Visible = true
            }
        ]
    };

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        FrameCount = 1600;

        var offset = 2;
        var window = windowService.GetWindow();
        var w = window.Size.X;
        var h = window.Size.Y;

        // Sample points:
        SampleCoordinates = [
            // Background margin (top-left corner area before rectangle starts)
            new(offset, offset),
            new(200, offset),
            new(offset, 150),

            // Inside the red rectangle
            new(102, 102),
            new(200, 150),
            new(298, 198),

            // Outside the rectangle (far corner)
            new(w - offset, h - offset)
        ];

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    // Background margin -> DarkBlue
                    Colors.DarkBlue,
                    Colors.DarkBlue,
                    Colors.DarkBlue,

                    // Inside rectangle -> Red
                    Colors.Red,
                    Colors.Red,
                    Colors.Red,

                    // Outside rectangle -> DarkBlue
                    Colors.DarkBlue
                }
            }
        };
    }
}
