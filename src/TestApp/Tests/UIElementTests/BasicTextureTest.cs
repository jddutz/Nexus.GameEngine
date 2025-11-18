using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.UIElementTests;

/// <summary>
/// Tests Element with a real texture (test_texture.png - solid red).
/// Validates that Element can load and display external textures.
/// Single frame test: Red textured rectangle at (100,100) size 256x256
/// </summary>
public partial class BasicTextureTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Basic texture test (Element with test_texture.png)")]
    public readonly static BasicTextureTestTemplate BasicTextureTestInstance = new()
    {
            Subcomponents = [
            new DrawableElementTemplate()
            {
                Name = "TexturedElement",
                Alignment = Align.TopLeft,
                AnchorPoint = Align.TopLeft,
                OffsetLeft = 100,
                OffsetTop = 100,
                Size = new Vector2D<int>(256, 256),
                Texture = TestResources.TestTexture  // Solid red texture
            }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        return [
            new(228, 228),    // Center of textured rectangle
            new(100, 100),    // Top-left corner
            new(355, 355),    // Bottom-right corner (inside)
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),    // Red center
                new(1, 0, 0, 1),    // Red top-left
                new(1, 0, 0, 1),    // Red bottom-right
            ]
        };
    }
}
