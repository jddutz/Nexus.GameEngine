using Moq;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.Descriptors;

namespace Tests.GameEngine.GUI;

public class BackgroundImageLayerTests
{
    [Fact]
    public void GetDrawCommands_ReturnsEmpty_WhenNotActivatedOrResourcesMissing()
    {
        var desc = new Mock<IDescriptorManager>();
        var resources = new Mock<IResourceManager>();
        var pipeline = new Mock<IPipelineManager>();

        var layer = new BackgroundImageLayer(desc.Object, resources.Object, pipeline.Object);

        // Without activation and resources the draw commands enumerable should be empty
        var cmds = layer.GetDrawCommands(default!).ToList();
        Assert.Empty(cmds);
    }
}
