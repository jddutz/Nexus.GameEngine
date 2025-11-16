using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.UIElementTests;

/// <summary>
/// Tests ColorRect component with basic rendering using NDC coordinates and identity matrix.
/// </summary>
public partial class ColorRectTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("ColorRect test")]
    public readonly static ColorRectTestTemplate TestTemplate = new()
    {
        FrameCount = 1,
        Subcomponents = [
            new DrawableElementTemplate()
            {
                TintColor = Colors.Red,
                // Set proper pixel-space transform: 200x100 rectangle from (100,100) to (300,200)
                // Position is where the AnchorPoint is located
                // AnchorPoint sets the anchor, so Position=(100,100) in top-left coords
                // gets converted to centered coords for the camera system
                Position = ToCenteredPositionDefault(100, 100),
                Size = new Vector2D<int>(200, 100),
                AnchorPoint = Align.TopLeft,
                Visible = true
            }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        return [
            new(50, 50),   // Outside rect (top-left) - should be blue
            new(50, 150),   // Outside rect (left edge) - should be blue
            new(50, 250),   // Outside rect (bottom-left) - should be blue
            new(102, 102),   // Inside rect (near top-left) - should be red
            new(102, 150),   // Inside rect (left edge) - should be red
            new(102, 198),   // Inside rect (near bottom-left) - should be red
            new(102, 250),   // Outside rect (below) - should be blue
            new(200, 50),   // Outside rect (above) - should be blue
            new(200, 102),   // Inside rect (top edge) - should be red
            new(200, 150),   // Inside rect (center) - should be red
            new(200, 198),   // Inside rect (bottom edge) - should be red
            new(200, 250),   // Outside rect (below) - should be blue
            new(298, 102),   // Inside rect (near top-right) - should be red
            new(298, 150),   // Inside rect (right edge) - should be red
            new(298, 198),   // Inside rect (near bottom-right) - should be red
            new(350, 50),   // Outside rect (top-right) - should be blue
            new(350, 150),   // Outside rect (right edge) - should be blue
            new(350, 250),   // Outside rect (bottom-right) - should be blue
            new(960, 540),   // Outside rect (screen center) - should be blue
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                Colors.DarkBlue,  // (50, 50)
                Colors.DarkBlue,  // (50, 150)
                Colors.DarkBlue,  // (50, 250)
                Colors.Red,   // (102, 102)
                Colors.Red,   // (102, 150)
                Colors.Red,   // (102, 198)
                Colors.DarkBlue,  // (102, 250)
                Colors.DarkBlue,  // (200, 50)
                Colors.Red,   // (200, 102)
                Colors.Red,   // (200, 150)
                Colors.Red,   // (200, 198)
                Colors.DarkBlue,  // (200, 250)
                Colors.Red,   // (298, 102)
                Colors.Red,   // (298, 150)
                Colors.Red,   // (298, 198)
                Colors.DarkBlue,  // (350, 50)
                Colors.DarkBlue,  // (350, 150)
                Colors.DarkBlue,  // (350, 250)
                Colors.DarkBlue,  // (960, 540)
            ]
        };
    }
}
