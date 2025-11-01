using Nexus.GameEngine.Assets.Build.Models;

namespace Nexus.GameEngine.Assets.Build.Management;

/// <summary>
/// Manages registration and execution of asset processors.
/// </summary>
public class AssetProcessorManager : IAssetProcessorManager
{
    private readonly List<IAssetProcessor> _processors = [];
    private readonly object _lock = new();

    /// <summary>
    /// Registers an asset processor.
    /// </summary>
    /// <param name="processor">The processor to register</param>
    public void RegisterProcessor(IAssetProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);

        lock (_lock)
        {
            if (!_processors.Contains(processor))
            {
                var extensions = string.Join(", ", processor.SupportedExtensions);
                _processors.Add(processor);
                _processors.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                Log.Debug($"Registered asset processor with priority {processor.Priority} for extensions: {extensions}");
            }
        }
    }

    /// <summary>
    /// Unregisters an asset processor.
    /// </summary>
    /// <param name="processor">The processor to unregister</param>
    public void UnregisterProcessor(IAssetProcessor processor)
    {
        if (processor == null)
            return;

        lock (_lock)
        {
            if (_processors.Remove(processor))
            {
                Log.Debug("Unregistered asset processor for extensions: {Extensions}",
                    string.Join(", ", processor.SupportedExtensions));
            }
        }
    }

    /// <summary>
    /// Gets all registered processors that can handle the specified asset.
    /// </summary>
    /// <param name="inputPath">The input asset path</param>
    /// <param name="targetPlatform">The target platform</param>
    /// <returns>List of compatible processors ordered by priority</returns>
    public IEnumerable<IAssetProcessor> GetProcessorsForAsset(string inputPath, string targetPlatform)
    {
        if (string.IsNullOrEmpty(inputPath))
            return Enumerable.Empty<IAssetProcessor>();

        lock (_lock)
        {
            return _processors
                .Where(p => p.CanProcess(inputPath, targetPlatform))
                .ToList(); // Return a copy to avoid concurrent modification
        }
    }

    /// <summary>
    /// Processes an asset using the first compatible processor.
    /// </summary>
    /// <param name="context">The processing context</param>
    /// <returns>The processing result</returns>
    public async Task<AssetProcessingResult> ProcessAssetAsync(AssetProcessingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var processors = GetProcessorsForAsset(context.InputPath, ConvertTargetPlatformToString(context.TargetPlatform));
        var processor = processors.FirstOrDefault();

        if (processor == null)
        {
            Log.Warning($"No processor found for asset: {context.InputPath} (platform: {context.TargetPlatform})");

            return new AssetProcessingResult
            {
                Success = false,
                ErrorMessage = $"No processor found for asset: {context.InputPath}"
            };
        }

        try
        {
            Log.Debug("Processing asset {Asset} with {ProcessorType}",
                context.InputPath, processor.GetType().Name);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await processor.ProcessAsync(context);
            stopwatch.Stop();

            result.ProcessingTime = stopwatch.Elapsed;

            if (result.Success)
            {
                Log.Debug($"Successfully processed asset {context.InputPath} in {result.ProcessingTime.TotalMilliseconds}ms");
            }
            else
            {
                Log.Error("Failed to process asset {Asset}: {Error}",
                    context.InputPath, result.ErrorMessage ?? "Unknown Error");
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Exception occurred while processing asset {context.InputPath}");

            return new AssetProcessingResult
            {
                Success = false,
                ErrorMessage = $"Exception occurred: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Processes multiple assets in parallel.
    /// </summary>
    /// <param name="contexts">The processing contexts</param>
    /// <param name="maxConcurrency">Maximum number of concurrent processing operations</param>
    /// <returns>Dictionary of results keyed by input path</returns>
    public async Task<Dictionary<string, AssetProcessingResult>> ProcessAssetsAsync(
        IEnumerable<AssetProcessingContext> contexts,
        int maxConcurrency = 0)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        if (maxConcurrency <= 0)
            maxConcurrency = Environment.ProcessorCount;

        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new List<Task<(string, AssetProcessingResult)>>();

        foreach (var context in contexts)
        {
            tasks.Add(ProcessAssetWithSemaphoreAsync(context, semaphore));
        }

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.Item1, r => r.Item2);
    }

    private async Task<(string, AssetProcessingResult)> ProcessAssetWithSemaphoreAsync(
        AssetProcessingContext context, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync(context.CancellationToken);
        try
        {
            var result = await ProcessAssetAsync(context);
            return (context.InputPath, result);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Gets all registered processors.
    /// </summary>
    /// <returns>List of all registered processors</returns>
    public IEnumerable<IAssetProcessor> GetAllProcessors()
    {
        lock (_lock)
        {
            return _processors.ToList(); // Return a copy
        }
    }

    /// <summary>
    /// Clears all registered processors.
    /// </summary>
    public void ClearProcessors()
    {
        lock (_lock)
        {
            var count = _processors.Count;
            _processors.Clear();
            Log.Debug($"Cleared {count} registered processors");
        }
    }

    /// <summary>
    /// Gets statistics about registered processors.
    /// </summary>
    /// <returns>Processor statistics</returns>
    public ProcessorStatistics GetStatistics()
    {
        lock (_lock)
        {
            var extensions = _processors
                .SelectMany(p => p.SupportedExtensions)
                .Distinct()
                .ToList();

            var platforms = _processors
                .SelectMany(p => p.SupportedPlatforms)
                .Distinct()
                .ToList();

            return new ProcessorStatistics
            {
                ProcessorCount = _processors.Count,
                SupportedExtensions = extensions,
                SupportedPlatforms = platforms,
                ProcessorsByPriority = _processors
                    .GroupBy(p => p.Priority)
                    .ToDictionary(g => g.Key, g => g.Select(p => p.Name).ToList())
            };
        }
    }

    /// <summary>
    /// Converts a TargetPlatformEnum to string representation.
    /// </summary>
    /// <param name="targetPlatform">Enum value</param>
    /// <returns>String representation</returns>
    private static string ConvertTargetPlatformToString(TargetPlatformEnum targetPlatform)
    {
        return targetPlatform switch
        {
            TargetPlatformEnum.Windows => "windows",
            TargetPlatformEnum.MacOS => "macos",
            TargetPlatformEnum.Linux => "linux",
            TargetPlatformEnum.Android => "android",
            TargetPlatformEnum.iOS => "ios",
            TargetPlatformEnum.Web => "web",
            _ => "universal"
        };
    }
}