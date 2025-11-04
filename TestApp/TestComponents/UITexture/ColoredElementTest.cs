using Nexus.GameEngine;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.UITexture;

/// <summary>
/// Tests Element with solid color (red tint, uniform color texture).
/// Validates that Element can render solid colors using UniformColor texture + TintColor.
/// Single frame test: Red rectangle at (100,100) size 200x100
/// </summary>
public partial class ColoredElementTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IDescriptorManager descriptorManager,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Colored element test (red tint with dummy texture)")]
    public readonly static ColoredElementTestTemplate ColoredElementTestInstance = new()
    {
        SampleCoordinates = [
            new(200, 150),    // Center of red rectangle
            new(100, 100),    // Top-left corner
            new(299, 199),    // Bottom-right corner (inside)
            new(50, 50),      // Outside (should be background)
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),    // Red center
                new(1, 0, 0, 1),    // Red top-left
                new(1, 0, 0, 1),    // Red bottom-right
                Colors.DarkBlue,    // Dark blue background (default test background)
            ]
        }
    };

    protected override void OnActivate()
    {
        base.OnActivate();

        // Create Element directly (no template yet - that's T080)
        var element = new Element(descriptorManager)
        {
            Name = "RedElement",
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };

        element.Load(new Template { Name = "RedElement" });
        element.SetPosition(new Vector3D<float>(100, 100, 0));
        element.SetSize(new Vector2D<int>(200, 100));
        element.SetTintColor(new Vector4D<float>(1, 0, 0, 1)); // Red
        // Texture defaults to UniformColor (1x1 white pixel)
        element.ApplyUpdates(0); // Apply property changes

        AddChild(element);
        element.Activate();
    }
}
