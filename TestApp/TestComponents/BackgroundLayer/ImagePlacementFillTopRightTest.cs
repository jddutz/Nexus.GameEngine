using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillTopRight placement.
/// Validates that image anchors to top-right corner when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Shows top-right portion, crops bottom and left when both dimensions need cropping
/// </summary>
public partial class ImagePlacementFillTopRightTest(
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Image placement top right")]
    public readonly static ImagePlacementFillTopRightTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillTopRight
            }
        ],
        SampleCoordinates = [
            new(1919, 0),     // Top-right corner
            new(1440, 0),     // Top, 75% across
            new(1919, 270),   // Right edge, 25% down
            new(1440, 270),   // Interior
            new(960, 540)     // Center
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),            // Top-right: R=255 (1919, 0)
                new(0.753f, 0, 0, 1),       // 75% across (1440, 0)
                new(1, 0.138f, 0, 1),       // Right, 25% down (1919, 270)
                new(0.753f, 0.138f, 0, 1),  // Interior (1440, 270)
                new(0.503f, 0.279f, 0, 1)   // Center (960, 540)
            ]
        }
    };
}
