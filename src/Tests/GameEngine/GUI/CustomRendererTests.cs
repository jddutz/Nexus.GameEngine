using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.GUI;
using Xunit;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Tests.GameEngine.GUI;

// Custom renderer implementation for testing
public partial class MyCustomRenderer : RuntimeComponent, IDrawable
{
    public bool IsVisible() => true;

    public IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        yield return new DrawCommand
        {
            RenderMask = 1,
            Pipeline = PipelineHandle.Invalid,
            VertexBuffer = default,
            VertexCount = 3
        };
    }
}

public class CustomRendererTests
{
    [Fact]
    public void CustomRenderer_CanBeCreated_AndImplementsIDrawable()
    {
        var renderer = new MyCustomRenderer();
        Assert.IsAssignableFrom<IDrawable>(renderer);
        Assert.IsAssignableFrom<RuntimeComponent>(renderer);
    }

    [Fact]
    public void CustomRenderer_GetDrawCommands_ReturnsCommands()
    {
        var renderer = new MyCustomRenderer();
        var context = new RenderContext 
        { 
            Camera = null, 
            Viewport = new Nexus.GameEngine.Graphics.Viewport 
            { 
                Extent = new Extent2D(800, 600), 
                ClearColor = new Vector4D<float>(0, 0, 0, 1) 
            }, 
            AvailableRenderPasses = 1, 
            RenderPassNames = [], 
            DeltaTime = 0,
            ViewProjectionDescriptorSet = default
        };

        var commands = renderer.GetDrawCommands(context);
        Assert.Single(commands);
    }
}
