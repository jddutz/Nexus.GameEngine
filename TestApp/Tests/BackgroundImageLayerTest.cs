using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests;

/// <summary>
/// Tests ImageTextureBackground with Stretch placement.
/// Validates that image stretches non-uniformly to fill viewport (may distort aspect ratio).
/// 
/// Uses image_test.png (256x256): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Entire texture visible (UV 0,0 to 1,1), potentially distorted to fill viewport
/// </summary>
public partial class BackgroundImageLayerTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Image placement stretch")]
    public readonly static BackgroundImageLayerTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new BackgroundImageLayerTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.Stretch
            }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        int width = Window.FramebufferSize.X;
        int height = Window.FramebufferSize.Y;
        
        return [
            new(0, 0),              // Top-left UV (0,0)
            new(width - 1, 0),      // Top-right UV (1,0)
            new(0, height - 2),     // Bottom-left UV (~0,1) - avoid edge
            new(width - 1, height - 2),  // Bottom-right UV (~1,1) - avoid edge
            new(width / 2, height / 2)   // Center UV (0.5,0.5)
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        int width = Window.FramebufferSize.X;
        int height = Window.FramebufferSize.Y;
        
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0, 0, 0, 1),                              // Top-left: R=0, G=0
                new(1, 0, 0, 1),                              // Top-right: R=255, G=0
                new(0, (height - 2f) / (height - 1f), 0, 1), // Bottom-left: R=0, G=~255
                new(1, (height - 2f) / (height - 1f), 0, 1), // Bottom-right: R=255, G=~255
                new(0.5f, 0.5f, 0, 1)                         // Center: R=127.5, G=127.5
            ]
        };
    }
}
