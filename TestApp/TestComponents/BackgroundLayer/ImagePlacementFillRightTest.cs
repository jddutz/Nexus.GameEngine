using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillRight placement.
/// Validates that image anchors to right edge when cropping horizontally.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: When wide/short image, shows right and clips left; when narrow/tall, centers vertically
/// </summary>
public partial class ImagePlacementFillRightTest(
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : TestComponent.Template { }

    [Test("Image placement middle right")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackground.Template()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillRight
            }
        ]
    };

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var window = windowService.GetWindow();
        var offset = 2;
        var imageSize = 256;
        
        SampleCoordinates = [
            new(window.Size.X - offset, window.Size.Y / 4),         // Right edge, top quadrant
            new(window.Size.X - offset, window.Size.Y / 2),         // Right edge, center
            new(window.Size.X - offset, 3 * window.Size.Y / 4),     // Right edge, bottom quadrant
        ];

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillRight,
            imageSize, imageSize,
            window.Size.X, window.Size.Y);

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    new Vector4D<float>(uvMax.X, 0.25f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Top quadrant
                    new Vector4D<float>(uvMax.X, 0.5f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),   // Center
                    new Vector4D<float>(uvMax.X, 0.75f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Bottom quadrant
                }
            }
        };
    }
}
