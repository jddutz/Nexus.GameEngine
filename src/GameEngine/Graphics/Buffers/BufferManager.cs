using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Implements Vulkan buffer creation and destruction.
/// Extracted from component-level buffer management for centralized reuse.
/// </summary>
public unsafe class BufferManager(IGraphicsContext context) : IBufferManager
{
    /// <inheritdoc />
    public (Buffer, DeviceMemory) CreateVertexBuffer(ReadOnlySpan<byte> data)
    {
        var bufferSize = (ulong)data.Length;
        
        
        // Create buffer
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = bufferSize,
            Usage = BufferUsageFlags.VertexBufferBit,
            SharingMode = SharingMode.Exclusive
        };
        
        Buffer buffer;
        if (context.VulkanApi.CreateBuffer(context.Device, &bufferInfo, null, &buffer) != Result.Success)
        {
            throw new Exception("Failed to create vertex buffer");
        }
        
        // Allocate memory
        context.VulkanApi.GetBufferMemoryRequirements(context.Device, buffer, out var memRequirements);
        
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(
                memRequirements.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
        };
        
        DeviceMemory memory;
        if (context.VulkanApi.AllocateMemory(context.Device, &allocInfo, null, &memory) != Result.Success)
        {
            context.VulkanApi.DestroyBuffer(context.Device, buffer, null);
            throw new Exception("Failed to allocate vertex buffer memory");
        }
        
        // Bind and upload
        context.VulkanApi.BindBufferMemory(context.Device, buffer, memory, 0);
        
        void* mappedData;
        context.VulkanApi.MapMemory(context.Device, memory, 0, bufferSize, 0, &mappedData);
        
        fixed (byte* dataPtr = data)
        {
            System.Buffer.MemoryCopy(dataPtr, mappedData, (long)bufferSize, (long)bufferSize);
        }
        
        context.VulkanApi.UnmapMemory(context.Device, memory);
        
        
        return (buffer, memory);
    }
    
    /// <inheritdoc />
    public (Buffer, DeviceMemory) CreateUniformBuffer(ulong size)
    {
        
        // Create buffer with uniform buffer usage
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = BufferUsageFlags.UniformBufferBit,
            SharingMode = SharingMode.Exclusive
        };
        
        Buffer buffer;
        if (context.VulkanApi.CreateBuffer(context.Device, &bufferInfo, null, &buffer) != Result.Success)
        {
            throw new Exception("Failed to create uniform buffer");
        }
        
        // Allocate HOST_VISIBLE and HOST_COHERENT memory for easy CPU updates
        context.VulkanApi.GetBufferMemoryRequirements(context.Device, buffer, out var memRequirements);
        
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(
                memRequirements.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
        };
        
        DeviceMemory memory;
        if (context.VulkanApi.AllocateMemory(context.Device, &allocInfo, null, &memory) != Result.Success)
        {
            context.VulkanApi.DestroyBuffer(context.Device, buffer, null);
            throw new Exception("Failed to allocate uniform buffer memory");
        }
        
        // Bind buffer to memory
        context.VulkanApi.BindBufferMemory(context.Device, buffer, memory, 0);
        
        
        return (buffer, memory);
    }
    
    /// <inheritdoc />
    public void UpdateUniformBuffer(DeviceMemory memory, ReadOnlySpan<byte> data)
    {
        var size = (ulong)data.Length;
        
        
        // Map memory
        void* mappedData;
        context.VulkanApi.MapMemory(context.Device, memory, 0, size, 0, &mappedData);
        
        // Copy data
        fixed (byte* dataPtr = data)
        {
            System.Buffer.MemoryCopy(dataPtr, mappedData, (long)size, (long)size);
        }
        
        // Unmap (HOST_COHERENT flag means no need for explicit flush)
        context.VulkanApi.UnmapMemory(context.Device, memory);
    }
    
    /// <inheritdoc />
    public void DestroyBuffer(Buffer buffer, DeviceMemory memory)
    {
        
        // Wait for GPU to finish using the buffer
        context.VulkanApi.DeviceWaitIdle(context.Device);
        
        context.VulkanApi.DestroyBuffer(context.Device, buffer, null);
        context.VulkanApi.FreeMemory(context.Device, memory, null);
    }
    
    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        context.VulkanApi.GetPhysicalDeviceMemoryProperties(context.PhysicalDevice, out var memProperties);
        
        for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << (int)i)) != 0 &&
                (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }
        
        throw new Exception("Failed to find suitable memory type");
    }
}
