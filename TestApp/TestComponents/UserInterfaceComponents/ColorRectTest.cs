using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.UserInterfaceComponents;

/// <summary>
/// Tests ColorRect component with basic rendering.
/// Validates that the component renders a colored rectangle at the correct pixel position.
/// 
/// Test: Red rectangle near top-left with margin (20% margin, 20% size in pixels)
/// This leaves space at the top and left edges to sample the background.
/// </summary>
public partial class ColoredRectTest(
    IPixelSampler pixelSampler
    ) : RenderableTest(pixelSampler)
{
    [Test("ColorRect test")]
    public readonly static ColoredRectTestTemplate TestTemplate = new()
    {
        Subcomponents = [
            new ElementTemplate()
            {
                TintColor = Colors.Red,
                Bounds = new Rectangle<int>(20, 20, 200, 100),
                Visible = true
            }
        ]
    };
}
