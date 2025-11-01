using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillBottom placement.
/// Validates that image anchors to bottom edge when cropping vertically.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: When narrow/tall image, shows bottom and clips top; when wide/short, centers horizontally
/// </summary>
public partial class ImagePlacementFillBottomTest(
    IPixelSampler pixelSampler
    ) : RenderableTest(pixelSampler)
{
    [Test("Image placement bottom center")]
    public readonly static ImagePlacementFillBottomTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillBottom
            }
        ],
        SampleCoordinates = [
            new(960, 1079),   // Bottom center
            new(480, 1079),   // Bottom left quarter
            new(1440, 1079),  // Bottom right quarter
            new(960, 810),    // Above bottom
            new(960, 540)     // Center
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0.503f, 1, 0, 1),       // Bottom center: R=127.5, G=255 (960, 1079)
                new(0.25f, 1, 0, 1),        // Bottom left quarter (480, 1079)
                new(0.75f, 1, 0, 1),        // Bottom right quarter (1440, 1079)
                new(0.503f, 0.863f, 0, 1),  // Above bottom (960, 810)
                new(0.503f, 0.723f, 0, 1)   // Center (960, 540)
            ]
        }
    };
}
