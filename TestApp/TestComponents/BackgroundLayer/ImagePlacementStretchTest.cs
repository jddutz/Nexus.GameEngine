using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with Stretch placement.
/// Validates that image stretches non-uniformly to fill viewport (may distort aspect ratio).
/// 
/// Uses image_test.png (256x256): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Entire texture visible (UV 0,0 to 1,1), potentially distorted to fill viewport
/// </summary>
public partial class ImagePlacementStretchTest(
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : TestComponent.Template { }

    [Test("Image placement stretch")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackground.Template()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.Stretch
            }
        ]
    };

    // Helper to convert UV coordinates to expected RGB color from image_test.png
    // UV coordinates are [0,1], image pixels are [0,255]
    private static Vector4D<float> UVToExpectedColor(float u, float v)
    {
        // R channel = X coordinate (0-255) normalized to [0,1]
        // G channel = Y coordinate (0-255) normalized to [0,1]
        var x = u * 255f;
        var y = v * 255f;
        return new Vector4D<float>(x / 255f, y / 255f, 0f, 1f);
    }

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var window = windowService.GetWindow();
        var offset = 2;
        
        SampleCoordinates = [
            new(offset, offset),                                    // Top-left corner -> UV(0,0) -> RGB(0,0,0)
            new(window.Size.X - offset, offset),                    // Top-right corner -> UV(1,0) -> RGB(255,0,0)
            new(offset, window.Size.Y - offset),                    // Bottom-left corner -> UV(0,1) -> RGB(0,255,0)
            new(window.Size.X - offset, window.Size.Y - offset),    // Bottom-right corner -> UV(1,1) -> RGB(255,255,0)
            new(window.Size.X / 2, window.Size.Y / 2),              // Center -> UV(0.5,0.5) -> RGB(127,127,0)
        ];

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    UVToExpectedColor(0f, 0f),      // Top-left
                    UVToExpectedColor(1f, 0f),      // Top-right
                    UVToExpectedColor(0f, 1f),      // Bottom-left
                    UVToExpectedColor(1f, 1f),      // Bottom-right
                    UVToExpectedColor(0.5f, 0.5f),  // Center
                }
            }
        };
    }
}
