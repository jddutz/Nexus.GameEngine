namespace Nexus.GameEngine.Resources.Sources;

/// <summary>
/// Base interface for all resource sources.
/// A source knows HOW to load data, not WHAT to load.
/// </summary>
/// <typeparam name="TData">The type of raw data this source produces</typeparam>
public interface IResourceSource<out TData>
{
    /// <summary>
    /// Loads raw data from the source.
    /// </summary>
    /// <returns>Raw data ready for GPU resource creation</returns>
    TData Load();
}
