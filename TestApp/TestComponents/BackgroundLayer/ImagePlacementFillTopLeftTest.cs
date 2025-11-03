using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillTopLeft placement.
/// Validates that image anchors to top-left corner when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Shows top-left portion, crops bottom and right when both dimensions need cropping
/// </summary>
public partial class ImagePlacementFillTopLeftTest(
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Image placement top left")]
    public readonly static ImagePlacementFillTopLeftTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillTopLeft
            }
        ],
        SampleCoordinates = [
            new(0, 0),        // Top-left corner
            new(480, 0),      // Top edge
            new(0, 270),      // Left edge
            new(480, 270),    // Interior
            new(960, 540)     // Further interior
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0, 0, 0, 1),            // Top-left visible (0, 0)
                new(0.25f, 0, 0, 1),        // 25% across (480, 0)
                new(0, 0.138f, 0, 1),       // 25% down (0, 270)
                new(0.25f, 0.138f, 0, 1),   // Interior (480, 270)
                new(0.503f, 0.279f, 0, 1)   // Center (960, 540)
            ]
        }
    };
}
