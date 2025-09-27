using Nexus.GameEngine.Components;

namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// Resource memory statistics for monitoring and debugging.
/// </summary>
public record ResourceMemoryStats(
    int TotalResources,
    int PersistentResources,
    int ComponentScopedResources,
    long EstimatedMemoryUsage,
    long MaxCacheSize,
    DateTime LastPurge
);

/// <summary>
/// Detailed information about a cached resource.
/// </summary>
public record ResourceInfo(
    string Name,
    bool IsPersistent,
    int ReferenceCount,
    DateTime LastAccessed,
    long EstimatedSize
);

/// <summary>
/// Comprehensive resource manager for shared OpenGL resources with automatic discovery,
/// dependency tracking, and memory management.
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Gets or creates a resource using a resource definition directly.
    /// Automatically registers the definition if not already registered.
    /// Associates the resource with the specified component for dependency tracking.
    /// This is the preferred method for type-safe resource access.
    /// </summary>
    /// <typeparam name="T">Type of resource handle (typically uint for OpenGL)</typeparam>
    /// <param name="definition">Resource definition</param>
    /// <param name="usingComponent">Component that will use this resource (for dependency tracking)</param>
    /// <returns>Resource handle</returns>
    T GetOrCreateResource<T>(IResourceDefinition definition, IRuntimeComponent? usingComponent = null) where T : struct;

    /// <summary>
    /// Gets or creates a resource using the attribute-based registry system by name.
    /// Associates the resource with the specified component for dependency tracking.
    /// Used for resources discovered via reflection or previously registered.
    /// </summary>
    /// <typeparam name="T">Type of resource handle (typically uint for OpenGL)</typeparam>
    /// <param name="resourceName">Name of the resource</param>
    /// <param name="usingComponent">Component that will use this resource (for dependency tracking)</param>
    /// <returns>Resource handle</returns>
    T GetOrCreateResource<T>(string resourceName, IRuntimeComponent? usingComponent = null) where T : struct;

    /// <summary>
    /// Gets a cached resource by name without creating it.
    /// Returns default(T) if not found.
    /// </summary>
    /// <typeparam name="T">Type of resource handle</typeparam>
    /// <param name="name">Name of the resource</param>
    /// <returns>Resource handle or default if not found</returns>
    T GetSharedResourceID<T>(string name) where T : struct;

    /// <summary>
    /// Dynamically registers a resource definition at runtime.
    /// Useful for component-scoped assets that don't need global sharing.
    /// </summary>
    /// <param name="definition">Resource definition to register</param>
    void RegisterDefinition(IResourceDefinition definition);

    /// <summary>
    /// Checks if a resource definition exists (either from attributes or dynamic registration).
    /// </summary>
    /// <param name="resourceName">Name of the resource</param>
    /// <returns>True if the resource exists</returns>
    bool HasResource(string resourceName);

    /// <summary>
    /// Checks if a resource definition exists by comparing the definition itself.
    /// </summary>
    /// <param name="definition">Resource definition to check</param>
    /// <returns>True if the resource exists</returns>
    bool HasResource(IResourceDefinition definition);

    /// <summary>
    /// Gets a cached resource without creating it (legacy compatibility).
    /// </summary>
    /// <typeparam name="T">Type of resource</typeparam>
    /// <param name="name">Name of the resource</param>
    /// <returns>Resource or default if not found</returns>
    T GetSharedResource<T>(string name);

    /// <summary>
    /// Sets a shared resource in the cache (legacy compatibility).
    /// </summary>
    /// <typeparam name="T">Type of resource</typeparam>
    /// <param name="name">Name of the resource</param>
    /// <param name="resource">Resource to cache</param>
    void SetSharedResource<T>(string name, T resource);

    /// <summary>
    /// Removes a component's dependency on a resource.
    /// If no other components reference the resource and it's not persistent, it becomes eligible for purging.
    /// </summary>
    /// <param name="resourceName">Name of the resource</param>
    /// <param name="component">Component to remove from dependencies</param>
    void RemoveComponentDependency(string resourceName, IRuntimeComponent component);

    /// <summary>
    /// Purges all resources that have no component dependencies and are not marked as persistent.
    /// Returns number of resources purged and memory freed (estimated).
    /// </summary>
    /// <returns>Tuple of resources freed and estimated memory freed</returns>
    (int resourcesFreed, long memoryFreed) PurgeUnreferencedResources();

    /// <summary>
    /// Purges all resources associated with a specific component when it's disposed.
    /// Also removes the component from dependency lists of all resources.
    /// </summary>
    /// <param name="component">Component being disposed</param>
    /// <returns>Tuple of resources freed and estimated memory freed</returns>
    (int resourcesFreed, long memoryFreed) PurgeComponentResources(IRuntimeComponent component);

    /// <summary>
    /// Preloads specific resources by name.
    /// </summary>
    /// <param name="resourceNames">Names of resources to preload</param>
    void PreloadResources(params string[] resourceNames);

    /// <summary>
    /// Gets all resource names from the registry.
    /// </summary>
    /// <returns>Collection of resource names</returns>
    IEnumerable<string> GetResourceNames();

    /// <summary>
    /// Unregisters a dynamically registered resource (useful for cleanup).
    /// </summary>
    /// <param name="resourceName">Name of the resource to unregister</param>
    void UnregisterDefinition(string resourceName);

    /// <summary>
    /// Gets current memory usage statistics.
    /// </summary>
    /// <returns>Memory statistics</returns>
    ResourceMemoryStats GetMemoryStats();

    /// <summary>
    /// Gets detailed information about all cached resources.
    /// </summary>
    /// <returns>Collection of resource information</returns>
    IEnumerable<ResourceInfo> GetResourceInfo();

    /// <summary>
    /// Clears the resource cache (useful for testing).
    /// </summary>
    void ClearCache();
}