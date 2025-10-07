using Nexus.GameEngine.Assets;

namespace Nexus.GameEngine.Resources;

/// <summary>
/// Manages the lifecycle of OpenGL resources
/// </summary>
public interface IResourceManager : IDisposable
{
    /// <summary>
    /// Gets or creates an OpenGL resource from a resource definition
    /// </summary>
    /// <param name="definition">The resource definition</param>
    /// <param name="assetService">Optional asset service for loading external assets</param>
    /// <returns>OpenGL resource ID</returns>
    uint GetOrCreateResource(IResourceDefinition definition, IAssetService? assetService = null);

    /// <summary>
    /// Validates a resource definition
    /// </summary>
    /// <param name="definition">The resource definition to validate</param>
    /// <returns>Validation result</returns>
    ResourceValidationResult ValidateResource(IResourceDefinition definition);

    /// <summary>
    /// Releases a resource by name
    /// </summary>
    /// <param name="resourceName">Name of the resource to release</param>
    /// <returns>True if the resource was found and released</returns>
    bool ReleaseResource(string resourceName);

    /// <summary>
    /// Gets resource statistics
    /// </summary>
    /// <returns>Resource usage statistics</returns>
    ResourceManagerStatistics GetStatistics();

    /// <summary>
    /// Purges unused resources to free memory
    /// </summary>
    /// <returns>Number of resources purged</returns>
    int PurgeUnusedResources();
}