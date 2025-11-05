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
            new ElementTemplate() { Name = "Element1", Position = new Vector3D<float>(100, 50, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "Element2", Position = new Vector3D<float>(250, 50, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "Element3", Position = new Vector3D<float>(400, 50, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "Element4", Position = new Vector3D<float>(550, 50, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "Element5", Position = new Vector3D<float>(700, 50, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "Element6", Position = new Vector3D<float>(100, 200, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "Element7", Position = new Vector3D<float>(250, 200, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "Element8", Position = new Vector3D<float>(400, 200, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "Element9", Position = new Vector3D<float>(550, 200, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "Element10", Position = new Vector3D<float>(700, 200, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture }
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
