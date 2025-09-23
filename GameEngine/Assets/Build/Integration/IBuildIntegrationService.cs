namespace Nexus.GameEngine.Assets.Build.Integration;

/// <summary>
/// Interface for build integration services.
/// </summary>
public interface IBuildIntegrationService
{
    /// <summary>
    /// Processes assets for the specified build configuration.
    /// </summary>
    /// <param name="request">The build request</param>
    /// <returns>The build result</returns>
    Task<BuildResult> ProcessAssetsAsync(BuildRequest request);

    /// <summary>
    /// Cleans processed assets for the specified configuration.
    /// </summary>
    /// <param name="request">The clean request</param>
    /// <returns>The clean result</returns>
    Task<CleanResult> CleanAssetsAsync(CleanRequest request);
}