using Nexus.GameEngine.Assets.Build.Models;
using Nexus.GameEngine.Assets.Build.Management;

namespace Nexus.GameEngine.Assets.Build.Integration;

/// <summary>
/// Service for integrating asset processing into the build pipeline.
/// </summary>
public class BuildIntegrationService(
    IAssetProcessorManager processorManager,
    ILogger<BuildIntegrationService> logger) : IBuildIntegrationService
{
    private readonly IAssetProcessorManager _processorManager = processorManager ?? throw new ArgumentNullException(nameof(processorManager));
    private readonly ILogger<BuildIntegrationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Processes assets for the specified build configuration.
    /// </summary>
    /// <param name="request">The build request</param>
    /// <returns>The build result</returns>
    public async Task<BuildResult> ProcessAssetsAsync(BuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new BuildResult
        {
            StartTime = DateTime.UtcNow,
            Request = request
        };

        try
        {
            Log.Info("Starting asset processing for platform: {Platform}, configuration: {Configuration}",
                request.TargetPlatform, request.Configuration);

            // Discover assets to process
            var assetFiles = DiscoverAssets(request);
            Log.Info("Found {assetFiles.Count} assets to process");

            if (assetFiles.Count == 0)
            {
                result.Success = true;
                result.EndTime = DateTime.UtcNow;
                Log.Info("No assets to process");
                return result;
            }

            // Create processing contexts
            var contexts = CreateProcessingContexts(assetFiles, request);

            // Check which assets need processing
            var contextsToProcess = FilterAssetsNeedingProcessing(contexts, request);
            var skipped = contexts.Count - contextsToProcess.Count;
            Log.Info("Processing {contextsToProcess.Count} assets (skipping {skipped} up-to-date assets)");

            if (contextsToProcess.Count == 0)
            {
                result.Success = true;
                result.EndTime = DateTime.UtcNow;
                Log.Info("All assets are up-to-date");
                return result;
            }

            // Process assets
            var processingResults = await _processorManager.ProcessAssetsAsync(
                contextsToProcess, request.MaxConcurrency);

            // Analyze results
            result.ProcessedAssets = processingResults.Count;
            result.SuccessfulAssets = processingResults.Values.Count(r => r.Success);
            result.FailedAssets = processingResults.Values.Count(r => !r.Success);

            foreach (var kvp in processingResults)
            {
                var processingResult = kvp.Value;
                if (processingResult.Success)
                {
                    result.OutputFiles.AddRange(processingResult.OutputFiles);
                    result.Warnings.AddRange(processingResult.Warnings);
                }
                else
                {
                    result.Errors.Add($"Failed to process {kvp.Key}: {processingResult.ErrorMessage}");
                }
            }

            result.Success = result.FailedAssets == 0;
            result.EndTime = DateTime.UtcNow;

            Log.Info($"Asset processing completed. Success: {result.SuccessfulAssets}/{result.ProcessedAssets}, Errors: {result.FailedAssets}");

            // Generate build manifest
            if (result.Success && request.GenerateManifest)
            {
                await GenerateBuildManifestAsync(result, request);
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Asset processing failed");
            result.Success = false;
            result.Errors.Add($"Build failed: {ex.Message}");
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Cleans processed assets for the specified configuration.
    /// </summary>
    /// <param name="request">The clean request</param>
    /// <returns>The clean result</returns>
    public async Task<CleanResult> CleanAssetsAsync(CleanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new CleanResult
        {
            StartTime = DateTime.UtcNow,
            Request = request
        };

        try
        {
            Log.Info("Cleaning assets for platform: {Platform}, configuration: {Configuration}",
                request.TargetPlatform, request.Configuration);

            var outputDir = GetOutputDirectory(request.OutputDirectory, request.TargetPlatform, request.Configuration);

            if (Directory.Exists(outputDir))
            {
                var files = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories);
                result.DeletedFiles = files.Length;

                // Delete files asynchronously to avoid blocking
                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        File.Delete(file);
                        Log.Debug("Deleted: {File}", file);
                    }
                });

                // Remove empty directories
                RemoveEmptyDirectories(outputDir);

                Log.Info($"Cleaned {result.DeletedFiles} files from {outputDir}");
            }
            else
            {
                Log.Info("Output directory does not exist: {Directory}", outputDir);
            }

            result.Success = true;
            result.EndTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Asset cleaning failed");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    private List<string> DiscoverAssets(BuildRequest request)
    {
        var assets = new List<string>();

        foreach (var inputDir in request.InputDirectories)
        {
            if (!Directory.Exists(inputDir))
            {
                Log.Warning("Input directory does not exist: {Directory}", inputDir);
                continue;
            }

            // Use include patterns or default to all files
            var patterns = request.IncludePatterns.Any() ? request.IncludePatterns.ToArray() : ["*.*"];

            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(inputDir, pattern, SearchOption.AllDirectories);
                assets.AddRange(files);
            }
        }

        // Apply exclude patterns
        if (request.ExcludePatterns.Any())
        {
            assets = assets.Where(asset =>
                !request.ExcludePatterns.Any(pattern =>
                    Path.GetFileName(asset).Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return assets.Distinct().ToList();
    }

    private List<AssetProcessingContext> CreateProcessingContexts(List<string> assetFiles, BuildRequest request)
    {
        var contexts = new List<AssetProcessingContext>();

        foreach (var assetFile in assetFiles)
        {
            var processors = _processorManager.GetProcessorsForAsset(assetFile, request.TargetPlatform);
            if (!processors.Any())
            {
                Log.Debug("No processor found for asset: {Asset}", assetFile);
                continue;
            }

            var outputDir = GetOutputDirectory(request.OutputDirectory, request.TargetPlatform, request.Configuration);
            var relativePath = Path.GetRelativePath(request.InputDirectories.First(), assetFile);
            var assetOutputDir = Path.Combine(outputDir, Path.GetDirectoryName(relativePath) ?? "");
            var assetOutputFile = Path.Combine(assetOutputDir, Path.GetFileName(assetFile));

            var context = new AssetProcessingContext
            {
                InputPath = assetFile,
                OutputPath = assetOutputFile,
                OutputDirectory = assetOutputDir,
                TargetPlatform = ConvertStringToTargetPlatform(request.TargetPlatform),
                Configuration = request.Configuration,
                Options = new Dictionary<string, object>(request.ProcessingOptions),
                Logger = logger,
                ForceProcessing = request.ForceRebuild
            };

            contexts.Add(context);
        }

        return contexts;
    }

    private List<AssetProcessingContext> FilterAssetsNeedingProcessing(
        List<AssetProcessingContext> contexts, BuildRequest request)
    {
        if (request.ForceRebuild)
            return contexts;

        var filtered = new List<AssetProcessingContext>();

        foreach (var context in contexts)
        {
            var needsProcessing = AssetNeedsProcessing(context);
            if (needsProcessing)
            {
                filtered.Add(context);
            }
        }

        return filtered;
    }

    private bool AssetNeedsProcessing(AssetProcessingContext context)
    {
        try
        {
            var inputInfo = new FileInfo(context.InputPath);
            if (!inputInfo.Exists)
                return false;

            var processors = _processorManager.GetProcessorsForAsset(context.InputPath, ConvertTargetPlatformToString(context.TargetPlatform));
            var processor = processors.FirstOrDefault();
            if (processor == null)
                return false;

            var expectedOutputs = processor.GetExpectedOutputs(context.InputPath, ConvertTargetPlatformToString(context.TargetPlatform));

            foreach (var expectedOutput in expectedOutputs)
            {
                var outputPath = Path.Combine(context.OutputDirectory, expectedOutput);
                var outputInfo = new FileInfo(outputPath);

                if (!outputInfo.Exists || outputInfo.LastWriteTime < inputInfo.LastWriteTime)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Error checking if asset needs processing: {Asset}", context.InputPath);
            return true; // Process on error to be safe
        }
    }

    private string GetOutputDirectory(string baseOutputDir, string platform, string configuration)
    {
        return Path.Combine(baseOutputDir, platform, configuration);
    }

    private void RemoveEmptyDirectories(string directory)
    {
        try
        {
            var subdirectories = Directory.GetDirectories(directory);
            foreach (var subdirectory in subdirectories)
            {
                RemoveEmptyDirectories(subdirectory);
            }

            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
                Log.Debug("Removed empty directory: {Directory}", directory);
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Failed to remove empty directory: {Directory}", directory);
        }
    }

    private async Task GenerateBuildManifestAsync(BuildResult result, BuildRequest request)
    {
        try
        {
            var manifestPath = Path.Combine(
                GetOutputDirectory(request.OutputDirectory, request.TargetPlatform, request.Configuration),
                "build-manifest.json");

            var manifest = new
            {
                Platform = request.TargetPlatform,
                Configuration = request.Configuration,
                BuildTime = result.StartTime,
                ProcessedAssets = result.ProcessedAssets,
                OutputFiles = result.OutputFiles,
                Success = result.Success,
                Duration = result.EndTime - result.StartTime
            };

            var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(manifestPath, json);
            result.ManifestFile = manifestPath;

            Log.Info("Generated build manifest: {Manifest}", manifestPath);
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Failed to generate build manifest");
        }
    }

    /// <summary>
    /// Converts a string target platform to the enum equivalent.
    /// </summary>
    /// <param name="targetPlatform">String representation of target platform</param>
    /// <returns>TargetPlatformEnum value</returns>
    private static TargetPlatformEnum ConvertStringToTargetPlatform(string targetPlatform)
    {
        return targetPlatform?.ToLowerInvariant() switch
        {
            "windows" => TargetPlatformEnum.Windows,
            "macos" => TargetPlatformEnum.MacOS,
            "linux" => TargetPlatformEnum.Linux,
            "android" => TargetPlatformEnum.Android,
            "ios" => TargetPlatformEnum.iOS,
            "web" => TargetPlatformEnum.Web,
            _ => TargetPlatformEnum.Universal
        };
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