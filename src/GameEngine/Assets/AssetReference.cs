namespace Nexus.GameEngine.Assets;

/// <summary>
/// Reference to an asset that can be loaded on demand.
/// </summary>
/// <typeparam name="T">Type of asset being referenced</typeparam>
/// <remarks>
/// Initializes a new asset reference.
/// </remarks>
/// <param name="assetPath">Path to the asset</param>
/// <param name="assetService">Asset service for loading</param>
public class AssetReference<T>(string assetPath, IAssetService? assetService = null) where T : class
{
    private readonly string _assetPath = assetPath ?? throw new ArgumentNullException(nameof(assetPath));
    private readonly IAssetService? _assetService = assetService;
    private T? _cachedAsset;

    /// <summary>
    /// Initializes a new empty asset reference.
    /// </summary>
    public AssetReference() : this(string.Empty, null)
    {
    }

    /// <summary>
    /// Gets the path to the referenced asset.
    /// </summary>
    public string AssetPath => _assetPath;

    /// <summary>
    /// Gets the asset identifier.
    /// </summary>
    public AssetId AssetId { get; } = new AssetId(assetPath);

    /// <summary>
    /// Gets or sets the cached asset.
    /// </summary>
    public T? CachedAsset
    {
        get => _cachedAsset;
        set => _cachedAsset = value;
    }

    /// <summary>
    /// Gets whether the asset is currently loaded.
    /// </summary>
    public bool IsLoaded => _cachedAsset != null;

    /// <summary>
    /// Loads the asset if not already loaded.
    /// </summary>
    /// <returns>The loaded asset</returns>
    public async Task<T?> LoadAsync()
    {
        if (_cachedAsset != null)
            return _cachedAsset;

        if (_assetService == null)
            throw new InvalidOperationException("No asset service provided for loading");

        _cachedAsset = await _assetService.LoadAsync<T>(_assetPath);
        return _cachedAsset;
    }

    /// <summary>
    /// Loads the asset synchronously if not already loaded.
    /// </summary>
    /// <returns>The loaded asset</returns>
    public T? Load()
    {
        if (_cachedAsset != null)
            return _cachedAsset;

        if (_assetService == null)
            throw new InvalidOperationException("No asset service provided for loading");

        _cachedAsset = _assetService.Load<T>(_assetPath);
        return _cachedAsset;
    }

    /// <summary>
    /// Unloads the cached asset.
    /// </summary>
    public void Unload()
    {
        _cachedAsset = null;
        _assetService?.Unload(_assetPath);
    }

    /// <summary>
    /// Gets the cached asset without loading.
    /// </summary>
    /// <returns>The cached asset or null if not loaded</returns>
    public T? GetCached() => _cachedAsset;
}