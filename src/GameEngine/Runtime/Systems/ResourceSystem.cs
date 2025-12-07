namespace Nexus.GameEngine.Runtime.Systems;

internal sealed class ResourceSystem : IResourceSystem
{
    internal IResourceManager ResourceManager { get; }
    internal IBufferManager BufferManager { get; }

    public ResourceSystem(IResourceManager resourceManager, IBufferManager bufferManager)
    {
        ResourceManager = resourceManager;
        BufferManager = bufferManager;
    }
}
