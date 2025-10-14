using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Provides access to the Vulkan API and core resources.
/// Lazily initializes Vulkan resources on first property access after window creation.
/// </summary>
/// <remarks>
/// This context manages the lifecycle of core Vulkan objects:
/// - Vulkan API instance (Vk)
/// - Vulkan Instance
/// - Surface (created from window)
/// - Physical Device (GPU selection)
/// - Logical Device
/// - Graphics and Present Queues
/// 
/// All properties trigger lazy initialization if not already initialized.
/// Thread-safe initialization using double-check locking pattern.
/// </remarks>
public interface IGraphicsContext : IDisposable
{
    /// <summary>
    /// Gets the Vulkan API instance for making Vulkan calls.
    /// </summary>
    Vk VulkanApi { get; }

    /// <summary>
    /// Gets the Vulkan instance handle.
    /// </summary>
    Instance Instance { get; }

    /// <summary>
    /// Gets the window surface for rendering.
    /// </summary>
    SurfaceKHR Surface { get; }

    /// <summary>
    /// Gets the selected physical device (GPU).
    /// </summary>
    PhysicalDevice PhysicalDevice { get; }

    /// <summary>
    /// Gets the logical device handle.
    /// </summary>
    Device Device { get; }

    /// <summary>
    /// Gets the graphics queue for rendering commands.
    /// </summary>
    Queue GraphicsQueue { get; }

    /// <summary>
    /// Gets the present queue for displaying rendered images.
    /// </summary>
    Queue PresentQueue { get; }

    /// <summary>
    /// Gets whether the Vulkan context has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Queries swap chain support details for the selected physical device.
    /// Returns surface capabilities, supported formats, and present modes.
    /// </summary>
    SwapChainSupportDetails QuerySwapChainSupport();

    /// <summary>
    /// Finds a queue family index that supports the specified queue flags.
    /// Returns null if no suitable queue family is found.
    /// </summary>
    uint? FindQueueFamily(QueueFlags flags);
}
