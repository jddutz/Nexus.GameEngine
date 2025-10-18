using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Runtime.Settings;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Vulkan context implementation that initializes all Vulkan resources in the constructor.
/// </summary>
/// <remarks>
/// Initialization sequence (based on Vulkan tutorial):
/// [x] Create Instance
/// [x] Setup Debug Messenger
/// [x] Create Surface
/// [x] Pick Physical Device
/// [x] Create Logical Device
/// [x] Create Swap Chain
/// [x] Create Image Views
/// [x] Create Render Pass
/// [x] Create Framebuffers
/// [ ] Create Graphics Pipeline
/// [ ] Create Command Pool
/// 
/// All initialization happens in the constructor because the new startup sequence ensures
/// the window exists before VulkanContext is resolved from the DI container.
/// </remarks>
public unsafe class Context : IGraphicsContext
{
    private readonly ILogger _logger;
    private readonly IValidation? _validationLayers;
    private readonly VulkanSettings _vkSettings;

    public Context(
        IWindowService windowService,
        IOptions<ApplicationSettings> options,
        IOptions<VulkanSettings> vkSettings,
        ILoggerFactory loggerFactory,
        IValidation? validationLayers = null)
    {
        _logger = loggerFactory.CreateLogger(nameof(Context));
        _validationLayers = validationLayers;
        _vkSettings = vkSettings.Value;

        // Step 1: Get the window - it's guaranteed to exist at this point
        var window = windowService.GetWindow();
        if (window.VkSurface is null)
        {
            _logger.LogError("Window does not support Vulkan surfaces");
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        // Step 2: Load Vulkan API - provides access to all Vulkan functions
        _vulkanApi = Vk.GetApi();

        // Step 3: Create Vulkan instance - the connection between app and Vulkan library
        _logger.LogDebug("Creating Vulkan instance: App={AppName}, Engine={EngineName}, API=1.2",
            options.Value.General.ApplicationName, options.Value.General.EngineName);

        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(options.Value.General.ApplicationName),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi(options.Value.General.EngineName),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        // Get required extensions from the window system (platform-specific like Win32, X11, etc.)
        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);

        // Count total extensions needed
        var totalExtensions = glfwExtensionCount;
        nint debugUtilsNamePtr = 0;
        if (_validationLayers?.AreEnabled == true)
        {
            totalExtensions++;
            debugUtilsNamePtr = SilkMarshal.StringToPtr(ExtDebugUtils.ExtensionName);
            _logger.LogDebug("Adding debug utils extension for validation layers");
        }

        _logger.LogDebug("Total Vulkan extensions required: {ExtensionCount}", totalExtensions);

        // Build extension array
        var extensionsArray = stackalloc byte*[(int)totalExtensions];
        for (uint i = 0; i < glfwExtensionCount; i++)
        {
            extensionsArray[i] = glfwExtensions[i];
        }
        if (_validationLayers?.AreEnabled == true)
        {
            extensionsArray[glfwExtensionCount] = (byte*)debugUtilsNamePtr;
        }

        createInfo.EnabledExtensionCount = totalExtensions;
        createInfo.PpEnabledExtensionNames = extensionsArray;

        // Enable validation layers if available
        if (_validationLayers?.AreEnabled == true)
        {
            var layerNames = _validationLayers.LayerNames;
            var layerNamePtrs = stackalloc nint[layerNames.Length];
            var layerNameBytePtrs = stackalloc byte*[layerNames.Length];

            for (int i = 0; i < layerNames.Length; i++)
            {
                layerNamePtrs[i] = SilkMarshal.StringToPtr(layerNames[i]);
                layerNameBytePtrs[i] = (byte*)layerNamePtrs[i];
            }

            createInfo.EnabledLayerCount = (uint)layerNames.Length;
            createInfo.PpEnabledLayerNames = layerNameBytePtrs;

            var result = _vulkanApi.CreateInstance(in createInfo, null, out _instance);

            // Cleanup layer name pointers
            for (int i = 0; i < layerNames.Length; i++)
            {
                SilkMarshal.Free(layerNamePtrs[i]);
            }

            if (result != Result.Success)
            {
                _logger.LogError("Failed to create Vulkan instance: {Result}", result);
                throw new Exception($"Failed to create Vulkan instance: {result}");
            }
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            var result = _vulkanApi.CreateInstance(in createInfo, null, out _instance);
            if (result != Result.Success)
            {
                _logger.LogError("Failed to create Vulkan instance: {Result}", result);
                throw new Exception($"Failed to create Vulkan instance: {result}");
            }
        }

        _logger.LogDebug("Vulkan instance created (Handle: {Handle})", _instance.Handle);

        // Cleanup debug utils extension name if added
        if (debugUtilsNamePtr != 0)
        {
            SilkMarshal.Free(debugUtilsNamePtr);
        }

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);

