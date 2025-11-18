using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Textures.Definitions;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.UIElementTests;

/// <summary>
/// Tests Element with white texture and colored tint.
/// Validates that TintColor properly multiplies with texture color.
/// Single frame test: White texture with red tint = red result
/// </summary>
public partial class TintColorTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Tint color test (white texture * red tint = red)")]
    public readonly static TintColorTestTemplate TintColorTestInstance = new()
    {
        Subcomponents = [
            new DrawableElementTemplate()
            {
                Alignment = Align.TopLeft,
                AnchorPoint = Align.TopLeft,
                OffsetLeft = 100,
                OffsetTop = 100,
                Size = new Vector2D<int>(150,100),
                Texture = TextureDefinitions.UniformColor,
                TintColor = Colors.Red,
            }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        return [
            new(200, 150),    // Center of tinted element
            new(150, 100),    // Top-left
            new(249, 199),    // Bottom-right
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),    // Red (white * red tint)
                new(1, 0, 0, 1),    // Red
                new(1, 0, 0, 1),    // Red
            ]
        };
    }
}

