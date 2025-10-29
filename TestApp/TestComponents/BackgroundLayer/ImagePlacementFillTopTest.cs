using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
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
    public new record Template : TestComponent.Template { }

    [Test("Image placement top center")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackground.Template()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillTop
            }
        ]
    };

    private IWindow window => windowService.GetWindow();
    private const int ImageSize = 256;

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var window = windowService.GetWindow();
        var offset = 2;
        
        SampleCoordinates = [
            new(window.Size.X / 4, offset),                         // Top-left quadrant
            new(window.Size.X / 2, offset),                         // Top-center
            new(3 * window.Size.X / 4, offset),                     // Top-right quadrant
        ];

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillTop,
            ImageSize, ImageSize,
            window.Size.X, window.Size.Y);

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    new Vector4D<float>(0.25f * (uvMax.X - uvMin.X) + uvMin.X, uvMin.Y, 0f, 1f),  // Left quadrant
                    new Vector4D<float>(0.5f * (uvMax.X - uvMin.X) + uvMin.X, uvMin.Y, 0f, 1f),   // Center
                    new Vector4D<float>(0.75f * (uvMax.X - uvMin.X) + uvMin.X, uvMin.Y, 0f, 1f),  // Right quadrant
                }
            }
        };
    }
}
