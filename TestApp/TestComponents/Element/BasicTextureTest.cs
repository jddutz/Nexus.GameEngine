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
/// Tests Element with a real texture (test_texture.png - solid red).
/// Validates that Element can load and display external textures.
/// Single frame test: Red textured rectangle at (100,100) size 256x256
/// </summary>
public partial class BasicTextureTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IDescriptorManager descriptorManager,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Basic texture test (Element with test_texture.png)")]
    public readonly static BasicTextureTestTemplate BasicTextureTestInstance = new()
    {
        SampleCoordinates = [
            new(228, 228),    // Center of textured rectangle
            new(100, 100),    // Top-left corner
            new(355, 355),    // Bottom-right corner (inside)
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),    // Red center
                new(1, 0, 0, 1),    // Red top-left
                new(1, 0, 0, 1),    // Red bottom-right
            ]
        }
    };

    protected override void OnActivate()
    {
        base.OnActivate();

        // Create Element with test texture
        var element = new Element(descriptorManager)
        {
            Name = "TexturedElement",
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };

        element.Load(new Template { Name = "TexturedElement" });
        element.SetPosition(new Vector3D<float>(100, 100, 0));
        element.SetSize(new Vector2D<int>(256, 256));
        element.SetTexture(TestResources.TestTexture); // Solid red texture
        element.ApplyUpdates(0);

        AddChild(element);
        element.Activate();
    }
}
