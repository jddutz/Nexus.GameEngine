using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Buffers;
using Nexus.GameEngine.Graphics.Commands;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using StbTrueTypeSharp;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Resources.Fonts;

/// <summary>
/// Manages font resource lifecycle including loading, atlas generation, and caching.
/// Generates font atlases using StbTrueType and creates GPU textures directly with Vulkan.
/// </summary>
public unsafe class FontResourceManager : VulkanResourceManager<FontDefinition, FontResource>, IFontResourceManager
{
    private readonly IBufferManager _bufferManager;
    private readonly ICommandPoolManager _commandPoolManager;

    private const int AtlasWidth = 1024;
    private const int AtlasHeight = 1024;
    private const int Padding = 2; // Padding between glyphs to prevent bleeding

    public FontResourceManager(
        ILoggerFactory loggerFactory,
        IGraphicsContext context,
        IBufferManager bufferManager,
        ICommandPoolManager commandPoolManager)
        : base(loggerFactory, context)
    {
        _bufferManager = bufferManager;
        _commandPoolManager = commandPoolManager;
    }

    /// <summary>
    /// Gets a string key for logging purposes.
    /// </summary>
    protected override string GetResourceKey(FontDefinition definition)
    {
        return definition.Name;
    }

    /// <summary>
    /// Creates a new font resource from a definition.
    /// Loads font file, generates atlas with StbTrueType, and creates GPU texture.
    /// </summary>
    protected override FontResource CreateResource(FontDefinition definition)
    {
        _logger.LogInformation("Loading font: {FontName} @ {FontSize}px, Range: {CharRange}",
            definition.Name, definition.FontSize, definition.CharacterRange);

        // Load font file using source
        var sourceData = definition.Source.Load();
        var fontData = sourceData.FontFileData;

        // Generate atlas
        var (atlasData, glyphs, lineHeight, ascender, descender) = GenerateAtlas(fontData, definition);

        // Create GPU texture directly with Vulkan
        var atlasTexture = CreateAtlasTexture(atlasData, definition.Name);

        // Create and return resource
        var resource = new FontResource(atlasTexture, glyphs, lineHeight, ascender, descender, definition.FontSize);

        _logger.LogInformation("Font loaded successfully: {GlyphCount} glyphs, {LineHeight}px line height",
            glyphs.Count, lineHeight);

        return resource;
    }

    /// <summary>
    /// Creates a Vulkan texture from font atlas bitmap data.
    /// </summary>
    private Textures.TextureResource CreateAtlasTexture(byte[] atlasData, string name)
    {
        // Pin the pixel data for upload
        var pixelDataHandle = GCHandle.Alloc(atlasData, GCHandleType.Pinned);
        
        try
        {
            var pixelPtr = pixelDataHandle.AddrOfPinnedObject();
            
            // 1. Create Vulkan image
            var image = CreateVulkanImage(AtlasWidth, AtlasHeight, Format.R8Unorm);
            
            // 2. Allocate and bind memory
            var imageMemory = AllocateImageMemory(image);
            
            // 3. Upload pixels via staging buffer
            UploadPixelsToImage(image, pixelPtr, AtlasWidth, AtlasHeight, Format.R8Unorm);
            
            // 4. Transition image layout to shader read
            TransitionImageLayout(image, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, Format.R8Unorm);
            
            // 5. Create image view
            var imageView = CreateImageView(image, Format.R8Unorm);
            
            // 6. Create sampler
            var sampler = CreateSampler();
            
            return new Textures.TextureResource(
                image,
                imageMemory,
                imageView,
                sampler,
                (uint)AtlasWidth,
                (uint)AtlasHeight,
                Format.R8Unorm,
                $"FontAtlas_{name}");
        }
        finally
        {
            // Unpin the pixel data
            pixelDataHandle.Free();
        }
    }

    /// <summary>
    /// Generates a font atlas texture with glyph metrics.
    /// Uses StbTrueType for font parsing and rasterization.
    /// </summary>
    private (byte[] AtlasData, Dictionary<char, GlyphInfo> Glyphs, int LineHeight, int Ascender, int Descender) GenerateAtlas(
        byte[] fontData, FontDefinition definition)
    {
        // Parse font with StbTrueType
        var fontInfo = StbTrueType.CreateFont(fontData, 0);
        if (fontInfo == null)
        {
            throw new InvalidOperationException($"Failed to initialize font: {definition.Name}");
        }

        // Get font metrics
        float scale = StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, definition.FontSize);
        
