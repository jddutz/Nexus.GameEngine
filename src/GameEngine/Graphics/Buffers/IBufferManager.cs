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
    /// Destroys a buffer and frees its associated device memory.
    /// </summary>
    /// <param name="buffer">Buffer to destroy</param>
    /// <param name="memory">Memory to free</param>
    void DestroyBuffer(Buffer buffer, DeviceMemory memory);
}
