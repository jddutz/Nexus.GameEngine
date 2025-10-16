using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.GameEngine.Runtime;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages the Vulkan swap chain and the complete presentation pipeline.
/// Owns swapchain resources, render passes, framebuffers, and orchestrates their lifecycle.
/// Handles image presentation, window resize, and resource recreation.
/// </summary>
/// <remarks>
/// <para><strong>Ownership Hierarchy:</strong></para>
/// <list type="bullet">
/// <item>Swapchain → Images (owned by Vulkan)</item>
/// <item>ImageViews → Views into swapchain images</item>
/// <item>RenderPasses → Created from RenderPasses.Configurations static array (survive resize)</item>
/// <item>Framebuffers → Bind ImageViews to RenderPasses (recreated on resize)</item>
/// <item>ClearValues → Per-RenderPass clear values</item>
/// </list>
/// 
/// <para><strong>Initialization Sequence:</strong></para>
/// <list type="number">
/// <item>Query swap chain support details from physical device</item>
/// <item>Choose best surface format from VulkanSettings preferences</item>
/// <item>Choose best present mode from VulkanSettings preferences</item>
/// <item>Determine swap chain extent (window dimensions)</item>
/// <item>Create swap chain with chosen parameters</item>
/// <item>Retrieve swap chain images (N images)</item>
/// <item>Create image views for each swap chain image</item>
/// <item>Create render passes from RenderPasses.Configurations static array</item>
/// <item>Create N framebuffers per render pass</item>
/// </list>
/// 
/// <para><strong>Window Resize Lifecycle:</strong></para>
/// <list type="bullet">
/// <item>Destroy: Framebuffers → ImageViews → Swapchain</item>
/// <item>Recreate: Swapchain → ImageViews → Framebuffers</item>
/// <item>RenderPasses survive (format doesn't change)</item>
/// </list>
/// 
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// var mainRenderPass = swapChain.RenderPasses[0];
/// var framebuffer = swapChain.Framebuffers[mainRenderPass][imageIndex];
/// var clearValues = swapChain.ClearValues[mainRenderPass];
/// </code>
/// </remarks>
public unsafe class SwapChain : ISwapChain
{
    private readonly ILogger _logger;
    private readonly IGraphicsContext _context;
    private readonly IWindowService _windowService;
    private readonly VulkanSettings _settings;
    private readonly Vk _vk;

    private SwapchainKHR _swapchain;
    private Format _swapchainFormat;
    private Extent2D _swapchainExtent;
    private Image[] _swapchainImages = [];
    private ImageView[] _swapchainImageViews = [];
    
    // Depth buffer resources (created if any render pass uses depth)
    private Image _depthImage;
    private DeviceMemory _depthImageMemory;
    private ImageView _depthImageView;
    private Format _depthFormat;
    private bool _hasDepthAttachment;
    
    private const int PassCount = 11; // RenderPasses has 11 passes (0-10)
    private readonly RenderPass[] _renderPasses = new RenderPass[PassCount];
    private readonly Framebuffer[][] _framebuffers = new Framebuffer[PassCount][];
    private readonly ClearValue[][] _clearValues = new ClearValue[PassCount][];

    private KhrSwapchain? _khrSwapchain;

    public SwapChain(
        IGraphicsContext context,
        IWindowService windowService,
        IOptions<VulkanSettings> settings,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(Swapchain));
        _context = context;
        _windowService = windowService;
        _settings = settings.Value;
        _vk = context.VulkanApi;

        // Get the swap chain extension
        if (!_vk.TryGetDeviceExtension(_context.Instance, _context.Device, out _khrSwapchain))
        {
            _logger.LogError("Failed to get KHR_swapchain extension");
            throw new Exception("KHR_swapchain extension not available");
        }

        // Create the swap chain and presentation resources
        CreateSwapchain();
        CreateRenderPasses();
        CreateDepthResources(); // Create depth buffer if any render pass needs it
        CreateAllFramebuffers();
        
