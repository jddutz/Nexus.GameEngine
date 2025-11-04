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
/// Tests multiple Elements with different textures.
/// Validates texture resource management and descriptor set creation for multiple textures.
/// Single frame test: 3 rectangles with different textures
/// </summary>
public partial class MultiTextureTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IDescriptorManager descriptorManager,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Multi-texture test (3 Elements with different textures)")]
    public readonly static MultiTextureTestTemplate MultiTextureTestInstance = new()
    {
        SampleCoordinates = [
            new(150, 150),    // Center of red element
            new(450, 150),    // Center of white element
            new(750, 150),    // Center of icon element
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),      // Red
                new(1, 1, 1, 1),      // White
                new(1, 1, 1, 1),      // White (icon center)
            ]
        }
    };

    protected override void OnActivate()
    {
        base.OnActivate();

        // Element 1: Red texture
        var element1 = new Element(descriptorManager)
        {
            Name = "RedElement",
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };
        element1.Load(new Template { Name = "RedElement" });
        element1.SetPosition(new Vector3D<float>(100, 100, 0));
        element1.SetSize(new Vector2D<int>(100, 100));
        element1.SetTexture(TestResources.TestTexture);
        element1.ApplyUpdates(0);
        AddChild(element1);
        element1.Activate();

        // Element 2: White texture
        var element2 = new Element(descriptorManager)
        {
            Name = "WhiteElement",
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };
        element2.Load(new Template { Name = "WhiteElement" });
        element2.SetPosition(new Vector3D<float>(400, 100, 0));
        element2.SetSize(new Vector2D<int>(100, 100));
        element2.SetTexture(TestResources.WhiteTexture);
        element2.ApplyUpdates(0);
        AddChild(element2);
        element2.Activate();

        // Element 3: Icon texture
        var element3 = new Element(descriptorManager)
        {
            Name = "IconElement",
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };
        element3.Load(new Template { Name = "IconElement" });
        element3.SetPosition(new Vector3D<float>(700, 100, 0));
        element3.SetSize(new Vector2D<int>(100, 100));
        element3.SetTexture(TestResources.TestIcon);
        element3.ApplyUpdates(0);
        AddChild(element3);
        element3.Activate();
    }
}
