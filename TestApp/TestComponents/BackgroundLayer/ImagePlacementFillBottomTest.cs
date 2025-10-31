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
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : TestComponent.Template { }

    [Test("Image placement bottom center")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackground.Template()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillBottom
            }
        ]
    };

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var window = windowService.GetWindow();
        var offset = 2;
        var imageSize = 256;
        
        SampleCoordinates = [
            new(window.Size.X / 4, window.Size.Y - offset),         // Bottom-left quadrant
            new(window.Size.X / 2, window.Size.Y - offset),         // Bottom-center
            new(3 * window.Size.X / 4, window.Size.Y - offset),     // Bottom-right quadrant
        ];

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillBottom,
            imageSize, imageSize,
            window.Size.X, window.Size.Y);

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    new Vector4D<float>(0.25f * (uvMax.X - uvMin.X) + uvMin.X, uvMax.Y, 0f, 1f),  // Left quadrant
                    new Vector4D<float>(0.5f * (uvMax.X - uvMin.X) + uvMin.X, uvMax.Y, 0f, 1f),   // Center
                    new Vector4D<float>(0.75f * (uvMax.X - uvMin.X) + uvMin.X, uvMax.Y, 0f, 1f),  // Right quadrant
                }
            }
        };
    }
}
