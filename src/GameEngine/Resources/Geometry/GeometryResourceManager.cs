namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Implements geometry resource management with caching and reference counting.
/// Extends VulkanResourceManager to leverage base class functionality.
/// </summary>
public class GeometryResourceManager(IBufferManager bufferManager)
    : VulkanResourceManager<GeometryDefinition, GeometryResource>, IGeometryResourceManager
{
    IGeometryResource? IGeometryResourceManager.GetOrCreate(GeometryDefinition definition) => GetOrCreate(definition);
    IGeometryResource? IGeometryResourceManager.CreateResource(GeometryDefinition definition) => CreateResource(definition);
    
    /// <inheritdoc />
    protected override string GetResourceKey(GeometryDefinition definition)
    {
        return definition.Name;
    }
    
    /// <inheritdoc />
    public override GeometryResource CreateResource(GeometryDefinition definition)
    {
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
        
        var (buffer, memory) = bufferManager.CreateVertexBuffer(sourceData.VertexData);
        
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
        bufferManager.DestroyBuffer(resource.Buffer, resource.Memory);
    }
}
