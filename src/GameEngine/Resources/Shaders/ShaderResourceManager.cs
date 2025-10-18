using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Implements shader resource management with caching and reference counting.
/// </summary>
public class ShaderResourceManager : IShaderResourceManager
{
    private readonly ILogger<ShaderResourceManager> _logger;
    private readonly IGraphicsContext _context;
    private readonly Vk _vk;
    
    private readonly Dictionary<string, (ShaderResource Resource, int RefCount)> _cache = new();
    private readonly object _lock = new();
    
    public ShaderResourceManager(ILoggerFactory loggerFactory, IGraphicsContext context)
    {
        _logger = loggerFactory.CreateLogger<ShaderResourceManager>();
        _context = context;
        _vk = context.VulkanApi;
    }
    
    /// <inheritdoc />
    public ShaderResource GetOrCreate(IShaderDefinition definition)
    {
        lock (_lock)
        {
            // Check cache
            if (_cache.TryGetValue(definition.Name, out var cached))
            {
                _logger.LogDebug("Shader cache hit: {Name} (ref count: {RefCount} -> {NewRefCount})",
                    definition.Name, cached.RefCount, cached.RefCount + 1);
                
                _cache[definition.Name] = (cached.Resource, cached.RefCount + 1);
                return cached.Resource;
            }
            
            // Load shader modules
            _logger.LogDebug("Loading shader: {Name}", definition.Name);
            
            var vertShaderModule = CreateShaderModule(definition.VertexShaderPath);
            var fragShaderModule = CreateShaderModule(definition.FragmentShaderPath);
            
            if (vertShaderModule.Handle == 0 || fragShaderModule.Handle == 0)
            {
                throw new InvalidOperationException(
                    $"Failed to load shader modules for '{definition.Name}'");
            }
            
            var resource = new ShaderResource(vertShaderModule, fragShaderModule, definition);
            
            _cache[definition.Name] = (resource, 1);
            
            _logger.LogInformation("Shader loaded: {Name}", definition.Name);
            
            return resource;
        }
    }
    
    private unsafe ShaderModule CreateShaderModule(string shaderPath)
    {
        try
        {
            // Convert path to embedded resource name
            // "Shaders/vert.spv" -> "Nexus.GameEngine.Shaders.vert.spv"
            var resourceName = $"Nexus.GameEngine.{shaderPath.Replace('/', '.')}";
            
            var assembly = typeof(ShaderResourceManager).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            
            if (stream == null)
            {
                _logger.LogError("Shader resource not found: {ResourceName} (from path: {ShaderPath})", 
                    resourceName, shaderPath);
                return default;
            }

            // Read SPIR-V bytes
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var code = memoryStream.ToArray();

            _logger.LogTrace("Loaded {ByteCount} bytes from shader: {ShaderPath}", code.Length, shaderPath);

            // Create shader module
            fixed (byte* codePtr = code)
            {
                var createInfo = new ShaderModuleCreateInfo
                {
                    SType = StructureType.ShaderModuleCreateInfo,
                    CodeSize = (nuint)code.Length,
                    PCode = (uint*)codePtr
                };

                ShaderModule shaderModule;
                var result = _vk.CreateShaderModule(_context.Device, &createInfo, null, &shaderModule);
                
                if (result != Result.Success)
                {
                    _logger.LogError("Failed to create shader module: {Result}", result);
                    return default;
                }

                _logger.LogTrace("Created shader module from: {ShaderPath}", shaderPath);
                return shaderModule;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception loading shader: {ShaderPath}", shaderPath);
            return default;
        }
    }
    
    /// <inheritdoc />
    public void Release(IShaderDefinition definition)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(definition.Name, out var cached))
            {
                _logger.LogWarning("Attempted to release non-existent shader: {Name}", definition.Name);
                return;
            }
            
            var newRefCount = cached.RefCount - 1;
            
            if (newRefCount > 0)
            {
                _logger.LogDebug("Shader released: {Name} (ref count: {OldCount} -> {NewCount})",
                    definition.Name, cached.RefCount, newRefCount);
                
                _cache[definition.Name] = (cached.Resource, newRefCount);
            }
            else
            {
                _logger.LogInformation("Destroying shader resource: {Name} (ref count reached 0)", definition.Name);
                
                // Destroy shader modules
                DestroyShaderResource(cached.Resource);
                _cache.Remove(definition.Name);
            }
        }
    }
    
    private unsafe void DestroyShaderResource(ShaderResource resource)
    {
        if (resource.VertexShader.Handle != 0)
        {
            _vk.DestroyShaderModule(_context.Device, resource.VertexShader, null);
        }
        if (resource.FragmentShader.Handle != 0)
        {
            _vk.DestroyShaderModule(_context.Device, resource.FragmentShader, null);
        }
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            _logger.LogInformation("Disposing ShaderResourceManager: {Count} resources in cache", _cache.Count);
            
            // Destroy all shader modules
            foreach (var (resource, _) in _cache.Values)
            {
                DestroyShaderResource(resource);
            }
            
            _cache.Clear();
        }
    }
}
