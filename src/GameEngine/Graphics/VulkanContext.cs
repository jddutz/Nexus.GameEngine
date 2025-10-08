using System.Diagnostics;
using System.Runtime.InteropServices;
using Nexus.GameEngine.Runtime;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages the core Vulkan context including instance, device, and associated resources.
/// This is the central hub for all Vulkan operations in the engine.
/// Registered as a singleton service with lazy initialization on first access.
/// </summary>
public unsafe class VulkanContext : IDisposable
{
    private readonly IWindowService _windowService;
    private readonly Vk _vk;
    private Instance _instance;
    private KhrSurface? _surfaceExtension;
    private SurfaceKHR _surface;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _graphicsQueue;
    private Queue _presentQueue;
    private uint _graphicsQueueFamilyIndex;
    private uint _presentQueueFamilyIndex;
    private bool _initialized = false;
    private bool _disposed = false;
    private readonly object _initLock = new object();

    public VulkanContext(IWindowService windowService)
    {
        _windowService = windowService;
        _vk = Vk.GetApi();
    }

    /// <summary>
    /// Ensures the Vulkan context is fully initialized.
    /// Called automatically on first access to any Vulkan resource.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            // Step 1: Create Vulkan Instance
            CreateInstance();

            // Step 2: Get window handle and create surface
            var window = _windowService.GetWindow();
            var windowHandle = window.Native!.Win32!.Value.Hwnd;
            CreateSurface(windowHandle, 0);

            // Step 3: Select physical device (GPU) with surface support
            SelectPhysicalDevice();

            // Step 4: Create logical device and queues
            CreateDeviceAndQueues();

