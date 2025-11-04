using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources.Textures.Definitions;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.UITexture;

/// <summary>
/// Tests dynamic texture change during runtime.
/// Validates that OnTextureChanged handler properly reloads texture and updates descriptor set.
/// Multi-frame test: Starts with red texture, changes to white after 60 frames
/// </summary>
public partial class SetTextureTest(
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Dynamic texture change test (red → white)")]
    public readonly static SetTextureTestTemplate DynamicTextureChangeTestInstance = new()
    {
        FrameCount = 3, // Frame 0: init red, Frame 1: verify red, Frame 2: change queued/still red, Frame 3: white

        SampleCoordinates = [
            new(200, 150),    // Center of element
        ],

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [new(1, 0, 0, 1)],     // Frame 0: Red (initial)
            [2] = [new(1, 1, 1, 1)],     // Frame 3: Changed to white
        },

        Subcomponents = [
            new ElementTemplate()
            {
                Position = new Vector3D<float>(100, 100, 0),
                Size = new Vector2D<int>(200, 100),
                Texture = TestResources.TestTexture
            }
        ]
    };

    protected override void OnUpdate(double deltaTime)
    {
        // Change texture to white on frame 2
        // Change will be applied on the NEXT frame (frame 3) due to deferred updates
        if (FramesRendered > 0)
        {
            var element = GetChildren<Element>().First();
            element.SetTexture(TextureDefinitions.UniformColor);
        }

        base.OnUpdate(deltaTime);
    }
}
