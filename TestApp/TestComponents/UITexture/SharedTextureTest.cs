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
/// Tests multiple Elements sharing the same texture.
/// Validates that texture resources are properly shared and not duplicated.
/// Single frame test: 10 elements with same texture
/// </summary>
public partial class SharedTextureTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IDescriptorManager descriptorManager,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Shared texture test (10 Elements with same texture)")]
    public readonly static SharedTextureTestTemplate SharedTextureTestInstance = new()
    {
        SampleCoordinates = [
            new(150, 100),    // Element 1
            new(300, 100),    // Element 2
            new(450, 100),    // Element 3
            new(150, 250),    // Element 6
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),    // Red
                new(1, 0, 0, 1),    // Red
                new(1, 0, 0, 1),    // Red
                new(1, 0, 0, 1),    // Red
            ]
        }
    };

    protected override void OnActivate()
    {
        base.OnActivate();

        // Create 10 elements in a grid, all sharing TestTexture
        for (int i = 0; i < 10; i++)
        {
            int row = i / 5;
            int col = i % 5;
            
            var element = new Element(descriptorManager)
            {
                Name = $"Element{i + 1}",
                ResourceManager = resourceManager,
                PipelineManager = pipelineManager
            };
            
            element.Load(new Template { Name = $"Element{i + 1}" });
            element.SetPosition(new Vector3D<float>(100 + col * 150, 50 + row * 150, 0));
            element.SetSize(new Vector2D<int>(100, 100));
            element.SetTexture(TestResources.TestTexture); // All share same texture
            element.ApplyUpdates(0);
            
            AddChild(element);
            element.Activate();
        }
    }
}
