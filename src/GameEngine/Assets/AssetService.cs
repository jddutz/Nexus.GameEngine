namespace Nexus.GameEngine.Assets;

/// <summary>
/// Temporary stub implementation of IAssetService for dependency injection.
/// This will be replaced with a proper implementation later.
/// </summary>
public class AssetService : IAssetService
{
    public T? Load<T>(string assetPath) where T : class
    {
        // For now, return null - components should handle gracefully
        return null;
    }

    public async Task<T?> LoadAsync<T>(string assetPath) where T : class
    {
        // For now, return null - components should handle gracefully
        return await Task.FromResult<T?>(null);
    }

    public void Unload(string assetPath)
    {
        // No-op for stub implementation
    }

    public bool IsLoaded(string assetPath)
    {
        // Always return false for stub implementation
        return false;
    }

    public IEnumerable<string> GetLoadedAssets()
    {
        // Return empty collection for stub implementation
        return [];
    }

    public async Task PreloadAsync(string assetPath)
    {
        // No-op for stub implementation
        await Task.CompletedTask;
    }
}