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
/// Tests Element with white texture and colored tint.
/// Validates that TintColor properly multiplies with texture color.
/// Single frame test: White texture with red tint = red result
/// </summary>
public partial class TintColorTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IDescriptorManager descriptorManager,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Tint color test (white texture * red tint = red)")]
    public readonly static TintColorTestTemplate TintColorTestInstance = new()
    {
        SampleCoordinates = [
            new(200, 150),    // Center of tinted element
            new(150, 100),    // Top-left
            new(249, 199),    // Bottom-right
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),    // Red (white * red tint)
                new(1, 0, 0, 1),    // Red
                new(1, 0, 0, 1),    // Red
            ]
        }
    };

    protected override void OnActivate()
    {
        base.OnActivate();

        // White texture with red tint should produce red result
        var element = new Element(descriptorManager)
        {
            Name = "TintedElement",
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };
        
        element.Load(new Template { Name = "TintedElement" });
        element.SetPosition(new Vector3D<float>(100, 100, 0));
        element.SetSize(new Vector2D<int>(150, 100));
        element.SetTexture(TestResources.WhiteTexture);
        element.SetTintColor(new Vector4D<float>(1, 0, 0, 1)); // Red tint
        element.ApplyUpdates(0);
        
        AddChild(element);
        element.Activate();
    }
}
