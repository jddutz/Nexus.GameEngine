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
/// Tests UV coordinate control using texture atlas.
/// Validates that UVMin/UVMax can crop specific regions from a texture atlas.
/// Single frame test: Display only red quadrant from 4-color atlas
/// NOTE: UVMin/UVMax properties not yet implemented (Phase 4 deferred US2)
/// </summary>
public partial class UVCoordinateTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IDescriptorManager descriptorManager,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("UV coordinate test (atlas red quadrant)")]
    public readonly static UVCoordinateTestTemplate UVCoordinateTestInstance = new()
    {
        SampleCoordinates = [
            new(200, 150),    // Center - should be red
            new(150, 100),    // Top-left - should be red
            new(249, 199),    // Bottom-right - should be red
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),    // Red (from top-left quadrant)
                new(1, 0, 0, 1),    // Red
                new(1, 0, 0, 1),    // Red
            ]
        }
    };

    protected override void OnActivate()
    {
        base.OnActivate();

        // Display only the red quadrant (top-left) of the atlas
        // Atlas layout: [Red, Green]
        //               [Blue, Yellow]
        var element = new Element(descriptorManager)
        {
            Name = "AtlasElement",
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };
        
        element.Load(new Template { Name = "AtlasElement" });
        element.SetPosition(new Vector3D<float>(100, 100, 0));
        element.SetSize(new Vector2D<int>(150, 100));
        element.SetTexture(TestResources.TestAtlas);
        
        // TODO: When UVMin/UVMax properties are implemented:
        // element.SetUVMin(new Vector2D<float>(0, 0));
        // element.SetUVMax(new Vector2D<float>(0.5f, 0.5f)); // Top-left quadrant only
        
        element.ApplyUpdates(0);
        
        AddChild(element);
        element.Activate();
    }
}
