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
                Bounds = new Rectangle<int>(100, 100, 200, 100),  // Origin (100,100), Size (200,100) -> renders (100,100) to (300,200)
                Visible = true
            }
        ],
        SampleCoordinates = [
            // Inside rectangle - should be Red (2px inward from edges)
            new(102, 102),   // Top-left corner (100+2, 100+2)
            new(298, 102),   // Top-right corner (300-2, 100+2)
            new(102, 198),   // Bottom-left corner (100+2, 200-2)
            new(298, 198),   // Bottom-right corner (300-2, 200-2)
            new(200, 150),   // Center of rectangle
            
            // Outside rectangle - should be DarkBlue background (2px outward from edges)
            new(50, 50),     // Top-left background
            new(200, 98),    // Top-center background (2px above rectangle)
            new(350, 50),    // Top-right background
            new(302, 150),   // Right-center background (2px right of rectangle)
            new(350, 250),   // Bottom-right background
            new(200, 202),   // Bottom-center background (2px below rectangle)
            new(50, 250),    // Bottom-left background
            new(98, 150)     // Left-center background (2px left of rectangle)
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                // Red rectangle samples
                Colors.Red,  // Top-left corner
                Colors.Red,  // Top-right corner
                Colors.Red,  // Bottom-left corner
                Colors.Red,  // Bottom-right corner
                Colors.Red,  // Center
                
                // DarkBlue background samples (sRGB converted)
                new(0.000f, 0.000f, 0.546f, 1.000f),  // Top-left
                new(0.000f, 0.000f, 0.546f, 1.000f),  // Top-center
                new(0.000f, 0.000f, 0.546f, 1.000f),  // Top-right
                new(0.000f, 0.000f, 0.546f, 1.000f),  // Right-center
                new(0.000f, 0.000f, 0.546f, 1.000f),  // Bottom-right
                new(0.000f, 0.000f, 0.546f, 1.000f),  // Bottom-center
                new(0.000f, 0.000f, 0.546f, 1.000f),  // Bottom-left
                new(0.000f, 0.000f, 0.546f, 1.000f)   // Left-center
            ]
        }
    };
}
