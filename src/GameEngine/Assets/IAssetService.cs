namespace Nexus.GameEngine.Assets;

/// <summary>
/// Service for managing and loading game assets.
/// </summary>
public interface IAssetService
{
    /// <summary>
    /// Loads an asset of the specified type.
    /// </summary>
    /// <typeparam name="T">Type of asset to load</typeparam>
    /// <param name="assetPath">Path to the asset</param>
    /// <returns>The loaded asset</returns>
    Task<T?> LoadAsync<T>(string assetPath) where T : class;

    /// <summary>
    /// Loads an asset synchronously.
    /// </summary>
    /// <typeparam name="T">Type of asset to load</typeparam>
    /// <param name="assetPath">Path to the asset</param>
    /// <returns>The loaded asset</returns>
    T? Load<T>(string assetPath) where T : class;

    /// <summary>
    /// Unloads an asset from memory.
    /// </summary>
    /// <param name="assetPath">Path to the asset</param>
    void Unload(string assetPath);

    /// <summary>
    /// Checks if an asset is currently loaded.
    /// </summary>
    /// <param name="assetPath">Path to the asset</param>
    /// <returns>True if the asset is loaded</returns>
    bool IsLoaded(string assetPath);

    /// <summary>
    /// Gets all currently loaded asset paths.
    /// </summary>
    /// <returns>Collection of loaded asset paths</returns>
    IEnumerable<string> GetLoadedAssets();

    /// <summary>
    /// Preloads an asset into memory.
    /// </summary>
    /// <param name="assetPath">Path to the asset</param>
    Task PreloadAsync(string assetPath);
}