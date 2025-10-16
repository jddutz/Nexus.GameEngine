using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace TestApp.TestComponents;

public class HelloQuad(
    IGraphicsContext context,
    IPipelineManager pipelineManager)
    : RenderableBase(), IRenderable, ITestComponent
{    
    private Silk.NET.Vulkan.Buffer _vertexBuffer;
    private DeviceMemory _vertexBufferMemory;
    private bool _initialized;

    private struct Vertex
    {
        public Vector2D<float> pos;
        public Vector3D<float> color;
    }

    private Vertex[] verts = [
        new() { pos = new(-0.5f, -0.5f), color = new(1.0f, 0.0f, 0.0f) },
        new() { pos = new(-0.5f,  0.5f), color = new(0.0f, 1.0f, 0.0f) },
        new() { pos = new(0.5f,   0.5f), color = new(0.0f, 0.0f, 1.0f) },
        new() { pos = new(0.5f,  -0.5f), color = new(1.0f, 1.0f, 1.0f) },
    ];

    protected override void OnActivate()
    {
        base.OnActivate();
        CreateVertexBuffer();
    }
    
    private unsafe void CreateVertexBuffer()
    {
        if (_initialized) return;
        
        var bufferSize = (ulong)(sizeof(Vertex) * verts.Length);
        
        // Create buffer
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = bufferSize,
            Usage = BufferUsageFlags.VertexBufferBit,
            SharingMode = SharingMode.Exclusive
        };
        
        fixed (Silk.NET.Vulkan.Buffer* bufferPtr = &_vertexBuffer)
        {
            if (context.VulkanApi.CreateBuffer(context.Device, &bufferInfo, null, bufferPtr) != Result.Success)
            {
                throw new Exception("Failed to create vertex buffer");
            }
        }
        
        // Allocate memory
        context.VulkanApi.GetBufferMemoryRequirements(context.Device, _vertexBuffer, out var memRequirements);
        
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(
                memRequirements.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
        };
        
        fixed (DeviceMemory* memoryPtr = &_vertexBufferMemory)
        {
            if (context.VulkanApi.AllocateMemory(context.Device, &allocInfo, null, memoryPtr) != Result.Success)
            {
                throw new Exception("Failed to allocate vertex buffer memory");
            }
        }
        
        // Bind and upload
        context.VulkanApi.BindBufferMemory(context.Device, _vertexBuffer, _vertexBufferMemory, 0);
        
        void* data;
        context.VulkanApi.MapMemory(context.Device, _vertexBufferMemory, 0, bufferSize, 0, &data);
        fixed (Vertex* vertPtr = verts)
        {
            System.Buffer.MemoryCopy(vertPtr, data, (long)bufferSize, (long)bufferSize);
        }
        context.VulkanApi.UnmapMemory(context.Device, _vertexBufferMemory);
        
        _initialized = true;
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
    
    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (!_initialized) yield break;
        
        yield return new DrawCommand
        {
            RenderMask = RenderMask,
            Pipeline = pipelineManager.Get("HelloQuadPipeline"),
            VertexBuffer = _vertexBuffer,
            VertexCount = (uint)verts.Length,
            InstanceCount = 1
        };
    }
    
    protected override void OnDeactivate()
    {
        if (_initialized)
        {
            unsafe
            {
                context.VulkanApi.DestroyBuffer(context.Device, _vertexBuffer, null);
                context.VulkanApi.FreeMemory(context.Device, _vertexBufferMemory, null);
            }
            _initialized = false;
        }
        base.OnDeactivate();
    }

    public IEnumerable<TestResult> GetTestResults()
    {
        throw new NotImplementedException();
    }
}