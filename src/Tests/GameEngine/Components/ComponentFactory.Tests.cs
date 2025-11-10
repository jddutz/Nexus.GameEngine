using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.Descriptors;

namespace Tests.GameEngine.Components;

public class ComponentFactorTests
{
    [Fact]
    public void Create_TextElement_ResolvesGraphicsManagers()
    {
        // Arrange - register mocked descriptor/resource/pipeline managers in DI
        var services = new ServiceCollection();
        var descriptorMock = new Mock<IDescriptorManager>();
        var resourceMock = new Mock<IResourceManager>();
        var pipelineMock = new Mock<IPipelineManager>();

        services.AddSingleton(descriptorMock.Object);
        services.AddSingleton(resourceMock.Object);
        services.AddSingleton(pipelineMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var factory = new ComponentFactory(serviceProvider);

        // Act
        var component = factory.Create(typeof(TextElement));

        // Assert
        Assert.NotNull(component);
        Assert.IsType<TextElement>(component);

        var text = (TextElement)component!;

        // DrawableElement exposes ResourceManager and PipelineManager properties
        Assert.Same(resourceMock.Object, text.ResourceManager);
        Assert.Same(pipelineMock.Object, text.PipelineManager);
    }
}