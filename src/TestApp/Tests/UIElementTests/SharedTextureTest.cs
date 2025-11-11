using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.UIElementTests;

/// <summary>
/// Tests multiple Elements sharing the same texture.
/// Validates that texture resources are properly shared and not duplicated.
/// Single frame test: 10 elements with same texture
/// </summary>
public partial class SharedTextureTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Shared texture test (10 Elements with same texture)")]
    public readonly static SharedTextureTestTemplate SharedTextureTestInstance = new()
    {
        Subcomponents = [
            // Create 10 elements in a grid, all sharing TestTexture
            new DrawableElementTemplate() { Name = "Element1", Position = ToCenteredPositionDefault(100, 50), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new DrawableElementTemplate() { Name = "Element2", Position = ToCenteredPositionDefault(250, 50), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new DrawableElementTemplate() { Name = "Element3", Position = ToCenteredPositionDefault(400, 50), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new DrawableElementTemplate() { Name = "Element4", Position = ToCenteredPositionDefault(550, 50), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new DrawableElementTemplate() { Name = "Element5", Position = ToCenteredPositionDefault(700, 50), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new DrawableElementTemplate() { Name = "Element6", Position = ToCenteredPositionDefault(100, 200), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new DrawableElementTemplate() { Name = "Element7", Position = ToCenteredPositionDefault(250, 200), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new DrawableElementTemplate() { Name = "Element8", Position = ToCenteredPositionDefault(400, 200), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new DrawableElementTemplate() { Name = "Element9", Position = ToCenteredPositionDefault(550, 200), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new DrawableElementTemplate() { Name = "Element10", Position = ToCenteredPositionDefault(700, 200), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        return [
            new(150, 100),    // Element 1
            new(300, 100),    // Element 2
            new(450, 100),    // Element 3
            new(150, 250),    // Element 6
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),    // Red
                new(1, 0, 0, 1),    // Red
                new(1, 0, 0, 1),    // Red
                new(1, 0, 0, 1),    // Red
            ]
        };
    }
}

