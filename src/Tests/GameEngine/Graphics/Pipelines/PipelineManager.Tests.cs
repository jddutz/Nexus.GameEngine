using Xunit;
using Moq;
using Nexus.GameEngine.Graphics.Pipelines;
using Tests.GameEngine.Runtime.Systems;
using Nexus.GameEngine.Runtime.Systems;
using Nexus.GameEngine.Runtime;

namespace Tests.GameEngine.Graphics.Pipelines;

public class PipelineManagerTests
{
    [Fact]
    public void Constructor_InitializesWithDependencies()
    {
        // Arrange
        var graphics = MockSystemHelpers.CreateGraphics();
        var resources = MockSystemHelpers.CreateResources();
        var window = MockSystemHelpers.CreateWindow();

        // Act
        var manager = new PipelineManager(
            graphics.Context.Object,
            Mock.Of<IWindowService>(w => w.GetWindow() == window.Window.Object),
            graphics.SwapChain.Object,
            resources.ResourceManager.Object,
            graphics.DescriptorManager.Object);

        // Assert
        Assert.NotNull(manager);
    }
}
