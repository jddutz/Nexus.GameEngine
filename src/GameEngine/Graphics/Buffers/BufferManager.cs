using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nexus.GameEngine.Graphics.Buffers;

/// <summary>
/// Implements Vulkan buffer creation and destruction.
/// Extracted from component-level buffer management for centralized reuse.
/// </summary>
public unsafe class BufferManager : IBufferManager
{
    private readonly IGraphicsContext _context;
    private readonly ILogger<BufferManager> _logger;
    
    public BufferManager(IGraphicsContext context, ILoggerFactory loggerFactory)
    {
        _context = context;
        _logger = loggerFactory.CreateLogger<BufferManager>();
    }
    
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
        if (_context.VulkanApi.CreateBuffer(_context.Device, &bufferInfo, null, &buffer) != Result.Success)
        {
            throw new Exception("Failed to create vertex buffer");
        }
        
        // Allocate memory
        _context.VulkanApi.GetBufferMemoryRequirements(_context.Device, buffer, out var memRequirements);
        
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(
                memRequirements.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
        };
        
        DeviceMemory memory;
        if (_context.VulkanApi.AllocateMemory(_context.Device, &allocInfo, null, &memory) != Result.Success)
        {
            _context.VulkanApi.DestroyBuffer(_context.Device, buffer, null);
            throw new Exception("Failed to allocate vertex buffer memory");
        }
        
        // Bind and upload
        _context.VulkanApi.BindBufferMemory(_context.Device, buffer, memory, 0);
        
        void* mappedData;
        _context.VulkanApi.MapMemory(_context.Device, memory, 0, bufferSize, 0, &mappedData);
        
        fixed (byte* dataPtr = data)
        {
            System.Buffer.MemoryCopy(dataPtr, mappedData, (long)bufferSize, (long)bufferSize);
        }
        
        _context.VulkanApi.UnmapMemory(_context.Device, memory);
        
        
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
        if (_context.VulkanApi.CreateBuffer(_context.Device, &bufferInfo, null, &buffer) != Result.Success)
        {
            throw new Exception("Failed to create uniform buffer");
        }
        
        // Allocate HOST_VISIBLE and HOST_COHERENT memory for easy CPU updates
        _context.VulkanApi.GetBufferMemoryRequirements(_context.Device, buffer, out var memRequirements);
        
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(
                memRequirements.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
        };
        
        DeviceMemory memory;
        if (_context.VulkanApi.AllocateMemory(_context.Device, &allocInfo, null, &memory) != Result.Success)
        {
            _context.VulkanApi.DestroyBuffer(_context.Device, buffer, null);
            throw new Exception("Failed to allocate uniform buffer memory");
        }
        
        // Bind buffer to memory
        _context.VulkanApi.BindBufferMemory(_context.Device, buffer, memory, 0);
        
        
        return (buffer, memory);
    }
    
    /// <inheritdoc />
    public void UpdateUniformBuffer(DeviceMemory memory, ReadOnlySpan<byte> data)
    {
        var size = (ulong)data.Length;
        
        
        // Map memory
        void* mappedData;
        _context.VulkanApi.MapMemory(_context.Device, memory, 0, size, 0, &mappedData);
        
        // Copy data
        fixed (byte* dataPtr = data)
        {
            System.Buffer.MemoryCopy(dataPtr, mappedData, (long)size, (long)size);
        }
        
        // Unmap (HOST_COHERENT flag means no need for explicit flush)
        _context.VulkanApi.UnmapMemory(_context.Device, memory);
    }
    
    /// <inheritdoc />
    public void DestroyBuffer(Buffer buffer, DeviceMemory memory)
    {
        
        // Wait for GPU to finish using the buffer
        _context.VulkanApi.DeviceWaitIdle(_context.Device);
        
        _context.VulkanApi.DestroyBuffer(_context.Device, buffer, null);
        _context.VulkanApi.FreeMemory(_context.Device, memory, null);
    }
    
    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        _context.VulkanApi.GetPhysicalDeviceMemoryProperties(_context.PhysicalDevice, out var memProperties);
        
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
