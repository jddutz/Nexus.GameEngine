using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Textures;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.UITexture;

/// <summary>
/// Tests mixed Elements: some with solid colors (UniformColor + tint), some with textures.
/// Validates that uber-shader handles both cases without pipeline switches.
/// Single frame test: 5 elements total (3 solid colors, 2 textured)
/// </summary>
public partial class MixedUITest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IDescriptorManager descriptorManager,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Mixed UI test (solid colors + textures, no pipeline switches)")]
    public readonly static MixedUITestTemplate MixedUITestInstance = new()
    {
        SampleCoordinates = [
            new(150, 150),    // Red solid
            new(350, 150),    // Green solid
            new(550, 150),    // Blue solid
            new(150, 350),    // Red texture
            new(350, 350),    // White texture
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0, 0, 1),      // Red solid
                new(0, 1, 0, 1),      // Green solid
                new(0, 0, 1, 1),      // Blue solid
                new(1, 0, 0, 1),      // Red texture
                new(1, 1, 1, 1),      // White texture
            ]
        }
    };

    protected override void OnActivate()
    {
        base.OnActivate();

        // Row 1: Solid colors (UniformColor + tint)
        CreateSolidElement("Red", 100, 100, 1, 0, 0);
        CreateSolidElement("Green", 300, 100, 0, 1, 0);
        CreateSolidElement("Blue", 500, 100, 0, 0, 1);

        // Row 2: Textured elements
        CreateTexturedElement("RedTexture", 100, 300, TestResources.TestTexture);
        CreateTexturedElement("WhiteTexture", 300, 300, TestResources.WhiteTexture);
    }

    private void CreateSolidElement(string name, float x, float y, float r, float g, float b)
    {
        var element = new Element(descriptorManager)
        {
            Name = name,
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };
        
        element.Load(new Template { Name = name });
        element.SetPosition(new Vector3D<float>(x, y, 0));
        element.SetSize(new Vector2D<int>(100, 100));
        element.SetTintColor(new Vector4D<float>(r, g, b, 1));
        // Texture defaults to UniformColor (1x1 white pixel)
        element.ApplyUpdates(0);
        
        AddChild(element);
        element.Activate();
    }

    private void CreateTexturedElement(string name, float x, float y, TextureDefinition texture)
    {
        var element = new Element(descriptorManager)
        {
            Name = name,
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };
        
        element.Load(new Template { Name = name });
        element.SetPosition(new Vector3D<float>(x, y, 0));
        element.SetSize(new Vector2D<int>(100, 100));
        element.SetTexture(texture);
        element.ApplyUpdates(0);
        
        AddChild(element);
        element.Activate();
    }
}
