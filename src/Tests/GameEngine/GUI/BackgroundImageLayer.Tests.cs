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
        var layer = new BackgroundImageLayer();

        // Without activation and resources the draw commands enumerable should be empty
        var cmds = layer.GetDrawCommands(default!).ToList();
        Assert.Empty(cmds);
    }
}
