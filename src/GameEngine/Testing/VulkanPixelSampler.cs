using Nexus.GameEngine.Graphics.Commands;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nexus.GameEngine.Testing;

/// <summary>
/// Vulkan-based implementation of pixel sampling for testing.
/// Subscribes to renderer events and automatically samples pixels when configured.
/// WARNING: This has significant performance impact - only use for testing!
/// </summary>
public unsafe class VulkanPixelSampler : IPixelSampler, IDisposable
{
    private readonly IGraphicsContext _context;
    private readonly ISwapChain _swapChain;
    private readonly ICommandPoolManager _commandPoolManager;
    private readonly Vk _vk;
    
    private bool _enabled;
    private bool _disposed;

    private Buffer _stagingBuffer;
    private DeviceMemory _stagingMemory;
    private ulong _bufferSize;
    
    private Vector2D<int>[] _sampleCoordinates = Array.Empty<Vector2D<int>>();
    private bool _isActive;
    private readonly List<Vector4D<float>?[]> _capturedResults = [];

    public VulkanPixelSampler(
        IGraphicsContext context,
        ISwapChain swapChain,
        ICommandPoolManager commandPoolManager)
    {
        _context = context;
        _swapChain = swapChain;
        _commandPoolManager = commandPoolManager;
        _vk = context.VulkanApi;
        _enabled = false;
        
        // Log.Warning("VulkanPixelSampler created - this service impacts performance and should only be used for testing");
    }

    /// <inheritdoc />
    public bool IsAvailable => _enabled && !_disposed;
    
    /// <inheritdoc />
    public Vector2D<int>[] SampleCoordinates
    {
        get => _sampleCoordinates;
        set => _sampleCoordinates = value ?? Array.Empty<Vector2D<int>>();
    }
    
    /// <inheritdoc />
    public void Activate()
    {
        _isActive = true;
        _capturedResults.Clear();  // Start fresh when activating
    }
    
    /// <inheritdoc />
    public void Deactivate()
    {
        _isActive = false;
        // Keep results - don't clear here
    }
    
    /// <inheritdoc />
    public Vector4D<float>?[][] GetResults()
    {
        return _capturedResults.ToArray();  // Return copy without clearing
    }

    /// <inheritdoc />
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            
            _enabled = value;
            
