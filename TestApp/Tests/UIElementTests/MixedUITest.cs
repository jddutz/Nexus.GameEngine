using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.UIElementTests;

/// <summary>
/// Tests mixed Elements: some with solid colors (UniformColor + tint), some with textures.
/// Validates that uber-shader handles both cases without pipeline switches.
/// Single frame test: 5 elements total (3 solid colors, 2 textured)
/// </summary>
public partial class MixedUITest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Mixed UI test (solid colors + textures, no pipeline switches)")]
    public readonly static MixedUITestTemplate MixedUITestInstance = new()
    {
        Subcomponents = [
            // Row 1: Solid colors (UniformColor + tint)
            new ElementTemplate() { Name = "Red", Position = new Vector3D<float>(100, 100, 0), Size = new Vector2D<int>(100, 100), TintColor = new Vector4D<float>(1, 0, 0, 1) },
            new ElementTemplate() { Name = "Green", Position = new Vector3D<float>(300, 100, 0), Size = new Vector2D<int>(100, 100), TintColor = new Vector4D<float>(0, 1, 0, 1) },
            new ElementTemplate() { Name = "Blue", Position = new Vector3D<float>(500, 100, 0), Size = new Vector2D<int>(100, 100), TintColor = new Vector4D<float>(0, 0, 1, 1) },
            // Row 2: Textured elements
            new ElementTemplate() { Name = "RedTexture", Position = new Vector3D<float>(100, 300, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.TestTexture },
            new ElementTemplate() { Name = "WhiteTexture", Position = new Vector3D<float>(300, 300, 0), Size = new Vector2D<int>(100, 100), Texture = TestResources.WhiteTexture }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        return [
            new(150, 150),    // Red solid
            new(350, 150),    // Green solid
            new(550, 150),    // Blue solid
            new(150, 350),    // Red texture
            new(350, 350),    // White texture
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),      // Red solid
                new(0, 1, 0, 1),      // Green solid
                new(0, 0, 1, 1),      // Blue solid
                new(1, 0, 0, 1),      // Red texture
                new(1, 1, 1, 1),      // White texture
            ]
        };
    }
}
