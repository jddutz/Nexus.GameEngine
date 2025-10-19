using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Manages Vulkan buffer creation and destruction.
/// Encapsulates low-level Vulkan buffer operations for reuse across resource managers.
/// </summary>
public interface IBufferManager
{
    /// <summary>
    /// Creates a vertex buffer and allocates device memory for it.
    /// </summary>
    /// <param name="data">Vertex data to upload to the buffer</param>
    /// <returns>Tuple of (Buffer handle, DeviceMemory handle)</returns>
    (Buffer, DeviceMemory) CreateVertexBuffer(ReadOnlySpan<byte> data);
    
    /// <summary>
    /// Creates a uniform buffer and allocates HOST_VISIBLE|HOST_COHERENT device memory for it.
    /// Uniform buffers are used for passing data to shaders via descriptor sets.
    /// </summary>
    /// <param name="size">Size of the uniform buffer in bytes</param>
    /// <returns>Tuple of (Buffer handle, DeviceMemory handle)</returns>
    /// <remarks>
    /// Uniform buffers are created with HOST_VISIBLE and HOST_COHERENT memory properties,
    /// allowing them to be mapped and updated from CPU without explicit synchronization.
    /// Use UpdateUniformBuffer to update the buffer contents.
    /// </remarks>
    (Buffer, DeviceMemory) CreateUniformBuffer(ulong size);
    
    /// <summary>
    /// Updates the contents of a uniform buffer.
    /// </summary>
    /// <param name="memory">Device memory handle of the uniform buffer</param>
    /// <param name="data">Data to write to the buffer</param>
    /// <remarks>
    /// The buffer must have been created with HOST_VISIBLE memory.
    /// This method maps the memory, copies the data, and unmaps.
    /// </remarks>
    void UpdateUniformBuffer(DeviceMemory memory, ReadOnlySpan<byte> data);
    
    /// <summary>
    /// Destroys a buffer and frees its associated device memory.
    /// </summary>
    /// <param name="buffer">Buffer to destroy</param>
    /// <param name="memory">Memory to free</param>
    void DestroyBuffer(Buffer buffer, DeviceMemory memory);
}
