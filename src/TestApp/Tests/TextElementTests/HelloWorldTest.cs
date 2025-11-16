using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Fonts;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.TextElementTests;

/// <summary>
/// Integration test: Basic "Hello, World!" text rendering at (100, 100).
/// Visual validation: Should see white text in Roboto font at specified position.
/// </summary>
public partial class HelloWorldTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Should render 'Hello, World!'")]
    public readonly static HelloWorldTestTemplate HelloWorldTestInstance = new()
    {
        FrameCount = 1,

        Subcomponents = [
            new TextElementTemplate()
            {
                Text = "Hello, World!",
                Font = FontDefinitions.RobotoTitle,
                Position = ToCenteredPositionDefault(100, 100),  // Top-left at (100, 100)
                TintColor = Colors.White,
                AnchorPoint = Align.TopLeft
            }
        ]
    };



    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        // Hard-coded coordinates to verify text appears at expected location (100, 100)
        // Text: "Hello, World!" with RobotoTitle font (32pt Roboto-Bold)
        // Position: (100, 100), AnchorPoint: (-1, -1) = top-left
        return [
            // Background samples (outside text area)
            new(50, 50),
            new(50, 100),
            new(300, 50),
            new(300, 150),
            
            // Text samples (verified solid pixel locations)
            new(109, 115),  // First glyph 'H' center
            new(247, 115),  // Last glyph '!' center
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>
        {
            [0] = [
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.White,
                Colors.White,
            ]
        };
    }
}
