using Nexus.GameEngine.Assets.Build.Models;

namespace Nexus.GameEngine.Assets.Build;

/// <summary>
/// Interface for asset processors that handle specific asset types.
/// </summary>
public interface IAssetProcessor
{
    /// <summary>
    /// Gets the name of this processor.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of this processor.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the priority of this processor. Higher values indicate higher priority.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the supported input file extensions.
    /// </summary>
    IEnumerable<string> SupportedInputExtensions { get; }

    /// <summary>
    /// Gets the supported file extensions (alias for SupportedInputExtensions).
    /// </summary>
    IEnumerable<string> SupportedExtensions => SupportedInputExtensions;

    /// <summary>
    /// Gets the output file extensions this processor produces.
    /// </summary>
    IEnumerable<string> OutputExtensions { get; }

    /// <summary>
    /// Gets the supported platforms for this processor.
    /// </summary>
    IEnumerable<string> SupportedPlatforms { get; }

    /// <summary>
    /// Checks if this processor can handle the given input file for the target platform.
    /// </summary>
    /// <param name="inputPath">Path to the input file</param>
    /// <param name="targetPlatform">Target platform</param>
    /// <returns>True if this processor can handle the file</returns>
    bool CanProcess(string inputPath, string targetPlatform);

    /// <summary>
    /// Processes an asset using the provided context.
    /// </summary>
    /// <param name="context">Processing context</param>
    /// <returns>Processing result</returns>
    Task<AssetProcessingResult> ProcessAsync(AssetProcessingContext context);

    /// <summary>
    /// Gets the priority of this processor for handling a specific asset.
    /// Higher values indicate higher priority.
    /// </summary>
    /// <param name="inputPath">Path to the input file</param>
    /// <param name="targetPlatform">Target platform</param>
    /// <returns>Priority value</returns>
    int GetPriority(string inputPath, string targetPlatform);

    /// <summary>
    /// Gets the expected output files for a given input file and target platform.
    /// </summary>
    /// <param name="inputPath">Path to the input file</param>
    /// <param name="targetPlatform">Target platform</param>
    /// <returns>List of expected output file paths</returns>
    IEnumerable<string> GetExpectedOutputs(string inputPath, string targetPlatform);
}