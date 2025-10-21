using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics.Buffers;

namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Implements geometry resource management with caching and reference counting.
/// </summary>
public class GeometryResourceManager : IGeometryResourceManager
{
    private readonly IBufferManager _bufferManager;
    private readonly ILogger<GeometryResourceManager> _logger;
    
    private readonly Dictionary<string, (GeometryResource Resource, int RefCount)> _cache = [];
    private readonly object _lock = new();
    
    public GeometryResourceManager(IBufferManager bufferManager, ILoggerFactory loggerFactory)
    {
        _bufferManager = bufferManager;
        _logger = loggerFactory.CreateLogger<GeometryResourceManager>();
    }
    
    /// <inheritdoc />
    public GeometryResource GetOrCreate(IGeometryDefinition definition)
    {
        lock (_lock)
        {
            // Check cache
            if (_cache.TryGetValue(definition.Name, out var cached))
            {
                _cache[definition.Name] = (cached.Resource, cached.RefCount + 1);
                return cached.Resource;
            }
            
            // Create new resource            
            var vertexData = definition.GetVertexData();
            
            // Debug: Log vertex data for TexturedQuad (TRACE level for performance)
            if (definition.Name == "TexturedQuad" && vertexData.Length >= 64)
            {
                var bytes = vertexData.ToArray();
                for (int i = 0; i < 4; i++)
                {
                    var offset = i * 16;
                    var vertexBytes = string.Join("-", bytes.Skip(offset).Take(16).Select(b => b.ToString("X2")));
                }
            }
            
            var (buffer, memory) = _bufferManager.CreateVertexBuffer(vertexData);
            
            var resource = new GeometryResource(
                buffer,
                memory,
                definition.VertexCount,
                definition.Stride,
                definition.Name);
            
            _cache[definition.Name] = (resource, 1);
            
            
            return resource;
        }
    }
    
    /// <inheritdoc />
    public void Release(IGeometryDefinition definition)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(definition.Name, out var cached))
            {
                return;
            }
            
            var newRefCount = cached.RefCount - 1;
            
            if (newRefCount > 0)
            {
                _cache[definition.Name] = (cached.Resource, newRefCount);
            }
            else
            {
                
                _bufferManager.DestroyBuffer(cached.Resource.Buffer, cached.Resource.Memory);
                _cache.Remove(definition.Name);
            }
        }
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            
            foreach (var (name, cached) in _cache)
            {
                _bufferManager.DestroyBuffer(cached.Resource.Buffer, cached.Resource.Memory);
            }
            
            _cache.Clear();
        }
    }
}
