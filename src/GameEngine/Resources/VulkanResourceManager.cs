using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Resources;

/// <summary>
/// Base class for Vulkan resource managers providing common caching, reference counting, and lifecycle management.
/// Implements the Template Method pattern - subclasses override CreateResource and DestroyResource for specific resource types.
/// </summary>
/// <typeparam name="TDefinition">Resource definition type (the recipe for creating the resource)</typeparam>
/// <typeparam name="TResource">GPU resource type (the actual Vulkan handles and data)</typeparam>
public abstract class VulkanResourceManager<TDefinition, TResource> : IDisposable
    where TDefinition : notnull
{
    protected readonly ILogger _logger;
    protected readonly IGraphicsContext _context;
    protected readonly Vk _vk;
    
    private readonly Dictionary<TDefinition, (TResource Resource, int RefCount)> _cache = [];
    private readonly object _lock = new();
    
    /// <summary>
    /// Creates a new Vulkan resource manager.
    /// </summary>
    /// <param name="loggerFactory">Logger factory for creating typed loggers</param>
    /// <param name="context">Graphics context providing Vulkan device and API access</param>
    protected VulkanResourceManager(ILoggerFactory loggerFactory, IGraphicsContext context)
    {
        _logger = loggerFactory.CreateLogger(GetType());
        _context = context;
        _vk = context.VulkanApi;
    }
    
    /// <summary>
    /// Gets an existing resource from cache or creates a new one.
    /// Increments reference count.
    /// </summary>
    /// <param name="definition">Resource definition (used as cache key and creation recipe)</param>
    /// <returns>GPU resource</returns>
    public TResource GetOrCreate(TDefinition definition)
    {
        lock (_lock)
        {
            // Check cache (uses definition's equality - for records this is value-based)
            if (_cache.TryGetValue(definition, out var cached))
            {
                _cache[definition] = (cached.Resource, cached.RefCount + 1);
                _logger.LogDebug("Resource cache hit: {DefinitionKey}", GetResourceKey(definition));
                return cached.Resource;
            }
            
            _logger.LogDebug("Creating new resource: {DefinitionKey}", GetResourceKey(definition));
            
            // Create new resource (template method - implemented by subclass)
            var resource = CreateResource(definition);
            
            // Cache with ref count of 1
            _cache[definition] = (resource, 1);
            
            return resource;
        }
    }
    
    /// <summary>
    /// Releases a resource, decrementing its reference count.
    /// If reference count reaches zero, the resource is destroyed and removed from cache.
    /// </summary>
    /// <param name="definition">Resource definition to release</param>
    public void Release(TDefinition definition)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(definition, out var cached))
            {
                _logger.LogWarning("Attempted to release non-cached resource: {DefinitionKey}", GetResourceKey(definition));
                return;
            }
            
            var newRefCount = cached.RefCount - 1;
            
            if (newRefCount > 0)
            {
                _cache[definition] = (cached.Resource, newRefCount);
                _logger.LogDebug("Resource ref count decremented: {DefinitionKey}, RefCount={RefCount}", 
                    GetResourceKey(definition), newRefCount);
            }
            else
            {
                _logger.LogDebug("Destroying resource (ref count reached zero): {DefinitionKey}", GetResourceKey(definition));
                
                // Destroy resource (template method - implemented by subclass)
                DestroyResource(cached.Resource);
                
                _cache.Remove(definition);
            }
        }
    }
    
    /// <summary>
    /// Creates a new GPU resource from a definition.
    /// Template method - must be implemented by subclasses.
    /// </summary>
    /// <param name="definition">Resource definition containing all information needed to create the resource</param>
    /// <returns>Created GPU resource</returns>
    protected abstract TResource CreateResource(TDefinition definition);
    
    /// <summary>
    /// Destroys a GPU resource, freeing all Vulkan handles.
    /// Template method - must be implemented by subclasses.
    /// </summary>
    /// <param name="resource">Resource to destroy</param>
    protected abstract void DestroyResource(TResource resource);
    
    /// <summary>
    /// Gets a string key for logging purposes.
    /// Can be overridden by subclasses for better logging.
    /// </summary>
    /// <param name="definition">Resource definition</param>
    /// <returns>String representation for logging</returns>
    protected virtual string GetResourceKey(TDefinition definition)
    {
        return definition.ToString() ?? "Unknown";
    }
    
    /// <summary>
    /// Disposes all cached resources.
    /// Called when the resource manager is disposed (typically on application shutdown).
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _logger.LogInformation("Disposing {ResourceManagerType}: {CachedCount} resources", 
                GetType().Name, _cache.Count);
            
            // Destroy all cached resources
            foreach (var (definition, (resource, refCount)) in _cache)
            {
                if (refCount > 1)
                {
                    _logger.LogWarning("Resource still has {RefCount} references during dispose: {DefinitionKey}", 
                        refCount, GetResourceKey(definition));
                }
                
                DestroyResource(resource);
            }
            
            _cache.Clear();
        }
        
        GC.SuppressFinalize(this);
    }
}
