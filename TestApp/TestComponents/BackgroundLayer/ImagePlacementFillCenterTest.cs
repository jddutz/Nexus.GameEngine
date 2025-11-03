using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillCenter placement (DEFAULT).
/// Validates that image maintains aspect ratio, fills viewport, and centers when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Aspect ratio maintained, excess cropped equally from both sides, center visible
/// </summary>
public partial class ImagePlacementFillCenterTest(
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Image placement middle center")]
    public readonly static ImagePlacementFillCenterTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillCenter
            }
        ],
        SampleCoordinates = [
            new(960, 540),    // Center
            new(480, 270),    // Upper-left quadrant
            new(1440, 270),   // Upper-right quadrant
            new(480, 810),    // Lower-left quadrant
            new(1440, 810)    // Lower-right quadrant
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0.503f, 0.503f, 0, 1),  // Center: R=127.5, G=127.5 (960, 540)
                new(0.25f, 0.361f, 0, 1),   // Upper-left quadrant (480, 270)
                new(0.753f, 0.361f, 0, 1),  // Upper-right quadrant (1440, 270)
                new(0.25f, 0.644f, 0, 1),   // Lower-left quadrant (480, 810)
                new(0.753f, 0.644f, 0, 1)   // Lower-right quadrant (1440, 810)
            ]
        }
    };
}
