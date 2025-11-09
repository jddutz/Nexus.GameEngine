using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.UIElementTests;

/// <summary>
/// Tests Element with solid color (red tint, uniform color texture).
/// Validates that Element can render solid colors using UniformColor texture + TintColor.
/// Single frame test: Red rectangle at (100,100) size 200x100
/// </summary>
public partial class ColoredElementTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Colored element test (red tint with dummy texture)")]
    public readonly static ColoredElementTestTemplate ColoredElementTestInstance = new()
    {
            Subcomponents = [
            new DrawableElementTemplate()
            {
                Name = "RedElement",
                Position = new Vector3D<float>(100, 100, 0),
                Size = new Vector2D<int>(200, 100),
                TintColor = new Vector4D<float>(1, 0, 0, 1)  // Red
                // Texture defaults to UniformColor (1x1 white pixel)
            }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        return [
            new(200, 150),    // Center of red rectangle
            new(100, 100),    // Top-left corner
            new(299, 199),    // Bottom-right corner (inside)
            new(50, 50),      // Outside (should be background)
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
                Colors.DarkBlue,    // Dark blue background (default test background)
            ]
        };
    }
}
