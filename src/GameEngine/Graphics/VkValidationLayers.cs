using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages Vulkan validation layers and debug messenger for runtime error detection and diagnostics.
/// </summary>
public unsafe class VkValidationLayers : IVkValidationLayers
{
    private readonly ILogger logger;
    private readonly VkSettings settings;
    private readonly string[] _layerNames;

    private Vk? _vk;
    private Instance _instance;
    private ExtDebugUtils? _debugUtils;
    private DebugUtilsMessengerEXT _debugMessenger;
    private bool _isInitialized;

    public VkValidationLayers(ILoggerFactory loggerFactory, IOptions<VkSettings> options)
    {
        logger = loggerFactory.CreateLogger(nameof(VkValidationLayers));
        settings = options.Value;
        _layerNames = DetectValidationLayers();
    }

    // Validation layer priority list: try modern first, fall back to legacy
    private static readonly string[][] ValidationLayerPriority =
    [
        ["VK_LAYER_KHRONOS_validation"],              // Modern unified layer
        ["VK_LAYER_LUNARG_standard_validation"],      // Legacy standard validation
        [                                              // Very old individual layers
            "VK_LAYER_GOOGLE_threading",
            "VK_LAYER_LUNARG_parameter_validation",
            "VK_LAYER_LUNARG_object_tracker",
            "VK_LAYER_LUNARG_core_validation",
            "VK_LAYER_GOOGLE_unique_objects"
        ]
    ];

    public bool AreEnabled => settings.ValidationEnabled && _layerNames.Length > 0;
    public string[] LayerNames => _layerNames;
    public bool IsInitialized => _isInitialized;
    public DebugUtilsMessengerEXT DebugMessenger => _debugMessenger;

    public void Initialize(Vk vk, Instance instance)
    {
        logger.LogDebug("Initialize called: ValidationEnabled={ValidationEnabled}, LayerCount={LayerCount}, AreEnabled={AreEnabled}",
            settings.ValidationEnabled, _layerNames.Length, AreEnabled);

        if (!AreEnabled)
        {
            logger.LogDebug("Skipping validation layer initialization (disabled or unavailable)");
            return;
        }

        _vk = vk;
        _instance = instance;

        logger.LogDebug("Attempting to get debug utils extension...");
        ExtDebugUtils debugUtils;
        if (!vk.TryGetInstanceExtension(instance, out debugUtils))
        {
            logger.LogWarning("VK_EXT_debug_utils extension not available - validation messages will not be captured");
            logger.LogWarning("Instance handle: {InstanceHandle}", instance.Handle);
            return;
        }
        _debugUtils = debugUtils;
        logger.LogDebug("Debug utils extension acquired successfully");

        CreateDebugMessenger();
        _isInitialized = true;
        logger.LogDebug("Validation layer initialization complete");
    }

    /// <summary>
    /// Detects available validation layers on the system.
    /// Supports pattern matching with wildcards and regex.
    /// </summary>
    /// <remarks>
    /// Pattern matching rules:
    /// - "*" - Matches all available layers (uses priority-based selection)
    /// - "VK_LAYER_*" - Wildcard matching (converted to regex)
    /// - "VK_LAYER_KHRONOS_.*" - Regex pattern matching
    /// - Exact layer names are matched as-is
    /// 
    /// Examples:
    /// - ["*"] - All layers via priority selection
    /// - ["VK_LAYER_KHRONOS_validation"] - Specific layer
    /// - ["VK_LAYER_KHRONOS_*"] - All KHRONOS layers
    /// - ["VK_LAYER_.*_validation"] - All validation layers
    /// </remarks>
    private string[] DetectValidationLayers()
    {
        var vk = Vk.GetApi();

        uint layerCount = 0;
        vk.EnumerateInstanceLayerProperties(&layerCount, null);

        if (layerCount == 0)
        {
            logger.LogDebug("No validation layers available on this system");
            return [];
        }

        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* pAvailableLayers = availableLayers)
        {
            vk.EnumerateInstanceLayerProperties(&layerCount, pAvailableLayers);
        }

        var availableLayerNames = availableLayers
            .Select(layer => Marshal.PtrToStringAnsi((nint)layer.LayerName))
            .Where(name => name != null)
            .Cast<string>()
            .ToHashSet();

        logger.LogDebug("Available validation layers on system: {Layers}", string.Join(", ", availableLayerNames));

        // Handle wildcard "*" - use priority-based selection
        if (settings.EnabledValidationLayers.Length == 1 && settings.EnabledValidationLayers[0] == "*")
        {
            logger.LogDebug("Wildcard '*' detected - using priority-based layer selection");

            // Try each priority set until we find one where all layers are available
            foreach (var layerSet in ValidationLayerPriority)
            {
                if (layerSet.All(availableLayerNames.Contains))
                {
                    logger.LogInformation("Selected validation layers (priority match): {Layers}", string.Join(", ", layerSet));
                    return layerSet;
                }
            }

            logger.LogWarning("No complete priority validation layer set found");
            return [];
        }

