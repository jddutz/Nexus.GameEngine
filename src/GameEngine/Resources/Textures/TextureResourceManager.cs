using StbImageSharp;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Implements texture resource management with caching, reference counting, and Vulkan image creation.
/// Loads textures from embedded resources using StbImageSharp.
/// </summary>
public unsafe class TextureResourceManager : VulkanResourceManager<TextureDefinition, TextureResource>, ITextureResourceManager
{
    private readonly IBufferManager _bufferManager;
    private readonly Graphics.Commands.ICommandPoolManager _commandPoolManager;
    
    public TextureResourceManager(
        ILoggerFactory loggerFactory,
        IGraphicsContext context,
        IBufferManager bufferManager,
        Graphics.Commands.ICommandPoolManager commandPoolManager)
        : base(loggerFactory, context)
    {
        _bufferManager = bufferManager;
        _commandPoolManager = commandPoolManager;
        
        // Configure StbImage: do NOT flip images vertically on load
        // PNG images are stored with (0,0) at top-left, matching our UV coordinate system
        // where V=0 is top and V=1 is bottom (standard Vulkan/D3D texture coordinates).
        // The TexturedQuad geometry defines UV coordinates to match this convention.
        StbImage.stbi_set_flip_vertically_on_load(0);
    }
    
    /// <summary>
    /// Gets a string key for logging purposes (uses texture name).
    /// </summary>
    protected override string GetResourceKey(TextureDefinition definition)
    {
        return definition.Name;
    }
    
    /// <summary>
    /// Creates a new texture resource from a definition.
    /// Loads texture data from source and creates Vulkan image resources.
    /// </summary>
    protected override TextureResource CreateResource(TextureDefinition definition)
    {
        // 1. Load texture data from source
        var sourceData = definition.Source.Load();
        
        // 2. Validate source data
        if (sourceData.PixelData == null || sourceData.PixelData.Length == 0)
        {
            throw new InvalidOperationException($"Texture source '{definition.Name}' returned null or empty pixel data");
        }
        
        if (sourceData.Width <= 0 || sourceData.Height <= 0)
        {
            throw new InvalidOperationException($"Texture source '{definition.Name}' returned invalid dimensions: {sourceData.Width}x{sourceData.Height}");
        }
        
        // 3. Create Vulkan texture from source data
        return CreateVulkanTexture(sourceData, definition.Name);
    }
    
    /// <summary>
    /// Creates Vulkan texture resources from texture source data.
    /// This is the low-level Vulkan implementation that pins memory, creates images, and uploads data.
    /// </summary>
    private TextureResource CreateVulkanTexture(TextureSourceData sourceData, string name)
    {
        // Pin the pixel data for upload
        var pixelDataHandle = GCHandle.Alloc(sourceData.PixelData, GCHandleType.Pinned);
        
        try
        {
            var pixelPtr = pixelDataHandle.AddrOfPinnedObject();
            
            // 1. Create Vulkan image
            var image = CreateVulkanImage(sourceData.Width, sourceData.Height, sourceData.Format);
            
            // 2. Allocate and bind memory
            var imageMemory = AllocateImageMemory(image);
            
            // 3. Upload pixels via staging buffer
            UploadPixelsToImage(image, pixelPtr, sourceData.Width, sourceData.Height, sourceData.Format);
            
            // 4. Transition image layout to shader read
            TransitionImageLayout(image, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, sourceData.Format);
            
            // 5. Create image view
            var imageView = CreateImageView(image, sourceData.Format);
            
            // 6. Create sampler
            var sampler = CreateSampler();
            
            return new TextureResource(
                image,
                imageMemory,
                imageView,
                sampler,
                (uint)sourceData.Width,
                (uint)sourceData.Height,
                sourceData.Format,
                name);
        }
        finally
        {
            // Unpin the pixel data
            pixelDataHandle.Free();
        }
    }
    
    private Image CreateVulkanImage(int width, int height, Format format)
    {
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D
            {
                Width = (uint)width,
                Height = (uint)height,
                Depth = 1
            },
            MipLevels = 1,
            ArrayLayers = 1,
            Format = format,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            SharingMode = SharingMode.Exclusive,
            Samples = SampleCountFlags.Count1Bit,
            Flags = 0
        };
        
