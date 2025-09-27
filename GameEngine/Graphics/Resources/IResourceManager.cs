namespace Nexus.GameEngine.Graphics.Resources;

public interface IResourceManager
{
    public uint? GetOrCreateResource<TResource>(string name, TResource resource)
    {
        return null;
    }
}