using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillLeft placement.
/// Validates that image anchors to left edge when cropping horizontally.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: When wide/short image, shows left and clips right; when narrow/tall, centers vertically
/// </summary>
public partial class ImagePlacementFillLeftTest(
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : TestComponent.Template { }

    [Test("Image placement middle left")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackground.Template()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillLeft
            }
        ]
    };

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var window = windowService.GetWindow();
        var offset = 2;
        var imageSize = 256;
        
        SampleCoordinates = [
            new(offset, window.Size.Y / 4),                         // Left edge, top quadrant
            new(offset, window.Size.Y / 2),                         // Left edge, center
            new(offset, 3 * window.Size.Y / 4),                     // Left edge, bottom quadrant
        ];

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillLeft,
            imageSize, imageSize,
            window.Size.X, window.Size.Y);

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    new Vector4D<float>(uvMin.X, 0.25f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Top quadrant
                    new Vector4D<float>(uvMin.X, 0.5f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),   // Center
                    new Vector4D<float>(uvMin.X, 0.75f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Bottom quadrant
                }
            }
        };
    }
}
