using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.UIElementTests;

/// <summary>
/// Tests texture atlas rendering with full UV coordinates.
/// Validates that atlas texture displays all quadrants correctly.
/// Atlas layout: [Red, BrightGreen]
///               [Blue, Yellow]
/// Single frame test: Display full atlas stretched to element size
/// </summary>
public partial class UVCoordinateTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("UV coordinate test (atlas full texture)")]
    public readonly static UVCoordinateTestTemplate UVCoordinateTestInstance = new()
    {
        Subcomponents = [
            new ElementTemplate()
            {
                Position = new Vector3D<float>(128, 128, 0),
                Size = new Vector2D<int>(128, 128),
                Texture = TestResources.TestAtlas,
                MinUV = new Vector2D<float>(0.0f, 0.0f), // top-left corner
                MaxUV = new Vector2D<float>(0.5f, 0.5f), // center
            }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        return [
            new(126, 126),   // Outside rect (top-left)
            new(126, 172),   // Outside rect (left edge)
            new(126, 300),   // Outside rect (bottom-left)
            new(130, 300),   // Outside rect (below)
            new(172, 126),   // Outside rect (above)
            new(172, 300),   // Outside rect (below)
            new(258, 126),   // Outside rect (top-right)
            new(258, 172),   // Outside rect (right edge)
            new(258, 300),    // Outside rect (bottom-right)
            new(130, 130),   // Inside rect (near top-left)
            new(130, 172),   // Inside rect (left edge)
            new(130, 254),   // Inside rect (near bottom-left)
            new(172, 130),   // Inside rect (top edge)
            new(172, 172),   // Inside rect (center)
            new(172, 254),   // Inside rect (bottom edge)
            new(254, 130),   // Inside rect (near top-right)
            new(254, 172),   // Inside rect (right edge)
            new(254, 254),   // Inside rect (near bottom-right)
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.Red,
                Colors.Red,
                Colors.Red,
                Colors.Red,
                Colors.Red,
                Colors.Red,
                Colors.Red,
                Colors.Red,
                Colors.Red,
            ],
            [1] = [
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.BrightGreen,
                Colors.BrightGreen,
                Colors.BrightGreen,
                Colors.BrightGreen,
                Colors.BrightGreen,
                Colors.BrightGreen,
                Colors.BrightGreen,
                Colors.BrightGreen,
                Colors.BrightGreen,
            ],
            [2] = [
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.Blue,
                Colors.Blue,
                Colors.Blue,
                Colors.Blue,
                Colors.Blue,
                Colors.Blue,
                Colors.Blue,
                Colors.Blue,
            ],
            [3] = [
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.Yellow,
                Colors.Yellow,
                Colors.Yellow,
                Colors.Yellow,
                Colors.Yellow,
                Colors.Yellow,
                Colors.Yellow,
                Colors.Yellow,
            ]
        };
    }

    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);

        var element = GetChildren<Element>().First();

        // Updates should be deferred, so the changes should be visible next frame
        switch (FramesRendered)
        {
            case 0:
                element.SetMinUV(new Vector2D<float>(0.5f, 0.0f));
                element.SetMaxUV(new Vector2D<float>(1.0f, 0.5f));
                break;
            case 1:
                element.SetMinUV(new Vector2D<float>(0.0f, 0.5f));
                element.SetMaxUV(new Vector2D<float>(0.5f, 1.0f));
                break;
            case 2:
                element.SetMinUV(new Vector2D<float>(0.5f, 0.5f));
                element.SetMaxUV(new Vector2D<float>(1.0f, 1.0f));
                break;
            default:
                break;
        }
    }
}
