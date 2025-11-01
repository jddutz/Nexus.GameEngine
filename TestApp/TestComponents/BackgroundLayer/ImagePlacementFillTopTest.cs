using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillTop placement.
/// Validates that image anchors to top edge when cropping vertically.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: When narrow/tall image, shows top and clips bottom; when wide/short, centers horizontally
/// </summary>
public partial class ImagePlacementFillTopTest(
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    [Test("Image placement top center")]
    public readonly static ImagePlacementFillTopTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillTop
            }
        ],
        SampleCoordinates = [
            new(960, 0),      // Top center
            new(480, 0),      // Top left quarter
            new(1440, 0),     // Top right quarter
            new(960, 270),    // Below top
            new(960, 540)     // Center
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0.503f, 0, 0, 1),       // Top center: R=127.5 (960, 0)
                new(0.25f, 0, 0, 1),        // Top left quarter: R=63.75 (480, 0)
                new(0.75f, 0, 0, 1),        // Top right quarter: R=191.25 (1440, 0)
                new(0.503f, 0.138f, 0, 1),  // Below top (960, 270)
                new(0.503f, 0.279f, 0, 1)   // Center (960, 540)
            ]
        }
    };

    private IWindow window => windowService.GetWindow();
    private const int ImageSize = 256;
}