            if (_enabled)
            {
                InitializeStagingResources();
                _swapChain.BeforePresent += OnBeforePresent;
            }
            else
            {
                _swapChain.BeforePresent -= OnBeforePresent;
                CleanupStagingResources();
                _capturedResults.Clear();  // Clear results when disabling
            }
        }
    }
    
    private void OnBeforePresent(object? sender, PresentEventArgs e)
    {
        if (!_isActive || _sampleCoordinates.Length == 0) return;
        
        var samples = SamplePixelsFromImage(e.Image);
        _capturedResults.Add(samples);
    }

    /// <summary>
    /// Samples pixels from the specified swapchain image.
    /// Called internally by the BeforePresent event handler.
    /// </summary>
    private Vector4D<float>?[] SamplePixelsFromImage(Image swapchainImage)
    {
        var results = new Vector4D<float>?[_sampleCoordinates.Length];

        if (!IsAvailable) return results;

        // Wait for device to be idle to ensure frame is complete
        _vk.DeviceWaitIdle(_context.Device);
        
        // Copy swapchain image to staging buffer once
        CopyImageToBuffer(swapchainImage);
        
        // Map staging buffer memory
        void* mappedData;
        _vk.MapMemory(_context.Device, _stagingMemory, 0, _bufferSize, 0, &mappedData);
        
        try
        {
            var extent = _swapChain.SwapchainExtent;
            var format = _swapChain.SwapchainFormat;
            
            // Read all requested pixels
            for (int i = 0; i < _sampleCoordinates.Length; i++)
            {
                var x = _sampleCoordinates[i].X;
                var y = _sampleCoordinates[i].Y;
                
                // Validate coordinates
                if (x < 0 || x >= extent.Width || y < 0 || y >= extent.Height)
                {
                    // Log.Warning($"Pixel coordinates out of bounds: ({x}, {y})");
                    results[i] = null;
                    continue;
                }
                
                // Calculate pixel offset
                var pixelOffset = (y * extent.Width + x) * 4;
                var pixelPtr = (byte*)mappedData + pixelOffset;
                
                // Read pixel based on format
                if (format == Format.B8G8R8A8Srgb || format == Format.B8G8R8A8Unorm)
                {
                    // BGRA format
                    var color = new Vector4D<float>(
                        pixelPtr[2] / 255.0f, // R
                        pixelPtr[1] / 255.0f, // G
                        pixelPtr[0] / 255.0f, // B
                        pixelPtr[3] / 255.0f  // A
                    );
                    
                    // Convert from SRGB to linear if format is SRGB
                    results[i] = (format == Format.B8G8R8A8Srgb) ? SrgbToLinear(color) : color;
                }
                else if (format == Format.R8G8B8A8Srgb || format == Format.R8G8B8A8Unorm)
                {
                    // RGBA format
                    var color = new Vector4D<float>(
                        pixelPtr[0] / 255.0f, // R
                        pixelPtr[1] / 255.0f, // G
                        pixelPtr[2] / 255.0f, // B
                        pixelPtr[3] / 255.0f  // A
                    );
                    
                    // Convert from SRGB to linear if format is SRGB
                    results[i] = (format == Format.R8G8B8A8Srgb) ? SrgbToLinear(color) : color;
                }
                else
                {
                    results[i] = null;
                }
            }
        }
        finally
        {
            _vk.UnmapMemory(_context.Device, _stagingMemory);
        }
        
        return results;
    }

    /// <summary>
    /// Converts an SRGB color value to linear color space.
    /// SRGB uses a gamma curve for better perceptual uniformity.
    /// </summary>
    private static Vector4D<float> SrgbToLinear(Vector4D<float> srgb)
    {
        // Alpha is always linear
        return new Vector4D<float>(
            SrgbChannelToLinear(srgb.X),
            SrgbChannelToLinear(srgb.Y),
            SrgbChannelToLinear(srgb.Z),
            srgb.W
        );
    }
    
    /// <summary>
    /// Converts a single SRGB color channel to linear.
    /// Uses the standard SRGB to linear conversion formula.
    /// </summary>
    private static float SrgbChannelToLinear(float srgb)
    {
        if (srgb <= 0.04045f)
            return srgb / 12.92f;
        else
            return MathF.Pow((srgb + 0.055f) / 1.055f, 2.4f);
    }

    /// <summary>
    /// Copies a swapchain image to the staging buffer for CPU readback.
    /// Uses a one-time submit command buffer for the copy operation.
    /// </summary>
    private void CopyImageToBuffer(Image sourceImage)
    {
        var extent = _swapChain.SwapchainExtent;
        
        // Allocate a one-time command buffer
        var commandPool = _commandPoolManager.GraphicsPool;
        var cmdBuffers = commandPool.AllocateCommandBuffers(1, CommandBufferLevel.Primary);
        var cmd = cmdBuffers[0];
        
        // Begin command buffer
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        
        _vk.BeginCommandBuffer(cmd, &beginInfo);
        
        // Transition image layout from PresentSrcKhr to TransferSrcOptimal
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.PresentSrcKhr,
            NewLayout = ImageLayout.TransferSrcOptimal,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = sourceImage,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            SrcAccessMask = AccessFlags.MemoryReadBit,
            DstAccessMask = AccessFlags.TransferReadBit
        };
        
        _vk.CmdPipelineBarrier(
            cmd,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.TransferBit,
            0,
            0, null,
            0, null,
            1, &barrier
        );
        
        // Copy image to buffer
        var region = new BufferImageCopy
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = new Extent3D(extent.Width, extent.Height, 1)
        };
        
        _vk.CmdCopyImageToBuffer(
            cmd,
            sourceImage,
            ImageLayout.TransferSrcOptimal,
            _stagingBuffer,
            1,
            &region
        );
        
        // Transition image layout back to PresentSrcKhr
        barrier.OldLayout = ImageLayout.TransferSrcOptimal;
        barrier.NewLayout = ImageLayout.PresentSrcKhr;
        barrier.SrcAccessMask = AccessFlags.TransferReadBit;
        barrier.DstAccessMask = AccessFlags.MemoryReadBit;
        
        _vk.CmdPipelineBarrier(
            cmd,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.BottomOfPipeBit,
            0,
            0, null,
            0, null,
            1, &barrier
        );
        
        _vk.EndCommandBuffer(cmd);
        
        // Submit command buffer
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd
        };
        
        _vk.QueueSubmit(_context.GraphicsQueue, 1, &submitInfo, default);
        _vk.QueueWaitIdle(_context.GraphicsQueue);
        
        // Free command buffer
        commandPool.FreeCommandBuffers(cmdBuffers);
    }

    private void InitializeStagingResources()
    {
        var extent = _swapChain.SwapchainExtent;
        
        // Calculate buffer size: width * height * 4 bytes per pixel (RGBA)
        _bufferSize = (ulong)(extent.Width * extent.Height * 4);
        
        // Create staging buffer
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = _bufferSize,
            Usage = BufferUsageFlags.TransferDstBit, // Destination for image copy
            SharingMode = SharingMode.Exclusive
        };
        
        fixed (Buffer* pBuffer = &_stagingBuffer)
        {
            var result = _vk.CreateBuffer(_context.Device, &bufferInfo, null, pBuffer);
            if (result != Result.Success)
            {
                // Log.Error($"Failed to create staging buffer: {result}");
                throw new Exception($"Failed to create staging buffer: {result}");
            }
        }
        
        // Allocate memory for staging buffer
        _vk.GetBufferMemoryRequirements(_context.Device, _stagingBuffer, out var memRequirements);
        
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(
                memRequirements.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
        };
        
        fixed (DeviceMemory* pMemory = &_stagingMemory)
        {
            var result = _vk.AllocateMemory(_context.Device, &allocInfo, null, pMemory);
            if (result != Result.Success)
            {
                _vk.DestroyBuffer(_context.Device, _stagingBuffer, null);
                // Log.Error($"Failed to allocate staging buffer memory: {result}");
                throw new Exception($"Failed to allocate staging buffer memory: {result}");
            }
        }
        
        // Bind buffer memory
        _vk.BindBufferMemory(_context.Device, _stagingBuffer, _stagingMemory, 0);
    }

    private void CleanupStagingResources()
    {
        if (_stagingBuffer.Handle != 0)
        {
            // Wait for device to finish using the buffer
            _vk.DeviceWaitIdle(_context.Device);
            
            _vk.DestroyBuffer(_context.Device, _stagingBuffer, null);
            _stagingBuffer = default;
        }
        
        if (_stagingMemory.Handle != 0)
        {
            _vk.FreeMemory(_context.Device, _stagingMemory, null);
            _stagingMemory = default;
        }
        
        _bufferSize = 0;
    }
    
    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        _vk.GetPhysicalDeviceMemoryProperties(_context.PhysicalDevice, out var memProperties);
        
        for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << (int)i)) != 0 &&
                (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }
        
        throw new Exception("Failed to find suitable memory type for staging buffer");
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        CleanupStagingResources();
        _disposed = true;
    }
}
