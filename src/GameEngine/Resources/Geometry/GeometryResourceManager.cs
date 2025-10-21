using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Buffers;

namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Implements geometry resource management with caching and reference counting.
/// Extends VulkanResourceManager to leverage base class functionality.
/// </summary>
public class GeometryResourceManager : VulkanResourceManager<GeometryDefinition, GeometryResource>, IGeometryResourceManager
{
    private readonly IBufferManager _bufferManager;
    
    public GeometryResourceManager(IBufferManager bufferManager, ILoggerFactory loggerFactory, IGraphicsContext context)
        : base(loggerFactory, context)
    {
        _bufferManager = bufferManager;
    }
    
    /// <inheritdoc />
    protected override string GetResourceKey(GeometryDefinition definition)
    {
        return definition.Name;
    }
    
    /// <inheritdoc />
    protected override GeometryResource CreateResource(GeometryDefinition definition)
    {
        _logger.LogDebug("Loading geometry data from source: {Name}", definition.Name);
        
        // Load vertex data from source
        var sourceData = definition.Source.Load();
        
        // Validate source data
        if (sourceData.VertexData == null || sourceData.VertexData.Length == 0)
        {
            throw new InvalidOperationException($"Geometry source returned null or empty vertex data for '{definition.Name}'");
        }
        
        if (sourceData.VertexCount == 0)
        {
            throw new InvalidOperationException($"Geometry source returned zero vertex count for '{definition.Name}'");
        }
        
        if (sourceData.Stride == 0)
        {
            throw new InvalidOperationException($"Geometry source returned zero stride for '{definition.Name}'");
        }
        
        // Create Vulkan vertex buffer
        _logger.LogDebug("Creating vertex buffer: {Name}, VertexCount={VertexCount}, Stride={Stride}, Size={Size} bytes",
            definition.Name, sourceData.VertexCount, sourceData.Stride, sourceData.VertexData.Length);
        
        var (buffer, memory) = _bufferManager.CreateVertexBuffer(sourceData.VertexData);
        
        return new GeometryResource(
            buffer,
            memory,
            sourceData.VertexCount,
            sourceData.Stride,
            definition.Name);
    }
    
    /// <inheritdoc />
    protected override void DestroyResource(GeometryResource resource)
    {
        _logger.LogDebug("Destroying geometry resource: {Name}", resource.Name);
        _bufferManager.DestroyBuffer(resource.Buffer, resource.Memory);
    }
}
