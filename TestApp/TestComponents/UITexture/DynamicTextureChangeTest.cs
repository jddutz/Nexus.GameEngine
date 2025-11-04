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
/// Tests dynamic texture change during runtime.
/// Validates that OnTextureChanged handler properly reloads texture and updates descriptor set.
/// Multi-frame test: Starts with red texture, changes to white after 60 frames
/// </summary>
public partial class DynamicTextureChangeTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IDescriptorManager descriptorManager,
    IResourceManager resourceManager,
    IPipelineManager pipelineManager
    ) : RenderableTest(pixelSampler, renderer)
{
    private Element? _element;

    [Test("Dynamic texture change test (red → white)")]
    public readonly static DynamicTextureChangeTestTemplate DynamicTextureChangeTestInstance = new()
    {
        FrameCount = 4, // Frame 0: init red, Frame 1: verify red, Frame 2: change queued/still red, Frame 3: white

        SampleCoordinates = [
            new(200, 150),    // Center of element
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [new(1, 0, 0, 1)],     // Frame 0: Red (initial)
            [1] = [new(1, 0, 0, 1)],     // Frame 1: Still red (verify initial)
            [2] = [new(1, 0, 0, 1)],     // Frame 2: SetTexture called, but change applies NEXT frame
            [3] = [new(1, 1, 1, 1)],     // Frame 3: Changed to white (deferred update applied)
        }
    };

    protected override void OnActivate()
    {
        base.OnActivate();

        // Create element with initial red texture definition
        _element = new Element(descriptorManager)
        {
            Name = "DynamicElement",
            ResourceManager = resourceManager,
            PipelineManager = pipelineManager
        };
        
        _element.Load(new Template { Name = "DynamicElement" });
        _element.SetPosition(new Vector3D<float>(100, 100, 0));
        _element.SetSize(new Vector2D<int>(200, 100));
        _element.SetTexture(TestResources.TestTexture); // Red texture definition
        _element.ApplyUpdates(0);
        
        AddChild(_element);
        _element.Activate();
    }

    protected override void OnUpdate(double deltaTime)
    {
        // Change texture to white on frame 2
        // Change will be applied on the NEXT frame (frame 3) due to deferred updates
        if (Updates == 2 && _element != null)
        {
            _element.SetTexture(TestResources.WhiteTexture);
        }

        base.OnUpdate(deltaTime);
    }
}