        Image image;
        var result = _vk.CreateImage(_context.Device, &imageInfo, null, &image);
        
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create Vulkan image: {result}");
        }
        
        return image;
    }
    
    private DeviceMemory AllocateImageMemory(Image image)
    {
        _vk.GetImageMemoryRequirements(_context.Device, image, out var memRequirements);
        
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits,
                MemoryPropertyFlags.DeviceLocalBit)
        };
        
        DeviceMemory imageMemory;
        var result = _vk.AllocateMemory(_context.Device, &allocInfo, null, &imageMemory);
        
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to allocate image memory: {result}");
        }
        
        _vk.BindImageMemory(_context.Device, image, imageMemory, 0);
        
        
        return imageMemory;
    }
    
    private void UploadPixelsToImage(Image image, IntPtr pixels, int width, int height, Format format = Format.R8G8B8A8Srgb)
    {
        // Calculate bytes per pixel based on format
        int bytesPerPixel = format switch
        {
            Format.R8Unorm => 1,
            Format.R8Srgb => 1,
            Format.R8G8Unorm => 2,
            Format.R8G8Srgb => 2,
            Format.R8G8B8Unorm => 3,
            Format.R8G8B8Srgb => 3,
            Format.R8G8B8A8Unorm => 4,
            Format.R8G8B8A8Srgb => 4,
            _ => 4 // Default to RGBA for compatibility
        };
        
        var imageSize = (ulong)(width * height * bytesPerPixel);
        
        // Create staging buffer
        var stagingBuffer = CreateStagingBuffer(imageSize, out var stagingMemory);
        
        try
        {
            // Copy pixel data to staging buffer
            void* data;
            _vk.MapMemory(_context.Device, stagingMemory, 0, imageSize, 0, &data);
            System.Buffer.MemoryCopy((void*)pixels, data, (long)imageSize, (long)imageSize);
            _vk.UnmapMemory(_context.Device, stagingMemory);
            
            // Transition image to transfer destination
            TransitionImageLayout(image, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, format);
            
            // Copy buffer to image
            CopyBufferToImage(stagingBuffer, image, (uint)width, (uint)height);
            
        }
        finally
        {
            // Cleanup staging resources
            _vk.DestroyBuffer(_context.Device, stagingBuffer, null);
            _vk.FreeMemory(_context.Device, stagingMemory, null);
        }
    }
    
    private Silk.NET.Vulkan.Buffer CreateStagingBuffer(ulong size, out DeviceMemory memory)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = BufferUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive
        };
        
        Silk.NET.Vulkan.Buffer buffer;
        var result = _vk.CreateBuffer(_context.Device, &bufferInfo, null, &buffer);
        
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create staging buffer: {result}");
        }
        
        _vk.GetBufferMemoryRequirements(_context.Device, buffer, out var memRequirements);
        
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
        };
        
        DeviceMemory stagingMemory;
        result = _vk.AllocateMemory(_context.Device, &allocInfo, null, &stagingMemory);
        
        if (result != Result.Success)
        {
            _vk.DestroyBuffer(_context.Device, buffer, null);
            throw new InvalidOperationException($"Failed to allocate staging buffer memory: {result}");
        }
        
        _vk.BindBufferMemory(_context.Device, buffer, stagingMemory, 0);
        
        memory = stagingMemory;
        return buffer;
    }
    
    private void CopyBufferToImage(Silk.NET.Vulkan.Buffer buffer, Image image, uint width, uint height)
    {
        var commandBuffer = BeginSingleTimeCommands();
        
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
            ImageExtent = new Extent3D(width, height, 1)
        };
        
        _vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, &region);
        
        EndSingleTimeCommands(commandBuffer);
    }
    
    private void TransitionImageLayout(Image image, ImageLayout oldLayout, ImageLayout newLayout, Format format)
    {
        var commandBuffer = BeginSingleTimeCommands();
        
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };
        
        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;
        
        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;
            
            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;
            
            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else
        {
            throw new NotSupportedException($"Unsupported layout transition: {oldLayout} -> {newLayout}");
        }
        
        _vk.CmdPipelineBarrier(
            commandBuffer,
            sourceStage, destinationStage,
            0,
            0, null,
            0, null,
            1, &barrier);
        
        EndSingleTimeCommands(commandBuffer);
    }
    
    private CommandBuffer BeginSingleTimeCommands()
    {
        // Get the graphics pool for one-time commands
        var pool = _commandPoolManager.GetOrCreatePool(Graphics.Commands.CommandPoolType.TransientGraphics);
        
        var commandBuffers = pool.AllocateCommandBuffers(1, CommandBufferLevel.Primary);
        var commandBuffer = commandBuffers[0];
        
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        
        _vk.BeginCommandBuffer(commandBuffer, &beginInfo);
        
        return commandBuffer;
    }
    
    private void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        _vk.EndCommandBuffer(commandBuffer);
        
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };
        
        _vk.QueueSubmit(_context.GraphicsQueue, 1, &submitInfo, default);
        _vk.QueueWaitIdle(_context.GraphicsQueue);
        
        // Free the command buffer by resetting the pool
        // Note: This is safe because we waited for the queue to be idle
        var pool = _commandPoolManager.GetOrCreatePool(Graphics.Commands.CommandPoolType.TransientGraphics);
        pool.FreeCommandBuffers(new[] { commandBuffer });
    }
    
    private ImageView CreateImageView(Image image, Format format)
    {
        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };
        
        ImageView imageView;
        var result = _vk.CreateImageView(_context.Device, &viewInfo, null, &imageView);
        
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create image view: {result}");
        }
        
        
        return imageView;
    }
    
    private Sampler CreateSampler()
    {
        var samplerInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            AnisotropyEnable = false,
            MaxAnisotropy = 1.0f,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
            MipLodBias = 0.0f,
            MinLod = 0.0f,
            MaxLod = 0.0f
        };
        
        Sampler sampler;
        var result = _vk.CreateSampler(_context.Device, &samplerInfo, null, &sampler);
        
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create sampler: {result}");
        }
        
        
        return sampler;
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
        
        throw new InvalidOperationException("Failed to find suitable memory type");
    }
    
    /// <summary>
    /// Destroys a texture resource, freeing all Vulkan handles.
    /// Called by base class when reference count reaches zero.
    /// </summary>
    protected override void DestroyResource(TextureResource resource)
    {
        if (resource.Sampler.Handle != 0)
        {
            _vk.DestroySampler(_context.Device, resource.Sampler, null);
        }
        if (resource.ImageView.Handle != 0)
        {
            _vk.DestroyImageView(_context.Device, resource.ImageView, null);
        }
        if (resource.Image.Handle != 0)
        {
            _vk.DestroyImage(_context.Device, resource.Image, null);
        }
        if (resource.ImageMemory.Handle != 0)
        {
            _vk.FreeMemory(_context.Device, resource.ImageMemory, null);
        }
    }
}
