namespace Nexus.GameEngine.Data;

/// <summary>
/// Interface for content loading services
/// </summary>
public interface IContentService
{
    T Load<T>(string assetName);
    void Unload();
}