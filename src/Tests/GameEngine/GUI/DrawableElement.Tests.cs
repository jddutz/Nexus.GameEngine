using Moq;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.Descriptors;

namespace Tests.GameEngine.GUI;

public class DrawableElementTests
{
    [Fact]
    public void GetDrawCommands_YieldsCommand_WhenGeometryPresent()
    {
        var desc = new Mock<IDescriptorManager>();
        var resource = new Mock<IResourceManager>();
        var pipeline = new Mock<IPipelineManager>();

        // Create test drawable
        var d = new TestDrawable(desc.Object, resource.Object, pipeline.Object);

        // Mock geometry
        var geo = new Mock<IGeometryResource>();
    geo.SetupGet(g => g.VertexCount).Returns(4u);
    geo.SetupGet(g => g.Buffer).Returns(default(Silk.NET.Vulkan.Buffer));

        d.SetGeometry(geo.Object);

        var cmds = d.GetDrawCommands(default!).ToList();
        Assert.Single(cmds);
        Assert.Equal(4u, cmds[0].VertexCount);
    }

    private class TestDrawable : DrawableElement
    {
        public TestDrawable(IDescriptorManager d, IResourceManager r, IPipelineManager p) : base(d, r, p) { }

        public void SetGeometry(IGeometryResource g) => Geometry = g;
    }
}
