using System;
using Moq;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Graphics.Synchronization;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime.Systems;
using Silk.NET.Windowing;
using Nexus.GameEngine.Actions;
using Nexus.GameEngine.Graphics.Buffers;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;
using Silk.NET.Input;

namespace Tests.GameEngine.Runtime.Systems;

/// <summary>
/// Helper class for creating mocked system implementations for unit testing.
/// </summary>
public static class MockSystemHelpers
{
    public class MockGraphicsSystem
    {
        public IGraphicsSystem System { get; }
        public Mock<IGraphicsContext> Context { get; } = new();
        public Mock<IPipelineManager> PipelineManager { get; } = new();
        public Mock<IDescriptorManager> DescriptorManager { get; } = new();
        public Mock<ISwapChain> SwapChain { get; } = new();
        public Mock<ISyncManager> SyncManager { get; } = new();
        public Mock<ICommandPoolManager> CommandPoolManager { get; } = new();

        public MockGraphicsSystem()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IPipelineManager))).Returns(PipelineManager.Object);

            System = new GraphicsSystem(
                Context.Object,
                serviceProvider.Object,
                PipelineManager.Object,
                DescriptorManager.Object,
                SwapChain.Object,
                SyncManager.Object,
                CommandPoolManager.Object
            );
        }
    }

    public class MockResourceSystem
    {
        public IResourceSystem System { get; }
        public Mock<IResourceManager> ResourceManager { get; } = new();
        public Mock<IBufferManager> BufferManager { get; } = new();

        public MockResourceSystem()
        {
            System = new ResourceSystem(ResourceManager.Object, BufferManager.Object);
        }
    }

    public class MockContentSystem
    {
        public IContentSystem System { get; }
        public Mock<IContentManager> ContentManager { get; } = new();

        public MockContentSystem()
        {
            System = new ContentSystem(ContentManager.Object);
        }
    }

    public class MockWindowSystem
    {
        public IWindowSystem System { get; }
        public Mock<IWindowService> WindowService { get; } = new();
        public Mock<IWindow> Window { get; } = new();

        public MockWindowSystem()
        {
            WindowService.Setup(x => x.GetWindow()).Returns(Window.Object);
            System = new WindowSystem(WindowService.Object);
        }
    }

    public class MockInputSystem
    {
        public IInputSystem System { get; }
        public Mock<IWindowService> WindowService { get; } = new();
        public Mock<IActionFactory> ActionFactory { get; } = new();
        public Mock<IInputContext> InputContext { get; } = new();

        public MockInputSystem()
        {
            WindowService.Setup(x => x.InputContext).Returns(InputContext.Object);
            System = new InputSystem(WindowService.Object, ActionFactory.Object);
        }
    }

    public static MockGraphicsSystem CreateGraphics() => new();
    public static MockResourceSystem CreateResources() => new();
    public static MockContentSystem CreateContent() => new();
    public static MockWindowSystem CreateWindow() => new();
    public static MockInputSystem CreateInput() => new();
}