        // Handle pattern matching for configured layers
        if (settings.EnabledValidationLayers.Length > 0)
        {
            var selectedLayers = new List<string>();

            foreach (var pattern in settings.EnabledValidationLayers)
            {
                // Convert wildcard pattern to regex if it contains '*'
                string regexPattern;
                if (pattern.Contains('*'))
                {
                    // Convert wildcard to regex: * -> .*
                    regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                    logger.LogDebug("Pattern '{Pattern}' converted to regex: {Regex}", pattern, regexPattern);
                }
                else if (pattern.Contains('.') || pattern.Contains('[') || pattern.Contains('^'))
                {
                    // Looks like a regex pattern
                    regexPattern = pattern;
                    logger.LogDebug("Using pattern as regex: {Pattern}", pattern);
                }
                else
                {
                    // Exact match
                    if (availableLayerNames.Contains(pattern))
                    {
                        selectedLayers.Add(pattern);
                        logger.LogDebug("Exact match found: {Layer}", pattern);
                    }
                    else
                    {
                        logger.LogWarning("Requested layer '{Layer}' not available on this system", pattern);
                    }
                    continue;
                }

                // Apply regex pattern
                try
                {
                    var regex = new Regex(regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                    var matches = availableLayerNames.Where(name => regex.IsMatch(name)).ToList();

                    if (matches.Count > 0)
                    {
                        selectedLayers.AddRange(matches);
                        logger.LogDebug("Pattern '{Pattern}' matched {Count} layer(s): {Layers}",
                            pattern, matches.Count, string.Join(", ", matches));
                    }
                    else
                    {
                        logger.LogWarning("Pattern '{Pattern}' did not match any available layers", pattern);
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    logger.LogError("Regex pattern '{Pattern}' timed out during matching", pattern);
                }
                catch (ArgumentException ex)
                {
                    logger.LogError(ex, "Invalid regex pattern '{Pattern}': {Message}", pattern, ex.Message);
                }
            }

            // Remove duplicates while preserving order
            var distinctLayers = selectedLayers.Distinct().ToArray();

            if (distinctLayers.Length > 0)
            {
                logger.LogInformation("Selected validation layers (pattern match): {Layers}", string.Join(", ", distinctLayers));
                return distinctLayers;
            }
            else
            {
                logger.LogWarning("No validation layers matched the configured patterns");
                return [];
            }
        }

        // No configuration provided - use priority-based selection as fallback
        logger.LogDebug("No validation layer configuration - using priority-based selection");
        foreach (var layerSet in ValidationLayerPriority)
        {
            if (layerSet.All(availableLayerNames.Contains))
            {
                logger.LogInformation("Selected validation layers (default priority): {Layers}", string.Join(", ", layerSet));
                return layerSet;
            }
        }

        logger.LogWarning("No validation layers available");
        return [];
    }

    /// <summary>
    /// Creates the debug messenger to receive validation callbacks.
    /// </summary>
    private void CreateDebugMessenger()
    {
        var severityFlags = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;

        // Add severity flags based on enabled log levels
        if (logger.IsEnabled(LogLevel.Trace) || logger.IsEnabled(LogLevel.Debug))
        {
            severityFlags |= DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            severityFlags |= DebugUtilsMessageSeverityFlagsEXT.InfoBitExt;
        }

        if (logger.IsEnabled(LogLevel.Warning))
        {
            severityFlags |= DebugUtilsMessageSeverityFlagsEXT.WarningBitExt;
        }

        var createInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = severityFlags,
            MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                         DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                         DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback
        };

        fixed (DebugUtilsMessengerEXT* pMessenger = &_debugMessenger)
        {
            var result = _debugUtils!.CreateDebugUtilsMessenger(_instance, &createInfo, null, pMessenger);

            if (result != Result.Success)
            {
                logger.LogError("Failed to create debug messenger: {Result}", result);
                throw new Exception($"Failed to create debug messenger: {result}");
            }
        }

        logger.LogInformation("Debug messenger created (Handle: {Handle})", _debugMessenger.Handle);
    }

    /// <summary>
    /// Vulkan debug callback - routes validation messages to ILogger.
    /// </summary>
    private uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageTypes,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* pUserData)
    {
        var message = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage) ?? string.Empty;

        // Map Vulkan severity to ILogger level
        var logLevel = messageSeverity switch
        {
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => LogLevel.Error,
            DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => LogLevel.Warning,
            DebugUtilsMessageSeverityFlagsEXT.InfoBitExt => LogLevel.Information,
            DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt => LogLevel.Debug,
            _ => LogLevel.Debug
        };

        logger.Log(logLevel, "[Vulkan {MessageType}] {Message}", messageTypes, message);

        return Vk.False;
    }

    public void Dispose()
    {
        if (_isInitialized && _debugMessenger.Handle != 0)
        {
            _debugUtils!.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
            logger.LogDebug("Debug messenger destroyed");
        }

        _debugUtils?.Dispose();
        _isInitialized = false;
    }
}