            _initialized = true;
        }
    }

    private void CreateInstance()
    {
        // Application info - describes our application to the Vulkan driver
        var appInfo = new ApplicationInfo()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Nexus Game Engine"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("Nexus Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12 // Use Vulkan 1.2
        };

        // Required extensions for surface creation
        var extensions = GetRequiredExtensions();

        // Instance creation info
        var createInfo = new InstanceCreateInfo()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,

            // TODO: Add validation layers for debug builds
            EnabledLayerCount = 0,
            PpEnabledLayerNames = null,

            // Enable required extensions
            EnabledExtensionCount = (uint)extensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions)
        };

        // Create the instance
        var result = _vk.CreateInstance(in createInfo, null, out _instance);

        // Clean up temporary strings
        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create Vulkan instance: {result}");
        }
    }

    /// <summary>
    /// The main Vulkan API interface for all Vulkan operations.
    /// </summary>
    public Vk Vk
    {
        get
        {
            EnsureInitialized();
            return _vk;
        }
    }

    /// <summary>
    /// The Vulkan instance - represents a connection to the Vulkan library.
    /// This is the root object from which all other Vulkan objects are created.
    /// </summary>
    public Instance Instance
    {
        get
        {
            EnsureInitialized();
            return _instance;
        }
    }

    /// <summary>
    /// The selected physical device (GPU) for rendering operations.
    /// </summary>
    public PhysicalDevice PhysicalDevice
    {
        get
        {
            EnsureInitialized();
            return _physicalDevice;
        }
    }

    /// <summary>
    /// The logical device for issuing commands to the GPU.
    /// </summary>
    public Device Device
    {
        get
        {
            EnsureInitialized();
            return _device;
        }
    }

    /// <summary>
    /// Queue for graphics operations (drawing, compute).
    /// </summary>
    public Queue GraphicsQueue
    {
        get
        {
            EnsureInitialized();
            return _graphicsQueue;
        }
    }

    /// <summary>
    /// Queue for presentation operations (displaying to screen).
    /// </summary>
    public Queue PresentQueue
    {
        get
        {
            EnsureInitialized();
            return _presentQueue;
        }
    }

    /// <summary>
    /// The Vulkan surface for presenting rendered images to the window.
    /// </summary>
    public SurfaceKHR Surface
    {
        get
        {
            EnsureInitialized();
            return _surface;
        }
    }

    private static string[] GetRequiredExtensions()
    {
        // Basic extensions needed for surface creation
        var extensions = new List<string>
        {
            KhrSurface.ExtensionName,
        };

        // Add platform-specific surface extension
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            extensions.Add("VK_KHR_win32_surface");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            extensions.Add("VK_KHR_xcb_surface");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            extensions.Add("VK_MVK_macos_surface");
        }

        return extensions.ToArray();
    }

    private unsafe void CreateSurface(nint windowHandle, nint displayHandle)
    {
        // Get surface extension
        if (!_vk.TryGetInstanceExtension(_instance, out _surfaceExtension))
        {
            throw new InvalidOperationException("VK_KHR_surface extension not available");
        }

        // For now, create a minimal surface - we'll improve this once basic rendering works
        // This is a placeholder that will be replaced with proper platform surface creation
        _surface = new SurfaceKHR(1); // Dummy surface for now

        // TODO: Implement proper platform-specific surface creation
        // TODO: Add support for Linux (XCB) and macOS surfaces
    }

    private unsafe void SelectPhysicalDevice()
    {
        // Get number of physical devices
        uint deviceCount = 0;
        _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, null);

        if (deviceCount == 0)
        {
            throw new InvalidOperationException("No Vulkan-compatible GPUs found!");
        }

        // Get all physical devices
        var devices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* devicesPtr = devices)
        {
            _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, devicesPtr);
        }

        // For now, just pick the first discrete GPU we find, or the first GPU if no discrete GPU
        PhysicalDevice? discreteGpu = null;
        PhysicalDevice? firstGpu = null;

        foreach (var device in devices)
        {
            _vk.GetPhysicalDeviceProperties(device, out var properties);

            if (firstGpu == null)
                firstGpu = device;

            if (properties.DeviceType == PhysicalDeviceType.DiscreteGpu)
            {
                discreteGpu = device;
                break;
            }
        }

        _physicalDevice = discreteGpu ?? firstGpu ?? throw new InvalidOperationException("No suitable GPU found");

        // Find queue families that support graphics
        FindQueueFamilies();
    }

    private unsafe void CreateDeviceAndQueues()
    {
        // Create device queues - for now, assume graphics and present are the same queue
        var queuePriority = 1.0f;
        var queueCreateInfo = new DeviceQueueCreateInfo()
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = _graphicsQueueFamilyIndex,
            QueueCount = 1,
            PQueuePriorities = &queuePriority
        };

        // Required device extensions
        var deviceExtensions = new[] { KhrSwapchain.ExtensionName };

        var deviceCreateInfo = new DeviceCreateInfo()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,
            EnabledExtensionCount = (uint)deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions)
        };

        var result = _vk.CreateDevice(_physicalDevice, in deviceCreateInfo, null, out _device);
        SilkMarshal.Free((nint)deviceCreateInfo.PpEnabledExtensionNames);

        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create logical device: {result}");
        }

        // Get queue handles
        _vk.GetDeviceQueue(_device, _graphicsQueueFamilyIndex, 0, out _graphicsQueue);
        _vk.GetDeviceQueue(_device, _presentQueueFamilyIndex, 0, out _presentQueue);
    }

    private unsafe void FindQueueFamilies()
    {
        uint queueFamilyCount = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, queueFamiliesPtr);
        }

        // Find a queue family that supports graphics
        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                _graphicsQueueFamilyIndex = i;
                _presentQueueFamilyIndex = i; // For now, assume same queue for both
                return;
            }
        }

        throw new InvalidOperationException("No graphics queue family found!");
    }

    public unsafe void Dispose()
    {
        if (!_disposed)
        {
            // Clean up in reverse order of creation
            if (_device.Handle != 0)
            {
                _vk.DestroyDevice(_device, null);
            }

            if (_instance.Handle != 0)
            {
                _vk.DestroyInstance(_instance, null);
            }

            _vk.Dispose();
            _disposed = true;
        }
    }
}
