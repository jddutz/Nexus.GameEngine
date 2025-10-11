using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages Vulkan validation layers and debug messenger for runtime error detection and diagnostics.
/// Validation layers can be enabled in both Debug and Release builds via configuration.
/// Requires Vulkan SDK to be installed on the target machine.
/// </summary>
/// <remarks>
/// Two-phase initialization:
/// 1. Construction: Detects available validation layers, provides layer names for instance creation
/// 2. Initialize(): Creates debug messenger after Vulkan instance is created
/// 
/// Configuration is runtime-based (not compile-time) to support:
/// - Debugging engine issues in Release builds
/// - Platform-specific GPU/driver diagnostics
/// - Performance profiling with validation overhead measurement
/// - Optional validation in shipped applications
/// </remarks>
public interface IVkValidationLayers : IDisposable
{
    /// <summary>
    /// Gets whether validation layers are enabled based on configuration.
    /// Available immediately after construction.
    /// </summary>
    /// <remarks>
    /// When false:
    /// - LayerNames returns empty array
    /// - Initialize() is a no-op
    /// - DebugMessenger will be invalid handle
    /// </remarks>
    bool AreEnabled { get; }

    /// <summary>
    /// Gets the validation layer names to enable during Vulkan instance creation.
    /// Available immediately after construction.
    /// Returns empty array if validation is disabled or no layers are available.
    /// </summary>
    /// <remarks>
    /// Layer selection priority:
    /// 1. VK_LAYER_KHRONOS_validation (modern unified layer)
    /// 2. VK_LAYER_LUNARG_standard_validation (legacy)
    /// 3. Individual layers (very old SDK versions)
    /// </remarks>
    string[] LayerNames { get; }

    /// <summary>
    /// Initializes the debug messenger after the Vulkan instance is created.
    /// Must be called by VkContext after instance creation if AreEnabled is true.
    /// </summary>
    /// <param name="vk">The Vulkan API instance</param>
    /// <param name="instance">The Vulkan instance handle with validation layers enabled</param>
    /// <remarks>
    /// Creates the debug messenger and registers callback for validation messages.
    /// Messages are routed to ILogger with appropriate severity levels:
    /// - ERROR → LogError
    /// - WARNING → LogWarning
    /// - INFO → LogInformation
    /// - VERBOSE → LogDebug
    /// </remarks>
    void Initialize(Vk vk, Instance instance);

    /// <summary>
    /// Gets the debug messenger handle.
    /// Only valid after Initialize() has been called successfully.
    /// </summary>
    DebugUtilsMessengerEXT DebugMessenger { get; }

    /// <summary>
    /// Gets whether the debug messenger has been initialized.
    /// True if Initialize() was called and debug messenger was created successfully.
    /// </summary>
    bool IsInitialized { get; }
}