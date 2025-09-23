using Nexus.GameEngine.Assets.Build.Models;

namespace Nexus.GameEngine.Assets.Build.Management;

/// <summary>
/// Interface for managing asset processors.
/// </summary>
public interface IAssetProcessorManager
{
    /// <summary>
    /// Registers an asset processor.
    /// </summary>
    /// <param name="processor">The processor to register</param>
    void RegisterProcessor(IAssetProcessor processor);

    /// <summary>
    /// Unregisters an asset processor.
    /// </summary>
    /// <param name="processor">The processor to unregister</param>
    void UnregisterProcessor(IAssetProcessor processor);

    /// <summary>
    /// Gets all registered processors that can handle the specified asset.
    /// </summary>
    /// <param name="inputPath">The input asset path</param>
    /// <param name="targetPlatform">The target platform</param>
    /// <returns>List of compatible processors ordered by priority</returns>
    IEnumerable<IAssetProcessor> GetProcessorsForAsset(string inputPath, string targetPlatform);

    /// <summary>
    /// Processes an asset using the first compatible processor.
    /// </summary>
    /// <param name="context">The processing context</param>
    /// <returns>The processing result</returns>
    Task<AssetProcessingResult> ProcessAssetAsync(AssetProcessingContext context);

    /// <summary>
    /// Processes multiple assets in parallel.
    /// </summary>
    /// <param name="contexts">The processing contexts</param>
    /// <param name="maxConcurrency">Maximum number of concurrent processing operations</param>
    /// <returns>Dictionary of results keyed by input path</returns>
    Task<Dictionary<string, AssetProcessingResult>> ProcessAssetsAsync(
        IEnumerable<AssetProcessingContext> contexts,
        int maxConcurrency = 0);

    /// <summary>
    /// Gets all registered processors.
    /// </summary>
    /// <returns>List of all registered processors</returns>
    IEnumerable<IAssetProcessor> GetAllProcessors();

    /// <summary>
    /// Clears all registered processors.
    /// </summary>
    void ClearProcessors();

    /// <summary>
    /// Gets statistics about registered processors.
    /// </summary>
    /// <returns>Processor statistics</returns>
    ProcessorStatistics GetStatistics();
}