        // Initialize validation layers (always call to provide debugger feedback)
        _validationLayers?.Initialize(_vulkanApi, _instance);

        // Step 4: Create surface - platform-agnostic abstraction for rendering to the window
        Surface = CreateSurface(window);
        _logger.LogDebug("Surface created (Handle: {Handle})", Surface.Handle);

        // Step 5: Select physical device - choose which GPU to use for rendering
        PhysicalDevice = SelectPhysicalDevice();

        // Step 6: Create logical device and queues - the interface for submitting work to the GPU
        var (device, graphicsQueue, presentQueue) = CreateDeviceAndQueues();
        Device = device;
        GraphicsQueue = graphicsQueue;
        PresentQueue = presentQueue;
        _logger.LogDebug("Logical device created. Device={DeviceHandle}, Graphics={GraphicsHandle}, Present={PresentHandle}",
            Device.Handle, GraphicsQueue.Handle, PresentQueue.Handle);

        _logger.LogDebug("Vulkan context initialization complete");
    }

    /// <summary>
    /// Gets the Vulkan API object that provides access to all Vulkan functions.
    /// This is the main entry point for calling Vulkan API methods.
    /// </summary>
    private Vk _vulkanApi;
    public Vk VulkanApi => _vulkanApi;

    /// <summary>
    /// Gets the Vulkan instance, which is the connection between the application and the Vulkan library.
    /// The instance is used to query physical devices and create surfaces.
    /// </summary>
    private Instance _instance;
    public Instance Instance => _instance;

    /// <summary>
    /// Gets the window surface that Vulkan will render to.
    /// This is an abstraction of the native platform window that allows Vulkan to present rendered images.
    /// Created from the window's VkSurface during initialization.
    /// </summary>
    public SurfaceKHR Surface { get; private init; }

    /// <summary>
    /// Gets the physical device (GPU) selected for rendering.
    /// Represents the actual hardware device that will execute Vulkan commands.
    /// Used to query device capabilities and create the logical device.
    /// </summary>
    public PhysicalDevice PhysicalDevice { get; private init; }

    /// <summary>
    /// Gets the logical device, which is the application's interface to the physical device.
    /// All Vulkan operations are performed through this logical device.
    /// Created with specific queues and features enabled based on application needs.
    /// </summary>
    public Device Device { get; private init; }

    /// <summary>
    /// Gets the graphics queue handle used for submitting rendering commands.
    /// Graphics queues support drawing operations and can execute graphics pipelines.
    /// Commands submitted to this queue are executed by the GPU.
    /// </summary>
    public Queue GraphicsQueue { get; private init; }

    /// <summary>
    /// Gets the present queue handle used for presenting rendered images to the surface.
    /// This queue supports presentation operations to display images on screen.
    /// May be the same queue as GraphicsQueue if the device supports both operations on one queue.
    /// </summary>
    public Queue PresentQueue { get; private init; }

    /// <summary>
    /// Gets whether the Vulkan context has been initialized.
    /// Always returns true since initialization occurs in the constructor.
    /// </summary>
    public bool IsInitialized => true;

    /// <summary>
    /// Creates a Vulkan surface from the window that will be used as the render target.
    /// The surface is a platform-agnostic abstraction over the native window system.
    /// </summary>
    /// <param name="window">The window to create the surface from. Must have VkSurface support.</param>
    /// <returns>The created Vulkan surface handle.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the window doesn't support Vulkan surfaces.</exception>
    private SurfaceKHR CreateSurface(IWindow window)
    {
        // Get the platform-specific Vulkan surface from the window
        var vkSurface = window.VkSurface;
        if (vkSurface is null)
        {
            var errorMessage = "Window.VkSurface is null. Window is not configured for Vulkan. "
            + "Ensure the window API is configured for Vulkan before constructing VulkanContext.";

            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        // Create the Vulkan surface handle - connects Vulkan to native windowing system (Win32, X11, Wayland, etc.)
        var surfaceKhr = vkSurface.Create<AllocationCallbacks>(Instance.ToHandle(), null).ToSurface();
        _logger.LogDebug("Surface KHR created from window (Handle: {Handle})", surfaceKhr.Handle);
        return surfaceKhr;
    }

    /// <summary>
    /// Selects a physical device (GPU) to use for rendering.
    /// Enumerates all available Vulkan-capable GPUs and selects one based on suitability.
    /// </summary>
    /// <returns>The selected physical device handle.</returns>
    /// <exception cref="Exception">Thrown if no Vulkan-capable GPUs are found.</exception>
    /// <remarks>
    /// Currently selects the first available device. Future implementation should score
    /// devices based on features, memory, and queue family support.
    /// </remarks>
    private PhysicalDevice SelectPhysicalDevice()
    {
        // First call: Get the count of available physical devices (GPUs)
        uint deviceCount = 0;
        var result = VulkanApi.EnumeratePhysicalDevices(Instance, &deviceCount, null);

        if (deviceCount == 0)
        {
            _logger.LogError("No Vulkan-capable GPUs found");
            throw new Exception("Failed to find GPUs with Vulkan support");
        }

        // Second call: Retrieve the actual device handles
        var devices = stackalloc PhysicalDevice[(int)deviceCount];
        result = VulkanApi.EnumeratePhysicalDevices(Instance, &deviceCount, devices);
        _logger.LogDebug("Found {DeviceCount} Vulkan-capable GPU(s)", deviceCount);

        // Log all available devices
        for (uint i = 0; i < deviceCount; i++)
        {
            PhysicalDeviceProperties props;
            VulkanApi.GetPhysicalDeviceProperties(devices[i], &props);
            var name = Marshal.PtrToStringAnsi((nint)props.DeviceName);
            _logger.LogDebug("- GPU {Index}: {Name} (Type: {Type})", i, name, props.DeviceType);
        }

        // Score all suitable devices
        var suitableDevices = new List<(PhysicalDevice device, int score)>();

        for (uint i = 0; i < deviceCount; i++)
        {
            var device = devices[i];

            // Check if device meets requirements
            if (!IsDeviceSuitable(device, out int score))
            {
                PhysicalDeviceProperties props;
                VulkanApi.GetPhysicalDeviceProperties(device, &props);
                var name = Marshal.PtrToStringAnsi((nint)props.DeviceName);
                _logger.LogDebug("GPU {Index} ({Name}) is not suitable", i, name);
                continue;
            }

            suitableDevices.Add((device, score));

            PhysicalDeviceProperties properties;
            VulkanApi.GetPhysicalDeviceProperties(device, &properties);
            var deviceName = Marshal.PtrToStringAnsi((nint)properties.DeviceName);
            _logger.LogDebug("GPU {Index} ({Name}) is suitable (score: {Score})", i, deviceName, score);
        }

        if (suitableDevices.Count == 0)
        {
            _logger.LogError("No suitable GPU found that meets requirements");
            throw new Exception("Failed to find a suitable GPU");
        }

        // Select highest-scored device
        var selected = suitableDevices.OrderByDescending(d => d.score).First();

        PhysicalDeviceProperties selectedProps;
        VulkanApi.GetPhysicalDeviceProperties(selected.device, &selectedProps);
        var selectedName = Marshal.PtrToStringAnsi((nint)selectedProps.DeviceName);
        _logger.LogInformation("Selected GPU: {Name} (Score: {Score})", selectedName, selected.score);

        return selected.device;
    }

    private bool IsDeviceSuitable(PhysicalDevice device, out int score)
    {
        score = 0;

        // 1. Check queue families
        if (!HasRequiredQueueFamilies(device))
        {
            return false;
        }

        // 2. Check required extensions (from GraphicsSettings)
        if (!HasRequiredExtensions(device))
        {
            return false;
        }

        // 3. Check swap chain support
        var swapChainSupport = QuerySwapChainSupportInternal(device);
        if (!swapChainSupport.IsAdequate)
        {
            return false;
        }

        // 4. Score device based on capabilities
        score = ScoreDevice(device);

        return true;
    }

    private bool HasRequiredQueueFamilies(PhysicalDevice device)
    {
        uint queueFamilyCount = 0;
        VulkanApi.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

        var queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        VulkanApi.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies);

        if (!VulkanApi.TryGetInstanceExtension(Instance, out KhrSurface khrSurface))
        {
            return false;
        }

        bool hasGraphics = false;
        bool hasPresent = false;

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if ((queueFamilies[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
            {
                hasGraphics = true;
            }

            Bool32 presentSupport = false;
            khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, Surface, &presentSupport);
            if (presentSupport)
            {
                hasPresent = true;
            }

            if (hasGraphics && hasPresent)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasRequiredExtensions(PhysicalDevice device)
    {
        uint extensionCount;
        VulkanApi.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, null);

        var availableExtensions = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* pExtensions = availableExtensions)
        {
            VulkanApi.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, pExtensions);
        }

        var availableExtensionNames = availableExtensions
            .Select(ext => Marshal.PtrToStringAnsi((nint)ext.ExtensionName))
            .ToHashSet();

        // Check all required extensions from settings
        foreach (var required in _vkSettings.RequiredDeviceExtensions)
        {
            if (!availableExtensionNames.Contains(required))
            {
                _logger.LogDebug("Device missing required extension: {Extension}", required);
                return false;
            }
        }

        return true;
    }

    private int ScoreDevice(PhysicalDevice device)
    {
        PhysicalDeviceProperties props;
        VulkanApi.GetPhysicalDeviceProperties(device, &props);

        int score = 0;

        // Prefer discrete GPU if configured
        if (_vkSettings.PreferDiscreteGpu && props.DeviceType == PhysicalDeviceType.DiscreteGpu)
        {
            score += 1000;
        }

        // Higher maximum texture size is better
        score += (int)props.Limits.MaxImageDimension2D;

        // Bonus points for optional extensions
        uint extensionCount;
        VulkanApi.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, null);
        var availableExtensions = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* pExtensions = availableExtensions)
        {
            VulkanApi.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, pExtensions);
        }

        var availableExtensionNames = availableExtensions
            .Select(ext => Marshal.PtrToStringAnsi((nint)ext.ExtensionName))
            .ToHashSet();

        foreach (var optional in _vkSettings.OptionalDeviceExtensions)
        {
            if (availableExtensionNames.Contains(optional))
            {
                score += 100;
            }
        }

        return score;
    }

    /// <summary>
    /// Creates the logical device and retrieves queue handles for graphics and presentation.
    /// This involves finding suitable queue families, creating the device with required extensions,
    /// and obtaining handles to the graphics and present queues.
    /// </summary>
    /// <returns>A tuple containing the logical device, graphics queue, and present queue.</returns>
    /// <exception cref="Exception">Thrown if suitable queue families cannot be found or device creation fails.</exception>
    /// <remarks>
    /// Queue families are groups of queues with similar capabilities. We need:
    /// - A graphics queue family for rendering commands
    /// - A present queue family for displaying images to the surface
    /// These may be the same family on some hardware.
    /// </remarks>
    private (Device, Queue, Queue) CreateDeviceAndQueues()
    {
        uint queueFamilyCount = 0;
        VulkanApi.GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, &queueFamilyCount, null);
        _logger.LogDebug("Query returned {QueueFamilyCount} queue families available", queueFamilyCount);

        var queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        VulkanApi.GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, &queueFamilyCount, queueFamilies);

        _logger.LogDebug("Physical device has {QueueFamilyCount} queue families", queueFamilyCount);
        for (uint i = 0; i < queueFamilyCount; i++)
        {
            _logger.LogDebug("- Family {Index}: {Count} queue(s), Flags={Flags}",
                i, queueFamilies[i].QueueCount, queueFamilies[i].QueueFlags);
        }

        uint? graphicsFamily = null;
        uint? presentFamily = null;

        if (!VulkanApi.TryGetInstanceExtension(Instance, out KhrSurface khrSurface))
        {
            _logger.LogError("KHR_surface extension not available");
            throw new Exception("KHR_surface extension not available");
        }

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if ((queueFamilies[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
            {
                graphicsFamily = i;
                _logger.LogDebug("Family {Index} supports graphics operations", i);
            }

            Bool32 presentSupport = false;
            khrSurface.GetPhysicalDeviceSurfaceSupport(PhysicalDevice, i, Surface, &presentSupport);
            if (presentSupport)
            {
                presentFamily = i;
                _logger.LogDebug("Family {Index} supports presentation to surface", i);
            }

            if (graphicsFamily.HasValue && presentFamily.HasValue)
            {
                _logger.LogDebug("Found all required queue families, stopping search");
                break;
            }
        }

        if (!graphicsFamily.HasValue || !presentFamily.HasValue)
        {
            _logger.LogError("Failed to find suitable queue families. Graphics: {Graphics}, Present: {Present}",
                graphicsFamily, presentFamily);
            throw new Exception("Failed to find suitable queue families");
        }

        _logger.LogDebug("Using queue families - Graphics: {GraphicsFamily}, Present: {PresentFamily}",
            graphicsFamily.Value, presentFamily.Value);

        var uniqueQueueFamilies = graphicsFamily.Value == presentFamily.Value
            ? new[] { graphicsFamily.Value }
            : new[] { graphicsFamily.Value, presentFamily.Value };

        _logger.LogDebug("Creating {QueueCount} unique queue(s). Same family for both: {IsSame}",
            uniqueQueueFamilies.Length, graphicsFamily.Value == presentFamily.Value);

        var queueCreateInfos = stackalloc DeviceQueueCreateInfo[uniqueQueueFamilies.Length];
        float queuePriority = 1.0f;

        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
            _logger.LogDebug("Queue create info {Index}: Family={Family}, Count=1, Priority={Priority}",
                i, uniqueQueueFamilies[i], queuePriority);
        }

        var deviceFeatures = new PhysicalDeviceFeatures();
        _logger.LogDebug("Device features: none enabled (using defaults)");

        var extensionName = stackalloc byte*[1];
        extensionName[0] = (byte*)SilkMarshal.StringToPtr(KhrSwapchain.ExtensionName);

        var createInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,
            PEnabledFeatures = &deviceFeatures,
            EnabledExtensionCount = 1,
            PpEnabledExtensionNames = extensionName
        };

        _logger.LogDebug("Device create info configured: QueueFamilies={QueueCount}, Extensions={ExtCount}",
            uniqueQueueFamilies.Length, 1);

        // Creating the logical device using selected physical device and queues
        Device device;
        var result = VulkanApi.CreateDevice(PhysicalDevice, &createInfo, null, &device);

        SilkMarshal.Free((nint)extensionName[0]);

        if (result != Result.Success)
        {
            _logger.LogError("Failed to create logical device: {Result}", result);
            throw new Exception($"Failed to create logical device: {result}");
        }
        _logger.LogDebug("Logical device created successfully (Handle: {Handle})", device.Handle);

        Queue graphicsQueue, presentQueue;
        VulkanApi.GetDeviceQueue(device, graphicsFamily.Value, 0, &graphicsQueue);
        _logger.LogDebug("Graphics queue retrieved: Family={Family}, Index=0, Handle={Handle}",
            graphicsFamily.Value, graphicsQueue.Handle);

        VulkanApi.GetDeviceQueue(device, presentFamily.Value, 0, &presentQueue);
        _logger.LogDebug("Present queue retrieved: Family={Family}, Index=0, Handle={Handle}",
            presentFamily.Value, presentQueue.Handle);

        return (device, graphicsQueue, presentQueue);
    }

    /// <summary>
    /// Disposes the Vulkan context and cleans up all Vulkan resources.
    /// Resources are destroyed in reverse order of creation to ensure proper cleanup.
    /// </summary>
    /// <remarks>
    /// Vulkan requires explicit cleanup of all created objects. The cleanup order is critical:
    /// 1. Wait for device operations to complete
    /// 2. Destroy logical device (implicitly destroys queues)
    /// 3. Destroy surface
    /// 4. Destroy instance
    /// 5. Unload Vulkan library
    /// </remarks>
    public void Dispose()
    {
        // Clean up Vulkan resources in reverse order of creation

        // 1. Wait for device to finish any pending operations
        // Critical: ensures no commands are executing when we destroy resources
        if (Device.Handle != 0)
        {
            VulkanApi.DeviceWaitIdle(Device);
        }

        // 2. Destroy logical device (also implicitly destroys all associated queues)
        if (Device.Handle != 0)
        {
            VulkanApi.DestroyDevice(Device, null);
        }

        // 3. Destroy surface (must be before destroying the instance)
        if (Surface.Handle != 0 && VulkanApi.TryGetInstanceExtension(Instance, out KhrSurface khrSurface))
        {
            khrSurface.DestroySurface(Instance, Surface, null);
        }

        // 4. Destroy instance (created first, destroyed last)
        if (Instance.Handle != 0)
        {
            VulkanApi.DestroyInstance(Instance, null);
        }

        // 5. Unload Vulkan library and free function pointers
        VulkanApi.Dispose();

        _logger.LogDebug("Vulkan context disposed");
    }

    /// <summary>
    /// Public API for Swapchain to query swap chain capabilities.
    /// </summary>
    public SwapChainSupportDetails QuerySwapChainSupport()
    {
        return QuerySwapChainSupportInternal(PhysicalDevice);
    }

    /// <summary>
    /// Finds a queue family index that supports the specified queue flags.
    /// </summary>
    public uint? FindQueueFamily(QueueFlags flags)
    {
        uint queueFamilyCount = 0;
        VulkanApi.GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, &queueFamilyCount, null);

        var queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        VulkanApi.GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, &queueFamilyCount, queueFamilies);

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if ((queueFamilies[i].QueueFlags & flags) == flags)
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    /// Internal method used during device selection to query swap chain support.
    /// </summary>
    private SwapChainSupportDetails QuerySwapChainSupportInternal(PhysicalDevice device)
    {
        if (!VulkanApi.TryGetInstanceExtension(Instance, out KhrSurface khrSurface))
        {
            throw new Exception("KHR_surface extension not available");
        }

        // Query capabilities
        SurfaceCapabilitiesKHR capabilities;
        khrSurface.GetPhysicalDeviceSurfaceCapabilities(device, Surface, &capabilities);

        // Query formats
        uint formatCount;
        khrSurface.GetPhysicalDeviceSurfaceFormats(device, Surface, &formatCount, null);
        var formats = new SurfaceFormatKHR[formatCount];
        if (formatCount > 0)
        {
            fixed (SurfaceFormatKHR* pFormats = formats)
            {
                khrSurface.GetPhysicalDeviceSurfaceFormats(device, Surface, &formatCount, pFormats);
            }
        }

        // Query present modes
        uint presentModeCount;
        khrSurface.GetPhysicalDeviceSurfacePresentModes(device, Surface, &presentModeCount, null);
        var presentModes = new PresentModeKHR[presentModeCount];
        if (presentModeCount > 0)
        {
            fixed (PresentModeKHR* pPresentModes = presentModes)
            {
                khrSurface.GetPhysicalDeviceSurfacePresentModes(device, Surface, &presentModeCount, pPresentModes);
            }
        }

        return new SwapChainSupportDetails
        {
            Capabilities = capabilities,
            Formats = formats,
            PresentModes = presentModes
        };
    }
}