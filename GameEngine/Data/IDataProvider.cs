namespace Nexus.GameEngine.Data;

/// <summary>
/// Provides access to game data from storage (files, databases, etc.)
/// </summary>
public interface IDataProvider
{
    Task<Dictionary<string, object>> LoadJobsAsync();
    Task<Dictionary<string, object>> LoadFactionsAsync();
    Task<List<object>> LoadTileSetsAsync();
    Task<object> LoadGameDataAsync();
    Task SaveGameDataAsync(object data);
    Task<object> LoadSavedGameAsync(string gameId);
}