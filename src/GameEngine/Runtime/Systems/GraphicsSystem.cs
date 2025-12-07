using Nexus.GameEngine.Graphics.Commands;
using Nexus.GameEngine.Graphics.Descriptors;
using Nexus.GameEngine.Graphics.Synchronization;
using Microsoft.Extensions.DependencyInjection;

namespace Nexus.GameEngine.Runtime.Systems;

internal sealed class GraphicsSystem : IGraphicsSystem
{
    private readonly IServiceProvider _serviceProvider;

    internal IGraphicsContext Context { get; }
    internal IPipelineManager PipelineManager { get; }
    internal IDescriptorManager DescriptorManager { get; }
    internal ISwapChain SwapChain { get; }
    internal ISyncManager SyncManager { get; }
    internal ICommandPoolManager CommandPoolManager { get; }

    public GraphicsSystem(
        IGraphicsContext context, 
        IServiceProvider serviceProvider,
        IPipelineManager pipelineManager,
        IDescriptorManager descriptorManager,
        ISwapChain swapChain,
        ISyncManager syncManager,
        ICommandPoolManager commandPoolManager)
    {
        Context = context;
        _serviceProvider = serviceProvider;
        PipelineManager = pipelineManager;
        DescriptorManager = descriptorManager;
        SwapChain = swapChain;
        SyncManager = syncManager;
        CommandPoolManager = commandPoolManager;
    }
}
