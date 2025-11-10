using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Fonts;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.TextElementTests;

/// <summary>
/// Integration test: Verify text alignment with different anchor points.
/// Tests that AnchorPoint correctly positions text relative to Position coordinate.
/// </summary>
public partial class TextAnchorPointTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Text should align correctly with different anchor points")]
    public readonly static TextAnchorPointTestTemplate TextAnchorTestInstance = new()
    {
        FrameCount = 1,
        Subcomponents = [
            new TextElementTemplate()
            {
                Text = "Top-Left",
                Font = FontDefinitions.RobotoTitle,
                Position = new Vector3D<float>(100,100,0),
                TintColor = Colors.Yellow,
                AnchorPoint = new Vector2D<float>(-1, -1)
            },
            new TextElementTemplate()
            {
                Text = "Center",
                Font = FontDefinitions.RobotoTitle,
                Position = new Vector3D<float>(200,200,0),
                TintColor = Colors.Green,
                AnchorPoint = new Vector2D<float>(0, 0)
            },
            new TextElementTemplate()
            {
                Text = "Bottom-Right",
                Font = FontDefinitions.RobotoTitle,
                Position = new Vector3D<float>(300,300,0),
                TintColor = Colors.Cyan,
                AnchorPoint = new Vector2D<float>(1, 1)
            }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        // Compute samples based on current framebuffer size
        int width = Window.FramebufferSize.X;
        int height = Window.FramebufferSize.Y;

        // Background samples at corners (avoiding the very edge)
        int rightEdge = width - 100;
        int bottomEdge = height - 80;

        // Hard-coded coordinates to avoid antialiasing issues
        // These are adjusted from measured glyph centers to hit solid pixels
        return [
            // Background samples at corners
            new(100, 100),
            new(rightEdge, 100),
            new(100, bottomEdge),
            new(rightEdge, bottomEdge),
            
            // Yellow "Top-Left" text samples
            new(109, 115),  // First glyph 'T'
            new(198, 116),  // Last glyph 't'
            
            // Green "Center" text samples
            new(204, 215),  // First glyph 'C'
            new(272, 217),  // Last glyph 'r'
            
            // Cyan "Bottom-Right" text samples
            new(307, 315),  // First glyph 'B'
            new(456, 316),  // Last glyph 't'
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.Yellow,
                Colors.Yellow,
                Colors.Green,
                Colors.Green,
                Colors.Cyan,
                Colors.Cyan
            ]
        };
    }
}