        _logger.LogDebug("Swap chain initialization complete with {RenderPassCount} render passes", PassCount);
    }

    public SwapchainKHR Swapchain => _swapchain;
    public Format SwapchainFormat => _swapchainFormat;
    public Extent2D SwapchainExtent => _swapchainExtent;
    public Image[] SwapchainImages => _swapchainImages;
    public ImageView[] SwapchainImageViews => _swapchainImageViews;
    public RenderPass[] Passes => _renderPasses;
    public Framebuffer[][] Framebuffers => _framebuffers;
    public ClearValue[][] ClearValues => _clearValues;
    public Image DepthImage => _depthImage;
    public bool HasDepthAttachment => _hasDepthAttachment;

    /// <summary>
    /// Creates the swap chain with optimal settings based on device capabilities and preferences.
    /// Skips creation if window is minimized (extent 0x0) to avoid Vulkan validation errors.
    /// </summary>
    private void CreateSwapchain()
    {
        var swapChainSupport = _context.QuerySwapChainSupport();

        // Choose best settings from available options
        var surfaceFormat = ChooseSurfaceFormat(swapChainSupport.Formats);
        var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
        var extent = ChooseExtent(swapChainSupport.Capabilities);

        // Skip swapchain creation if window is minimized (0x0 extent)
        if (extent.Width == 0 || extent.Height == 0)
        {
            _logger.LogDebug("Skipping swapchain creation: Window is minimized (extent {Width}x{Height})", 
                extent.Width, extent.Height);
            _swapchainExtent = extent;
            return;
        }

        // Determine image count (request one more than minimum for triple buffering potential)
        uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && 
            imageCount > swapChainSupport.Capabilities.MaxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        // Apply minimum from settings
        if (imageCount < _settings.MinImageCount)
        {
            imageCount = _settings.MinImageCount;
            _logger.LogDebug("Adjusting image count to settings minimum: {MinCount}", imageCount);
        }

        _logger.LogDebug("Creating swap chain: Format={Format}, PresentMode={PresentMode}, Extent={Width}x{Height}, ImageCount={ImageCount}",
            surfaceFormat.Format, presentMode, extent.Width, extent.Height, imageCount);

        // Create swap chain
        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _context.Surface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            PreTransform = swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = default
        };

        // Determine queue family sharing mode
        var graphicsFamily = _context.FindQueueFamily(QueueFlags.GraphicsBit);
        var presentFamily = FindPresentQueueFamily();

        if (!graphicsFamily.HasValue || !presentFamily.HasValue)
        {
            _logger.LogError("Failed to find required queue families");
            throw new Exception("Required queue families not found");
        }

        uint* queueFamilyIndices = stackalloc uint[2];
        queueFamilyIndices[0] = graphicsFamily.Value;
        queueFamilyIndices[1] = presentFamily.Value;

        if (graphicsFamily.Value != presentFamily.Value)
        {
            // Multiple queue families - use concurrent mode
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            createInfo.PQueueFamilyIndices = queueFamilyIndices;
            _logger.LogDebug("Using concurrent sharing mode (graphics family {Graphics} != present family {Present})",
                graphicsFamily.Value, presentFamily.Value);
        }
        else
        {
            // Same queue family - use exclusive mode (better performance)
            createInfo.ImageSharingMode = SharingMode.Exclusive;
            createInfo.QueueFamilyIndexCount = 0;
            createInfo.PQueueFamilyIndices = null;
            _logger.LogDebug("Using exclusive sharing mode (same queue family {Family})", graphicsFamily.Value);
        }

        // Create the swap chain
        SwapchainKHR swapchain;
        var result = _khrSwapchain!.CreateSwapchain(_context.Device, &createInfo, null, &swapchain);
        if (result != Result.Success)
        {
            _logger.LogError("Failed to create swap chain: {Result}", result);
            throw new Exception($"Failed to create swap chain: {result}");
        }

        _swapchain = swapchain;
        _swapchainFormat = surfaceFormat.Format;
        _swapchainExtent = extent;

        _logger.LogDebug("Swap chain created successfully (Handle: {Handle})", _swapchain.Handle);

        // Retrieve swap chain images
        RetrieveSwapchainImages();

        // Create image views
        CreateImageViews();
    }

    /// <summary>
    /// Chooses the best surface format from available options based on GraphicsSettings preferences.
    /// </summary>
    private SurfaceFormatKHR ChooseSurfaceFormat(SurfaceFormatKHR[] availableFormats)
    {
        _logger.LogDebug("Available surface formats: {Count}", availableFormats.Length);
        foreach (var format in availableFormats)
        {
            _logger.LogDebug("  - Format: {Format}, ColorSpace: {ColorSpace}", format.Format, format.ColorSpace);
        }

        // Try each preferred format in order
        foreach (var preferred in _settings.PreferredSurfaceFormats)
        {
            var match = availableFormats.FirstOrDefault(f => 
                f.Format == preferred && f.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr);
            
            if (match.Format != Format.Undefined)
            {
                _logger.LogDebug("Selected surface format: {Format} (preferred)", match.Format);
                return match;
            }
        }

        // Fallback to first available format
        _logger.LogWarning("No preferred surface format available, using fallback: {Format}", availableFormats[0].Format);
        return availableFormats[0];
    }

    /// <summary>
    /// Chooses the best present mode from available options based on GraphicsSettings preferences.
    /// </summary>
    private PresentModeKHR ChoosePresentMode(PresentModeKHR[] availableModes)
    {
        _logger.LogDebug("Available present modes: {Count}", availableModes.Length);
        foreach (var mode in availableModes)
        {
            _logger.LogDebug("  - {Mode}", mode);
        }

        // Try each preferred mode in order
        foreach (var preferred in _settings.PreferredPresentModes)
        {
            if (availableModes.Contains(preferred))
            {
                _logger.LogDebug("Selected present mode: {Mode} (preferred)", preferred);
                return preferred;
            }
        }

        // Fallback to FIFO (guaranteed to be available per Vulkan spec)
        _logger.LogWarning("No preferred present mode available, using FIFO fallback");
        return PresentModeKHR.FifoKhr;
    }

    /// <summary>
    /// Chooses the swap chain extent (resolution) based on window size and surface capabilities.
    /// Returns 0x0 extent when window is minimized to signal that swapchain should not be created.
    /// </summary>
    private Extent2D ChooseExtent(SurfaceCapabilitiesKHR capabilities)
    {
        // If currentExtent is not uint.MaxValue, it's been set by the surface and we must use it
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            _logger.LogDebug("Using surface-defined extent: {Width}x{Height}",
                capabilities.CurrentExtent.Width, capabilities.CurrentExtent.Height);
            return capabilities.CurrentExtent;
        }

        // Otherwise, pick extent based on window size (clamped to surface limits)
        var window = _windowService.GetWindow();
        var actualExtent = new Extent2D
        {
            Width = (uint)window.FramebufferSize.X,
            Height = (uint)window.FramebufferSize.Y
        };

        actualExtent.Width = Math.Clamp(actualExtent.Width, 
            capabilities.MinImageExtent.Width, 
            capabilities.MaxImageExtent.Width);
        actualExtent.Height = Math.Clamp(actualExtent.Height, 
            capabilities.MinImageExtent.Height, 
            capabilities.MaxImageExtent.Height);

        _logger.LogDebug("Calculated extent: {Width}x{Height} (window: {WinWidth}x{WinHeight}, clamped to [{MinW}-{MaxW}]x[{MinH}-{MaxH}])",
            actualExtent.Width, actualExtent.Height,
            window.FramebufferSize.X, window.FramebufferSize.Y,
            capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width,
            capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

        return actualExtent;
    }

    /// <summary>
    /// Retrieves swap chain images from the created swap chain.
    /// </summary>
    private void RetrieveSwapchainImages()
    {
        uint imageCount;
        _khrSwapchain!.GetSwapchainImages(_context.Device, _swapchain, &imageCount, null);

        _swapchainImages = new Image[imageCount];
        fixed (Image* pImages = _swapchainImages)
        {
            _khrSwapchain.GetSwapchainImages(_context.Device, _swapchain, &imageCount, pImages);
        }

        _logger.LogDebug("Retrieved {ImageCount} swap chain images", imageCount);
    }

    /// <summary>
    /// Creates image views for each swap chain image.
    /// </summary>
    private void CreateImageViews()
    {
        _swapchainImageViews = new ImageView[_swapchainImages.Length];

        for (int i = 0; i < _swapchainImages.Length; i++)
        {
            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = _swapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = _swapchainFormat,
                Components = new ComponentMapping
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity
                },
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
            var result = _vk.CreateImageView(_context.Device, &createInfo, null, &imageView);
            if (result != Result.Success)
            {
                _logger.LogError("Failed to create image view {Index}: {Result}", i, result);
                throw new Exception($"Failed to create image view: {result}");
            }

            _swapchainImageViews[i] = imageView;
        }

        _logger.LogDebug("Created {Count} image views", _swapchainImageViews.Length);
    }

    /// <summary>
    /// Finds the queue family that supports presentation to the surface.
    /// </summary>
    private uint? FindPresentQueueFamily()
    {
        if (!_vk.TryGetInstanceExtension(_context.Instance, out KhrSurface khrSurface))
        {
            return null;
        }

        uint queueFamilyCount = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(_context.PhysicalDevice, &queueFamilyCount, null);

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            Silk.NET.Core.Bool32 presentSupport = false;
            khrSurface.GetPhysicalDeviceSurfaceSupport(_context.PhysicalDevice, i, _context.Surface, &presentSupport);
            if (presentSupport)
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates all render passes from RenderPasses.Configurations array.
    /// Each render pass survives window resize since format doesn't change.
    /// </summary>
    /// <remarks>
    /// For each configuration:
    /// - Creates Vulkan RenderPass with color and optional depth attachments
    /// - Stores in dictionary keyed by bit flag (1 << bitPosition)
    /// - Stores clear values for use during rendering
    /// - Logs creation details for debugging
    /// </remarks>
    private void CreateRenderPasses()
    {
        var configs = RenderPasses.Configurations;
        
        for (int i = 0; i < configs.Length; i++)
        {
            var config = configs[i];
            
            var renderPass = CreateRenderPass(config);
            _renderPasses[i] = renderPass;
            _clearValues[i] = CreateClearValues(config);
            
            _logger.LogDebug("Created render pass '{Name}' (bit {Bit}): Format={Format}, Depth={Depth}", 
                config.Name,
                i,
                config.ColorFormat == Format.Undefined ? _swapchainFormat : config.ColorFormat,
                config.DepthFormat);
            
            // Track if any render pass needs depth
            if (config.DepthFormat != Format.Undefined)
            {
                _hasDepthAttachment = true;
                _depthFormat = config.DepthFormat;
            }
        }
    }

    /// <summary>
    /// Creates depth buffer resources if any render pass requires depth attachment.
    /// Allocates depth image, memory, and image view.
    /// </summary>
    private void CreateDepthResources()
    {
        if (!_hasDepthAttachment)
        {
            _logger.LogDebug("No depth attachment needed");
            return;
        }

        _logger.LogDebug("Creating depth resources: Format={Format}, Extent={Width}x{Height}", 
            _depthFormat, _swapchainExtent.Width, _swapchainExtent.Height);

        // Create depth image
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(_swapchainExtent.Width, _swapchainExtent.Height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Format = _depthFormat,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = ImageUsageFlags.DepthStencilAttachmentBit,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive
        };

        var result = _vk.CreateImage(_context.Device, &imageInfo, null, out _depthImage);
        if (result != Result.Success)
        {
            throw new Exception($"Failed to create depth image: {result}");
        }

        // Allocate memory for depth image
        _vk.GetImageMemoryRequirements(_context.Device, _depthImage, out var memRequirements);

        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };

        result = _vk.AllocateMemory(_context.Device, &allocInfo, null, out _depthImageMemory);
        if (result != Result.Success)
        {
            throw new Exception($"Failed to allocate depth image memory: {result}");
        }

        _vk.BindImageMemory(_context.Device, _depthImage, _depthImageMemory, 0);

        // Create depth image view
        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _depthImage,
            ViewType = ImageViewType.Type2D,
            Format = _depthFormat,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.DepthBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        result = _vk.CreateImageView(_context.Device, &viewInfo, null, out _depthImageView);
        if (result != Result.Success)
        {
            throw new Exception($"Failed to create depth image view: {result}");
        }

        _logger.LogDebug("Depth resources created successfully");
    }

    /// <summary>
    /// Finds a suitable memory type for the given requirements and properties.
    /// </summary>
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

        throw new Exception("Failed to find suitable memory type");
    }

    /// <summary>
    /// Creates a single render pass from configuration.
    /// </summary>
    /// <param name="config">Configuration specifying attachment formats, load/store operations, and clear values.</param>
    /// <returns>Native Vulkan RenderPass handle.</returns>
    /// <remarks>
    /// <para><strong>Attachments Created:</strong></para>
    /// <list type="bullet">
    /// <item>Color attachment: Uses swapchain format if config.ColorFormat is Format.Undefined</item>
    /// <item>Depth attachment: Optional, created only if config.DepthFormat != Format.Undefined</item>
    /// </list>
    /// 
    /// <para><strong>Subpass Configuration:</strong></para>
    /// Single graphics subpass with color and optional depth/stencil attachments.
    /// 
    /// <para><strong>Dependencies:</strong></para>
    /// External → Subpass 0 dependency for color and depth synchronization.
    /// </remarks>
    private RenderPass CreateRenderPass(RenderPassConfiguration config)
    {
        var attachments = new List<AttachmentDescription>();
        var colorReferences = new List<AttachmentReference>();
        AttachmentReference? depthReference = null;

        // Color attachment
        var colorFormat = config.ColorFormat == Format.Undefined ? _swapchainFormat : config.ColorFormat;
        attachments.Add(new AttachmentDescription
        {
            Format = colorFormat,
            Samples = config.SampleCount,
            LoadOp = config.ColorLoadOp,
            StoreOp = config.ColorStoreOp,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = config.ColorInitialLayout,
            FinalLayout = config.ColorFinalLayout
        });
        colorReferences.Add(new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        });

        // Depth attachment (if enabled)
        if (config.DepthFormat != Format.Undefined)
        {
            attachments.Add(new AttachmentDescription
            {
                Format = config.DepthFormat,
                Samples = config.SampleCount,
                LoadOp = config.DepthLoadOp,
                StoreOp = config.DepthStoreOp,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = config.DepthInitialLayout,
                FinalLayout = config.DepthFinalLayout
            });
            depthReference = new AttachmentReference
            {
                Attachment = 1,
                Layout = ImageLayout.DepthStencilAttachmentOptimal
            };
        }

        // Subpass
        var colorRef = colorReferences[0];
        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorRef
        };

        if (depthReference.HasValue)
        {
            var depthRef = depthReference.Value;
            subpass.PDepthStencilAttachment = &depthRef;
        }

        // Subpass dependency
        var dependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
        };

        // Create render pass
        var attachmentArray = attachments.ToArray();
        fixed (AttachmentDescription* pAttachments = attachmentArray)
        {
            var renderPassInfo = new RenderPassCreateInfo
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachmentArray.Length,
                PAttachments = pAttachments,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency
            };

            RenderPass renderPass;
            var result = _vk.CreateRenderPass(_context.Device, &renderPassInfo, null, &renderPass);
            if (result != Result.Success)
            {
                _logger.LogError("Failed to create render pass '{Name}': {Result}", config.Name, result);
                throw new Exception($"Failed to create render pass: {result}");
            }

            return renderPass;
        }
    }

    /// <summary>
    /// Creates clear values array for a render pass configuration.
    /// Clear values are used during vkCmdBeginRenderPass when attachments have LoadOp.Clear.
    /// </summary>
    /// <param name="config">Configuration containing clear color and depth values.</param>
    /// <returns>Array of ClearValue structures (one for color, one for depth if enabled).</returns>
    /// <remarks>
    /// Index 0: Color attachment clear value (RGBA from config.ClearColorValue)
    /// Index 1: Depth/stencil clear value (depth from config.ClearDepthValue, stencil always 0)
    /// </remarks>
    private ClearValue[] CreateClearValues(RenderPassConfiguration config)
    {
        var clearValues = new List<ClearValue>();

        // Color clear value
        clearValues.Add(new ClearValue
        {
            Color = new ClearColorValue
            {
                Float32_0 = config.ClearColorValue.X,
                Float32_1 = config.ClearColorValue.Y,
                Float32_2 = config.ClearColorValue.Z,
                Float32_3 = config.ClearColorValue.W
            }
        });

        // Depth clear value (if depth enabled)
        if (config.DepthFormat != Format.Undefined)
        {
            clearValues.Add(new ClearValue
            {
                DepthStencil = new ClearDepthStencilValue
                {
                    Depth = config.ClearDepthValue,
                    Stencil = 0
                }
            });
        }

        return clearValues.ToArray();
    }

    /// <summary>
    /// Creates framebuffers for all render passes.
    /// Creates N framebuffers per render pass, where N = number of swapchain images.
    /// </summary>
    /// <remarks>
    /// Each framebuffer binds a specific swapchain ImageView to a RenderPass.
    /// Framebuffers must be recreated when swapchain is recreated (window resize)
    /// because new ImageView handles are created.
    /// </remarks>
    private void CreateAllFramebuffers()
    {
        var configs = RenderPasses.Configurations;
        
        for (int passIndex = 0; passIndex < PassCount; passIndex++)
        {
            var renderPass = _renderPasses[passIndex];
            var config = configs[passIndex];
            var framebuffers = new Framebuffer[_swapchainImages.Length];
            
            for (int i = 0; i < _swapchainImages.Length; i++)
            {
                framebuffers[i] = CreateFramebuffer(renderPass, config, i);
            }
            
            _framebuffers[passIndex] = framebuffers;
        }
        
        _logger.LogDebug("Created framebuffers for {RenderPassCount} render passes ({ImageCount} per pass)", 
            PassCount, _swapchainImages.Length);
    }

    /// <summary>
    /// Creates a single framebuffer for a render pass and swapchain image index.
    /// Binds the specified ImageView to the RenderPass attachments.
    /// </summary>
    /// <param name="renderPass">The render pass this framebuffer will be used with.</param>
    /// <param name="config">Configuration for this render pass to determine attachment count.</param>
    /// <param name="imageIndex">Index of the swapchain image/imageview to bind.</param>
    /// <returns>Native Vulkan Framebuffer handle.</returns>
    /// <remarks>
    /// <para><strong>Attachment Configuration:</strong></para>
    /// Binds color attachment (swapchain ImageView) and optionally depth attachment based on config.DepthFormat.
    /// 
    /// <para><strong>Framebuffer Dimensions:</strong></para>
    /// Width/Height match swapchain extent. Layers = 1 (not using array textures).
    /// </remarks>
    private Framebuffer CreateFramebuffer(RenderPass renderPass, RenderPassConfiguration config, int imageIndex)
    {
        // Determine attachment count based on this specific render pass configuration
        var hasDepth = config.DepthFormat != Format.Undefined;
        var attachmentCount = hasDepth ? 2u : 1u;
        var attachments = stackalloc ImageView[(int)attachmentCount];
        attachments[0] = _swapchainImageViews[imageIndex];
        
        if (hasDepth)
        {
            attachments[1] = _depthImageView;
        }

        var framebufferInfo = new FramebufferCreateInfo
        {
            SType = StructureType.FramebufferCreateInfo,
            RenderPass = renderPass,
            AttachmentCount = attachmentCount,
            PAttachments = attachments,
            Width = _swapchainExtent.Width,
            Height = _swapchainExtent.Height,
            Layers = 1
        };

        Framebuffer framebuffer;
        var result = _vk.CreateFramebuffer(_context.Device, &framebufferInfo, null, &framebuffer);
        if (result != Result.Success)
        {
            _logger.LogError("Failed to create framebuffer: {Result}", result);
            throw new Exception($"Failed to create framebuffer: {result}");
        }

        return framebuffer;
    }

    /// <summary>
    /// Acquires the next available image from the swap chain.
    /// </summary>
    public uint AcquireNextImage(Semaphore imageAvailableSemaphore, out Result result)
    {
        uint imageIndex;
        result = _khrSwapchain!.AcquireNextImage(
            _context.Device,
            _swapchain,
            ulong.MaxValue, // Timeout (wait indefinitely)
            imageAvailableSemaphore,
            default, // Fence
            &imageIndex);

        if (result == Result.ErrorOutOfDateKhr)
        {
            _logger.LogWarning("Swap chain out of date during image acquisition");
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
        {
            _logger.LogError("Failed to acquire swap chain image: {Result}", result);
        }

        return imageIndex;
    }

    /// <summary>
    /// Presents the rendered image to the screen.
    /// </summary>
    public void Present(uint imageIndex, Semaphore renderFinishedSemaphore)
    {
        var swapchain = _swapchain;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &renderFinishedSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex,
            PResults = null
        };

        var result = _khrSwapchain!.QueuePresent(_context.PresentQueue, &presentInfo);

        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr)
        {
            _logger.LogWarning("Swap chain out of date or suboptimal during presentation");
            // Caller should handle recreation
        }
        else if (result != Result.Success)
        {
            _logger.LogError("Failed to present swap chain image: {Result}", result);
            throw new Exception($"Failed to present swap chain image: {result}");
        }
    }

    /// <summary>
    /// Recreates the swap chain (e.g., on window resize).
    /// Destroys and recreates swapchain, image views, and framebuffers while preserving render passes.
    /// </summary>
    /// <remarks>
    /// <para><strong>Destruction Order:</strong></para>
    /// <list type="number">
    /// <item>Framebuffers (all render passes)</item>
    /// <item>ImageViews</item>
    /// <item>Swapchain</item>
    /// </list>
    /// 
    /// <para><strong>Recreation Order:</strong></para>
    /// <list type="number">
    /// <item>Swapchain (new extent, new images)</item>
    /// <item>ImageViews (for new images)</item>
    /// <item>Framebuffers (binding new ImageViews to existing RenderPasses)</item>
    /// </list>
    /// 
    /// <para><strong>RenderPasses are NOT recreated</strong> - they survive resize because format doesn't change.</para>
    /// </remarks>
    public void Recreate()
    {
        _logger.LogDebug("Recreating swap chain");

        // Wait for device to finish operations
        _vk.DeviceWaitIdle(_context.Device);

        // Clean up old resources (framebuffers, image views, swapchain)
        CleanupSwapchain();

        // Clean up old depth resources (if they exist)
        if (_hasDepthAttachment)
        {
            if (_depthImageView.Handle != 0)
                _vk.DestroyImageView(_context.Device, _depthImageView, null);
            if (_depthImage.Handle != 0)
                _vk.DestroyImage(_context.Device, _depthImage, null);
            if (_depthImageMemory.Handle != 0)
                _vk.FreeMemory(_context.Device, _depthImageMemory, null);
        }

        // Recreate swap chain and image views
        CreateSwapchain();
        
        // If window is minimized (extent 0x0), CreateSwapchain returns early
        // Skip depth and framebuffer creation until window is restored
        if (_swapchainExtent.Width == 0 || _swapchainExtent.Height == 0)
        {
            _logger.LogDebug("Skipping depth and framebuffer recreation: Window is minimized");
            return;
        }
        
        // Recreate depth resources with new dimensions
        CreateDepthResources();
        
        // Recreate framebuffers for all render passes (render passes survive)
        CreateAllFramebuffers();

        _logger.LogDebug("Swap chain recreation complete");
    }

    /// <summary>
    /// Cleans up swap chain resources without destroying render passes.
    /// Called during resize (Recreate) and final disposal (Dispose).
    /// </summary>
    /// <remarks>
    /// <para><strong>Destroys (in order):</strong></para>
    /// <list type="number">
    /// <item>All framebuffers for all render passes</item>
    /// <item>All image views</item>
    /// <item>Swapchain handle</item>
    /// </list>
    /// 
    /// <para><strong>Does NOT destroy:</strong></para>
    /// <list type="bullet">
    /// <item>RenderPasses - destroyed only in Dispose()</item>
    /// <item>Swapchain Images - automatically destroyed when swapchain is destroyed</item>
    /// </list>
    /// </remarks>
    private void CleanupSwapchain()
    {
        // Destroy all framebuffers for all render passes
        foreach (var framebufferArray in _framebuffers)
        {
            if (framebufferArray != null)
            {
                foreach (var framebuffer in framebufferArray)
                {
                    if (framebuffer.Handle != 0)
                    {
                        _vk.DestroyFramebuffer(_context.Device, framebuffer, null);
                    }
                }
            }
        }
        Array.Clear(_framebuffers, 0, _framebuffers.Length);

        // Destroy image views
        foreach (var imageView in _swapchainImageViews)
        {
            if (imageView.Handle != 0)
            {
                _vk.DestroyImageView(_context.Device, imageView, null);
            }
        }
        _swapchainImageViews = [];

        // Destroy swap chain
        if (_swapchain.Handle != 0)
        {
            _khrSwapchain!.DestroySwapchain(_context.Device, _swapchain, null);
            _swapchain = default;
        }

        // Note: Swap chain images are owned by the swap chain and are automatically destroyed
        _swapchainImages = [];
    }

    /// <summary>
    /// Disposes the swap chain and all associated resources including render passes.
    /// This is final cleanup - unlike Recreate(), this destroys render passes too.
    /// </summary>
    /// <remarks>
    /// <para><strong>Destruction Order:</strong></para>
    /// <list type="number">
    /// <item>Wait for device idle (ensure no operations in flight)</item>
    /// <item>CleanupSwapchain() - destroys framebuffers, image views, swapchain</item>
    /// <item>Destroy all render passes</item>
    /// <item>Clear dictionaries (framebuffers, clear values)</item>
    /// </list>
    /// 
    /// <para>After disposal, this SwapChain instance cannot be used again.</para>
    /// </remarks>
    public void Dispose()
    {
        _logger.LogDebug("Disposing swap chain");

        // Wait for device to finish
        _vk.DeviceWaitIdle(_context.Device);

        // Clean up swapchain resources (framebuffers, image views, swapchain)
        CleanupSwapchain();

        // Destroy depth resources
        if (_hasDepthAttachment)
        {
            if (_depthImageView.Handle != 0)
                _vk.DestroyImageView(_context.Device, _depthImageView, null);
            if (_depthImage.Handle != 0)
                _vk.DestroyImage(_context.Device, _depthImage, null);
            if (_depthImageMemory.Handle != 0)
                _vk.FreeMemory(_context.Device, _depthImageMemory, null);
        }

        // Destroy render passes (survive resize but not disposal)
        foreach (var renderPass in _renderPasses)
        {
            if (renderPass.Handle != 0)
            {
                _vk.DestroyRenderPass(_context.Device, renderPass, null);
            }
        }
        Array.Clear(_renderPasses, 0, _renderPasses.Length);
        Array.Clear(_clearValues, 0, _clearValues.Length);

        _logger.LogDebug("Swap chain disposed");
    }
}
