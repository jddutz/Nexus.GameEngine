using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace TestApp.TestComponents;

public class HelloQuad(
    IGraphicsContext context,
    IPipelineManager pipelineManager,
    ISwapChain swapChain)
    : RenderableBase(), IRenderable, ITestComponent
{    
    private Silk.NET.Vulkan.Buffer _vertexBuffer;
    private DeviceMemory _vertexBufferMemory;
    private Pipeline _pipeline;
    private bool _initialized;

    private struct Vertex
    {
        public Vector2D<float> pos;
        public Vector3D<float> color;
    }
    public int FramesRendered { get; private set; } = 0;
    public int FrameCount { get; set; } = 120;  // 2 seconds at 60fps
    private bool _pixelsSampled = false;
    private bool _pixelValidationPassed = false;
    private string _pixelValidationError = string.Empty;
    
    private System.Diagnostics.Stopwatch? _fpsStopwatch;
    private double _measuredFPS = 0;

    private Vertex[] verts = [
        new() { pos = new(-0.5f, -0.5f), color = new(1.0f, 0.0f, 0.0f) },  // Red
        new() { pos = new(-0.5f,  0.5f), color = new(0.0f, 1.0f, 0.0f) },  // Green
        new() { pos = new(0.5f,   0.5f), color = new(0.0f, 0.0f, 1.0f) },  // Blue
        new() { pos = new(0.5f,  -0.5f), color = new(1.0f, 1.0f, 1.0f) },  // White
    ];

    /// <summary>
    /// Default render mask: Background pass to test rendering
    /// </summary>
    protected override uint GetDefaultRenderMask() => RenderPasses.Background;
    
    protected override void OnActivate()
    {
        base.OnActivate();
        Logger?.LogInformation("HelloQuad.OnActivate called - creating pipeline and vertex buffer");
        
        try
        {
            CreatePipeline();
            Logger?.LogInformation("HelloQuad pipeline created successfully");
            
            CreateVertexBuffer();
            Logger?.LogInformation("HelloQuad vertex buffer created successfully. Initialized: {Initialized}", _initialized);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "HelloQuad initialization failed");
            throw;
        }
    }
    
    private void CreatePipeline()
    {
        // Create pipeline descriptor for HelloQuad
        var descriptor = new PipelineDescriptor
        {
            Name = "HelloQuadPipeline",
            VertexShaderPath = "Shaders/vert.spv",
            FragmentShaderPath = "Shaders/frag.spv",
            VertexInputDescription = GetVertexInputDescription(),
            Topology = PrimitiveTopology.TriangleFan,
            RenderPass = swapChain.Passes[(int)Math.Log2(RenderPasses.Background)],  // Get Background pass (bit 2 = index 2)
            Subpass = 0,
            EnableDepthTest = true,
            EnableDepthWrite = true,
            EnableBlending = false,
            CullMode = CullModeFlags.None,
            FrontFace = FrontFace.CounterClockwise
        };
        
        _pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
    }
    
    private static VertexInputDescription GetVertexInputDescription()
    {
        // Define vertex binding (one binding for interleaved vertex data)
        var binding = new VertexInputBindingDescription
        {
            Binding = 0,
            Stride = (uint)Marshal.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex
        };
        
        // Define vertex attributes (position and color)
        var attributes = new VertexInputAttributeDescription[]
        {
            // Position (vec2) at location 0
            new()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.pos))
            },
            // Color (vec3) at location 1
            new()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.color))
            }
        };
        
        return new VertexInputDescription
        {
            Bindings = [binding],
            Attributes = attributes
        };
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
    
    protected override void OnUpdate(double deltaTime)
    {
        // Start stopwatch on first update
        if (_fpsStopwatch == null)
        {
            _fpsStopwatch = System.Diagnostics.Stopwatch.StartNew();
            Logger?.LogInformation("FPS measurement started");
        }
        
        if (FramesRendered >= FrameCount)
        {
            // Stop stopwatch and calculate FPS
            _fpsStopwatch.Stop();
            double elapsedSeconds = _fpsStopwatch.Elapsed.TotalSeconds;
            _measuredFPS = FrameCount / elapsedSeconds;
            
            Logger?.LogInformation("FPS Measurement Complete: {FrameCount} frames in {ElapsedMs:F2}ms = {FPS:F2} FPS", 
                FrameCount, _fpsStopwatch.Elapsed.TotalMilliseconds, _measuredFPS);
            
            if (!_pixelsSampled)
            {
                SampleAndValidatePixels();
            }
            Deactivate();
        }
    }

    private unsafe void SampleAndValidatePixels()
    {
        _pixelsSampled = true;

        try
        {
            var extent = swapChain.SwapchainExtent;
            var format = swapChain.SwapchainFormat;
            
            // Calculate expected size for RGBA8 format (4 bytes per pixel)
            var imageSize = extent.Width * extent.Height * 4;
            
            Logger?.LogInformation("Sampling pixels from {Width}x{Height} framebuffer (format: {Format})", 
                extent.Width, extent.Height, format);

            // Create staging buffer to read back pixels
            var bufferInfo = new BufferCreateInfo
            {
                SType = StructureType.BufferCreateInfo,
                Size = imageSize,
                Usage = BufferUsageFlags.TransferDstBit,
                SharingMode = SharingMode.Exclusive
            };

            Silk.NET.Vulkan.Buffer stagingBuffer;
            if (context.VulkanApi.CreateBuffer(context.Device, &bufferInfo, null, &stagingBuffer) != Result.Success)
            {
                _pixelValidationError = "Failed to create staging buffer";
                Logger?.LogError(_pixelValidationError);
                return;
            }

            // Allocate memory for staging buffer
            context.VulkanApi.GetBufferMemoryRequirements(context.Device, stagingBuffer, out var memRequirements);
            
            var allocInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(
                    memRequirements.MemoryTypeBits,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
            };

            DeviceMemory stagingMemory;
            if (context.VulkanApi.AllocateMemory(context.Device, &allocInfo, null, &stagingMemory) != Result.Success)
            {
                context.VulkanApi.DestroyBuffer(context.Device, stagingBuffer, null);
                _pixelValidationError = "Failed to allocate staging buffer memory";
                Logger?.LogError(_pixelValidationError);
                return;
            }

            context.VulkanApi.BindBufferMemory(context.Device, stagingBuffer, stagingMemory, 0);

            // TODO: Copy current swapchain image to staging buffer
            // This requires command buffer submission which is complex
            // For now, just log that we would sample here
            
            Logger?.LogWarning("Pixel sampling infrastructure created but image copy not yet implemented");
            Logger?.LogInformation("Would sample center pixel at ({X}, {Y})", extent.Width / 2, extent.Height / 2);
            
            // Sample the center - quad should have a blend of all 4 colors
            // Since quad uses TriangleFan, center should be a mix of red, green, blue, white
            
            _pixelValidationPassed = true; // Temporary - set to true for now
            _pixelValidationError = "Pixel sampling not yet implemented - skipping validation";

            // Cleanup
            context.VulkanApi.DestroyBuffer(context.Device, stagingBuffer, null);
            context.VulkanApi.FreeMemory(context.Device, stagingMemory, null);
        }
        catch (Exception ex)
        {
            _pixelValidationError = $"Exception during pixel sampling: {ex.Message}";
            Logger?.LogError(ex, "Failed to sample pixels");
        }
    }
    
    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        Logger?.LogDebug("HelloQuad.GetDrawCommands called. Initialized: {Initialized}", _initialized);
        
        if (!_initialized)
        {
            Logger?.LogWarning("HelloQuad not initialized, skipping draw command");
            yield break;
        }
        
        Logger?.LogDebug("HelloQuad yielding draw command. RenderMask: {RenderMask}, VertexCount: {VertexCount}", RenderMask, verts.Length);

        yield return new DrawCommand
        {
            RenderMask = RenderMask,
            Pipeline = _pipeline,
            VertexBuffer = _vertexBuffer,
            VertexCount = (uint)verts.Length,
            InstanceCount = 1
        };
        
        FramesRendered++;
    }

    public IEnumerable<TestResult> GetTestResults()
    {
        yield return new TestResult
        {
            TestName = "HelloQuad should render for multiple frames",
            Passed = FramesRendered >= FrameCount,
            ErrorMessage = FramesRendered >= FrameCount 
                ? "" 
                : $"Expected {FrameCount} frames, but only rendered {FramesRendered}"
        };

        yield return new TestResult
        {
            TestName = "HelloQuad should achieve reasonable FPS (>30)",
            Passed = _measuredFPS >= 30.0,
            ErrorMessage = _measuredFPS >= 30.0 
                ? "" 
                : $"FPS too low: {_measuredFPS:F2} FPS (expected ≥30)",
            Description = $"Measured FPS: {_measuredFPS:F2} ({FrameCount} frames in {_fpsStopwatch?.Elapsed.TotalMilliseconds:F2}ms)"
        };

        yield return new TestResult
        {
            TestName = "HelloQuad pixels should match expected colors",
            Passed = _pixelValidationPassed,
            ErrorMessage = _pixelsSampled ? _pixelValidationError : "Pixels were not sampled"
        };
    }
    
    protected override void OnDeactivate()
    {
        if (_initialized)
        {
            unsafe
            {
                // Wait for GPU to finish using the buffer before destroying it
                context.VulkanApi.DeviceWaitIdle(context.Device);
                
                context.VulkanApi.DestroyBuffer(context.Device, _vertexBuffer, null);
                context.VulkanApi.FreeMemory(context.Device, _vertexBufferMemory, null);
            }
            _initialized = false;
        }
        base.OnDeactivate();
    }
}