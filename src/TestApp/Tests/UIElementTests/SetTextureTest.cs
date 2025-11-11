using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources.Textures.Definitions;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.Tests.UIElementTests;

/// <summary>
/// Tests dynamic texture change during runtime.
/// Validates that OnTextureChanged handler properly reloads texture and updates descriptor set.
/// Multi-frame test: Starts with red texture, changes to white after 60 frames
/// </summary>
public partial class SetTextureTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService
    ) : RenderableTest(pixelSampler, renderer, windowService)
{
    [Test("Dynamic texture change test (red → white)")]
    public readonly static SetTextureTestTemplate DynamicTextureChangeTestInstance = new()
    {
        FrameCount = 3, // Frame 0: init red, Frame 1: verify red, Frame 2: change queued/still red, Frame 3: white

        Subcomponents = [
            new DrawableElementTemplate()
            {
                Position = ToCenteredPositionDefault(100, 100),
                Size = new Vector2D<int>(200, 100),
                Texture = TestResources.TestTexture
            }
        ]
    };

    protected override Vector2D<int>[] GetSampleCoordinates()
    {
        return [
            new(200, 150),    // Center of element
        ];
    }

    protected override Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [new(1, 0, 0, 1)],     // Frame 0: Red (initial)
            [2] = [new(1, 1, 1, 1)],     // Frame 3: Changed to white
        };
    }

    protected override void OnUpdate(double deltaTime)
    {
        // Change texture to white on frame 2
        // Change will be applied on the NEXT frame (frame 3) due to deferred updates
        if (FramesRendered > 0)
        {
            var element = GetChildren<DrawableElement>().FirstOrDefault();
            if (element != null)
            {
                element.SetTexture(TextureDefinitions.UniformColor);
            }
            else
            {
                // Safety: if the child hasn't been created for some reason, skip the texture change
                // This prevents an exception during integration runs and allows the test harness
                // to report a failing assertion instead of crashing the runner.
            }
        }

        base.OnUpdate(deltaTime);
    }
}

