using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.UIElementTests;

/// <summary>
/// Tests multiple Elements with different textures.
/// Validates texture resource management and descriptor set creation for multiple textures.
/// Single frame test: 3 rectangles with different textures
/// </summary>
public partial class MultiTextureTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Multi-texture test (3 Elements with different textures)")]
    public readonly static MultiTextureTestTemplate MultiTextureTestInstance = new()
    {
        Subcomponents = [
            // Element 1: Red texture
            new ElementTemplate()
            {
                Name = "RedElement",
                Position = new Vector3D<float>(100, 100, 0),
                Size = new Vector2D<int>(100, 100),
                Texture = TestResources.TestTexture
            },
            // Element 2: White texture
            new ElementTemplate()
            {
                Name = "WhiteElement",
                Position = new Vector3D<float>(400, 100, 0),
                Size = new Vector2D<int>(100, 100),
                Texture = TestResources.WhiteTexture
            },
            // Element 3: Icon texture
            new ElementTemplate()
            {
                Name = "IconElement",
                Position = new Vector3D<float>(700, 100, 0),
                Size = new Vector2D<int>(100, 100),
                Texture = TestResources.TestIcon
            }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        return [
            new(150, 150),    // Center of red element
            new(450, 150),    // Center of white element
            new(750, 150),    // Center of icon element
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),      // Red
                new(1, 1, 1, 1),      // White
                new(1, 1, 1, 1),      // White (icon center)
            ]
        };
    }
}