        unsafe
        {
            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(fontInfo, &ascent, &descent, &lineGap);

            int ascender = (int)(ascent * scale);
            int descender = (int)(descent * scale);
            int lineHeight = (int)((ascent - descent + lineGap) * scale);

            // Determine character range
            var characters = GetCharacterRange(definition.CharacterRange);

            // Create atlas bitmap
            var atlasData = new byte[AtlasWidth * AtlasHeight];
            var glyphs = new Dictionary<char, GlyphInfo>();

            // Simple rectangle packing - pack glyphs left-to-right, top-to-bottom
            int currentX = Padding;
            int currentY = Padding;
            int rowHeight = 0;

            foreach (char c in characters)
            {
                // Get glyph metrics
                int glyphIndex = StbTrueType.stbtt_FindGlyphIndex(fontInfo, c);
                if (glyphIndex == 0 && c != ' ')
                {
                    // Glyph not found in font, skip
                    continue;
                }

                int advance, leftSideBearing;
                StbTrueType.stbtt_GetGlyphHMetrics(fontInfo, glyphIndex, &advance, &leftSideBearing);
                
                int x0, y0, x1, y1;
                StbTrueType.stbtt_GetGlyphBitmapBox(fontInfo, glyphIndex, scale, scale,
                    &x0, &y0, &x1, &y1);

                int glyphWidth = x1 - x0;
                int glyphHeight = y1 - y0;

                // Check if we need to move to next row
                if (currentX + glyphWidth + Padding > AtlasWidth)
                {
                    currentX = Padding;
                    currentY += rowHeight + Padding;
                    rowHeight = 0;
                }

                // Check if we're out of vertical space
                if (currentY + glyphHeight + Padding > AtlasHeight)
                {
                    _logger?.LogWarning("Font atlas ran out of space at character '{Char}' (U+{Code:X4}). " +
                        "Remaining characters will be skipped.", c, (int)c);
                    break;
                }

                // Rasterize glyph
                fixed (byte* atlasPtr = atlasData)
                {
                    byte* glyphPtr = atlasPtr + currentY * AtlasWidth + currentX;
                    StbTrueType.stbtt_MakeGlyphBitmap(fontInfo, glyphPtr, glyphWidth, glyphHeight,
                        AtlasWidth, scale, scale, glyphIndex);
                }

                // Calculate UV coordinates (normalized 0-1)
                float uvMinX = (float)currentX / AtlasWidth;
                float uvMinY = (float)currentY / AtlasHeight;
                float uvMaxX = (float)(currentX + glyphWidth) / AtlasWidth;
                float uvMaxY = (float)(currentY + glyphHeight) / AtlasHeight;

                // Store glyph info
                glyphs[c] = new GlyphInfo
                {
                    Character = c,
                    TexCoordMin = new Vector2D<float>(uvMinX, uvMinY),
                    TexCoordMax = new Vector2D<float>(uvMaxX, uvMaxY),
                    Width = glyphWidth,
                    Height = glyphHeight,
                    BearingX = x0,
                    BearingY = -y0, // Note: StbTrueType uses negative Y
                    Advance = (int)(advance * scale)
                };

                // Update position
                currentX += glyphWidth + Padding;
                rowHeight = Math.Max(rowHeight, glyphHeight);
            }

            _logger?.LogDebug("Generated font atlas: {Width}x{Height}, {GlyphCount} glyphs",
                AtlasWidth, AtlasHeight, glyphs.Count);

            return (atlasData, glyphs, lineHeight, ascender, descender);
        }
    }

    /// <summary>
    /// Gets the list of characters for the specified character range.
    /// </summary>
    private static List<char> GetCharacterRange(CharacterRange range)
    {
        return range switch
        {
            CharacterRange.AsciiPrintable => Enumerable.Range(32, 95).Select(i => (char)i).ToList(),
            CharacterRange.Extended => Enumerable.Range(32, 224).Select(i => (char)i).ToList(),
            CharacterRange.Custom => throw new NotSupportedException("Custom character ranges not yet implemented"),
            _ => throw new ArgumentException($"Unknown character range: {range}")
        };
    }

    #region Vulkan Helper Methods

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
    
    private void UploadPixelsToImage(Image image, IntPtr pixels, int width, int height, Format format)
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
        var pool = _commandPoolManager.GetOrCreatePool(CommandPoolType.TransientGraphics);
        
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
        var pool = _commandPoolManager.GetOrCreatePool(CommandPoolType.TransientGraphics);
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

    #endregion

    /// <summary>
    /// Destroys a font resource, freeing all Vulkan handles.
    /// </summary>
    protected override void DestroyResource(FontResource resource)
    {
        if (resource.AtlasTexture.Sampler.Handle != 0)
        {
            _vk.DestroySampler(_context.Device, resource.AtlasTexture.Sampler, null);
        }
        if (resource.AtlasTexture.ImageView.Handle != 0)
        {
            _vk.DestroyImageView(_context.Device, resource.AtlasTexture.ImageView, null);
        }
        if (resource.AtlasTexture.Image.Handle != 0)
        {
            _vk.DestroyImage(_context.Device, resource.AtlasTexture.Image, null);
        }
        if (resource.AtlasTexture.ImageMemory.Handle != 0)
        {
            _vk.FreeMemory(_context.Device, resource.AtlasTexture.ImageMemory, null);
        }
    }
}
