using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages Vulkan validation layers and debug messenger for runtime error detection and diagnostics.
/// </summary>
public unsafe class Validation : IValidation
{
    private readonly ILogger logger;
    private readonly VulkanSettings _vkSettings;
    private readonly string[] _layerNames;

    private Vk? _vk;
    private Instance _instance;
    private ExtDebugUtils? _debugUtils;
    private DebugUtilsMessengerEXT _debugMessenger;
    private bool _isInitialized;

    public Validation(ILoggerFactory loggerFactory, IOptions<VulkanSettings> options)
    {
        logger = loggerFactory.CreateLogger(nameof(Validation));
        _vkSettings = options.Value;
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

    public bool AreEnabled => _vkSettings.EnableValidationLayers && _layerNames.Length > 0;
    public string[] LayerNames => _layerNames;
    public bool IsInitialized => _isInitialized;
    public DebugUtilsMessengerEXT DebugMessenger => _debugMessenger;

    public void Initialize(Vk vk, Instance instance)
    {
        bool debuggerAttached = Debugger.IsAttached;
        
        // Handle validation disabled case
        if (!_vkSettings.EnableValidationLayers)
        {
            if (debuggerAttached)
            {
                var sb = new StringBuilder()
                .AppendLine("=== VULKAN VALIDATION LAYERS DISABLED IN DEBUG MODE ===")
                .AppendLine("OPTIONAL: For development, enabling validation layers is recommended")
                .AppendLine("Your application will run fine without validation layers enabled")
                .AppendLine("\nTo enable validation layers, set 'EnableValidationLayers: true' in GraphicsSettings configuration");

                if (_layerNames.Length == 0)
                {
                    sb.AppendLine("\nNo validation layers available - Vulkan SDK does not appear to be installed")
                      .AppendLine("Install the Vulkan SDK from: https://vulkan.lunarg.com/sdk/home");
                }
                else
                {
                    sb.AppendLine("Vulkan SDK validation layers:")
                      .AppendLine($"  {string.Join("\n  ", _layerNames)}");
                }

                logger.LogInformation(sb.ToString());
            }
            
            // In production/release mode without debugger, be completely quiet
            return;
        }

        // Handle validation enabled case
        if (_vkSettings.EnableValidationLayers)
        {
            if (!debuggerAttached)
            {
                logger.LogWarning("=== VALIDATION LAYERS ENABLED (NO DEBUGGER) ===");
                logger.LogWarning("Performance impact: Validation layers cause significant performance degradation");
                logger.LogWarning("For production deployment, disable validation layers:");
                logger.LogWarning("1. Set 'EnableValidationLayers: false' in GraphicsSettings configuration");
                logger.LogWarning("2. This improves runtime performance substantially");
            }
            else
            {
                logger.LogInformation("=== VALIDATION LAYERS ENABLED (DEBUGGER DETECTED) ===");
                logger.LogInformation("Development mode: Validation layers active for debugging");
            }

            // Check if SDK validation layers are available
            if (_layerNames.Length == 0)
            {
                logger.LogError("=== VULKAN SDK VALIDATION LAYERS REQUIRED ===");
                logger.LogError("Validation is enabled but no SDK validation layers are available.");
                logger.LogError("Available layers on this system: GPU driver layers only");
                logger.LogError("SDK validation layers (like VK_LAYER_KHRONOS_validation) are required for validation.");
                logger.LogError("");
                logger.LogError("To install SDK validation layers:");
                logger.LogError("1. Download Vulkan SDK from: https://vulkan.lunarg.com/sdk/home");
                logger.LogError("2. Install the SDK for your platform");
                logger.LogError("3. Restart your development environment");
                logger.LogError("4. Verification: Run 'vulkaninfo' and check for validation layers");
                logger.LogError("");
                logger.LogError("Note: GPU driver layers alone cannot provide validation functionality.");
                return;
            }

            // Configure validation layers
            logger.LogInformation("Configuration: Available layers: [{Layers}]", string.Join(", ", _layerNames));

            _vk = vk;
            _instance = instance;

            logger.LogDebug("Setting up debug messenger...");
            
            ExtDebugUtils debugUtils;
            if (!vk.TryGetInstanceExtension(instance, out debugUtils))
            {
                logger.LogError("VK_EXT_debug_utils extension not available");
                logger.LogError("Validation messages will not be captured!");
                return;
            }
            
            _debugUtils = debugUtils;
            logger.LogDebug("VK_EXT_debug_utils extension acquired");

            CreateDebugMessenger();
            _isInitialized = true;
            logger.LogInformation("Validation layers initialized successfully - debug messenger active");
        }
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
        bool debuggerAttached = Debugger.IsAttached;
        
        // Only be verbose if validation is enabled OR if debugger is attached
        bool shouldLogDetails = _vkSettings.EnableValidationLayers || debuggerAttached;
        
        if (shouldLogDetails)
        {
            logger.LogInformation("=== VULKAN SDK VALIDATION LAYER DETECTION ===");
            logger.LogInformation("Configuration: EnableValidationLayers={EnableValidationLayers}", _vkSettings.EnableValidationLayers);
            logger.LogInformation("Requested layer patterns: [{Patterns}]", string.Join(", ", _vkSettings.EnabledValidationLayers));
        }

        var vk = Vk.GetApi();

        uint layerCount = 0;
        var enumResult = vk.EnumerateInstanceLayerProperties(&layerCount, null);
        
        if (shouldLogDetails)
        {
            logger.LogDebug("EnumerateInstanceLayerProperties (count query) returned: {Result}, LayerCount: {Count}", enumResult, layerCount);
        }

        if (layerCount == 0)
        {
            if (_vkSettings.EnableValidationLayers)
            {
                logger.LogError("[X] NO VULKAN LAYERS FOUND - SDK installation required for validation");
                logger.LogError("To install the Vulkan SDK:");
                logger.LogError("  1. Download from: https://vulkan.lunarg.com/sdk/home");
                logger.LogError("  2. Install for your platform (Windows/Linux/macOS)");
                logger.LogError("  3. Restart your development environment");
                logger.LogError("  4. Verify installation with 'vulkaninfo' command");
            }
            // Be completely quiet if validation disabled and no debugger
            return [];
        }

        if (shouldLogDetails)
        {
            logger.LogInformation("Found {LayerCount} total Vulkan layers on system", layerCount);
        }

        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* pAvailableLayers = availableLayers)
        {
            enumResult = vk.EnumerateInstanceLayerProperties(&layerCount, pAvailableLayers);
            logger.LogDebug("EnumerateInstanceLayerProperties (data query) returned: {Result}", enumResult);
        }

        var availableLayerNames = new List<string>();
        var validationLayers = new List<string>();
        var driverLayers = new List<string>();

        for (int i = 0; i < layerCount; i++)
        {
            var layer = availableLayers[i];
            var layerName = Marshal.PtrToStringAnsi((nint)layer.LayerName) ?? string.Empty;
            var description = Marshal.PtrToStringAnsi((nint)layer.Description) ?? string.Empty;
            var version = $"{layer.SpecVersion >> 22}.{(layer.SpecVersion >> 12) & 0x3FF}.{layer.SpecVersion & 0xFFF}";
            
            availableLayerNames.Add(layerName);
            
            // Categorize layers
            if (layerName.Contains("validation", StringComparison.OrdinalIgnoreCase))
            {
                validationLayers.Add(layerName);
                if (shouldLogDetails)
                {
                    logger.LogInformation("[+] Found VALIDATION layer: {Layer} (v{Version}) - {Description}", layerName, version, description);
                }
            }
            else
            {
                driverLayers.Add(layerName);
                if (shouldLogDetails)
                {
                    logger.LogDebug("[D] Found DRIVER layer: {Layer} (v{Version}) - {Description}", layerName, version, description);
                }
            }
        }

        var availableLayerNamesSet = availableLayerNames.ToHashSet();

        if (shouldLogDetails)
        {
            logger.LogInformation("=== LAYER ANALYSIS ===");
            logger.LogInformation("Total layers found: {Total}", layerCount);
            logger.LogInformation("Validation layers: {Count} [{Layers}]", validationLayers.Count, string.Join(", ", validationLayers));
            logger.LogInformation("Driver/GPU layers: {Count} [{Layers}]", driverLayers.Count, string.Join(", ", driverLayers));
        }

        // Check for SDK installation indicators
        bool hasKhronosValidation = validationLayers.Any(l => l == "VK_LAYER_KHRONOS_validation");
        bool hasLegacyValidation = validationLayers.Any(l => l == "VK_LAYER_LUNARG_standard_validation");
        bool hasSdkLayers = hasKhronosValidation || hasLegacyValidation;

        if (hasSdkLayers && shouldLogDetails)
        {
            logger.LogInformation("[+] VULKAN SDK DETECTED: Standard validation layers are available");
        }
        else if (_vkSettings.EnableValidationLayers)
        {
            // Only warn about missing SDK when validation is enabled
            logger.LogWarning("[!] VULKAN SDK NOT DETECTED: Only GPU driver layers found");
            logger.LogWarning("For development, install the Vulkan SDK from: https://vulkan.lunarg.com/sdk/home");
        }
        // Be completely quiet if validation disabled and no debugger

        // Handle wildcard "*" - use priority-based selection
        if (_vkSettings.EnabledValidationLayers.Length == 1 && _vkSettings.EnabledValidationLayers[0] == "*")
        {
            if (shouldLogDetails)
            {
                logger.LogInformation("=== PRIORITY-BASED LAYER SELECTION ===");
                logger.LogDebug("Wildcard '*' detected - using priority-based layer selection");
            }

            // Try each priority set until we find one where all layers are available
            int priorityIndex = 1;
            foreach (var layerSet in ValidationLayerPriority)
            {
                if (shouldLogDetails)
                {
                    logger.LogDebug("Trying priority set {Index}: [{Layers}]", priorityIndex, string.Join(", ", layerSet));
                }
                
                var missingLayers = layerSet.Where(layer => !availableLayerNamesSet.Contains(layer)).ToList();
                
                if (missingLayers.Count == 0)
                {
                    if (shouldLogDetails)
                    {
                        logger.LogInformation("[+] PRIORITY SET {Index} MATCHED: [{Layers}]", priorityIndex, string.Join(", ", layerSet));
                        logger.LogInformation("Selected validation layers (priority match): {Layers}", string.Join(", ", layerSet));
                    }
                    return layerSet;
                }
                else
                {
                    if (shouldLogDetails)
                    {
                        logger.LogDebug("[-] Priority set {Index} incomplete - missing: [{Missing}]", priorityIndex, string.Join(", ", missingLayers));
                    }
                }
                
                priorityIndex++;
            }

            if (_vkSettings.EnableValidationLayers)
            {
                logger.LogWarning("[-] NO PRIORITY VALIDATION LAYER SET FOUND");
                logger.LogWarning("Available layers: [{Available}]", string.Join(", ", availableLayerNames));
                logger.LogWarning("None of the predefined validation layer sets are completely available on this system.");
            }
            // Be quiet if validation disabled and no debugger
            return [];
        }

        // Handle pattern matching for configured layers
        if (_vkSettings.EnabledValidationLayers.Length > 0)
        {
            var selectedLayers = new List<string>();

            foreach (var pattern in _vkSettings.EnabledValidationLayers)
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
        logger.LogInformation("=== CREATING VULKAN DEBUG MESSENGER ===");
        
        var severityFlags = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;

        // Add severity flags based on enabled log levels
        if (logger.IsEnabled(LogLevel.Trace) || logger.IsEnabled(LogLevel.Debug))
        {
            severityFlags |= DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt;
            logger.LogDebug("[+] Enabled VERBOSE validation messages (LogLevel.Trace/Debug)");
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            severityFlags |= DebugUtilsMessageSeverityFlagsEXT.InfoBitExt;
            logger.LogDebug("[+] Enabled INFO validation messages (LogLevel.Information)");
        }

        if (logger.IsEnabled(LogLevel.Warning))
        {
            severityFlags |= DebugUtilsMessageSeverityFlagsEXT.WarningBitExt;
            logger.LogDebug("[+] Enabled WARNING validation messages (LogLevel.Warning)");
        }
        
        logger.LogDebug("[+] Enabled ERROR validation messages (always enabled)");
        logger.LogInformation("Debug messenger severity flags: {Flags}", severityFlags);

        var messageTypes = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                          DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                          DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt;
                          
        logger.LogInformation("Debug messenger message types: {Types}", messageTypes);
        logger.LogDebug("  - GENERAL: System-level messages");
        logger.LogDebug("  - VALIDATION: API usage validation errors");
        logger.LogDebug("  - PERFORMANCE: Performance-related warnings");

        var createInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = severityFlags,
            MessageType = messageTypes,
            PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback
        };

        logger.LogDebug("Creating debug messenger with Vulkan API...");
        fixed (DebugUtilsMessengerEXT* pMessenger = &_debugMessenger)
        {
            var result = _debugUtils!.CreateDebugUtilsMessenger(_instance, &createInfo, null, pMessenger);

            if (result != Result.Success)
            {
                logger.LogError("[-] FAILED TO CREATE DEBUG MESSENGER: {Result}", result);
                logger.LogError("Validation messages will not be captured!");
                throw new Exception($"Failed to create debug messenger: {result}");
            }
        }

        logger.LogInformation("[+] DEBUG MESSENGER CREATED SUCCESSFULLY (Handle: {Handle})", _debugMessenger.Handle);
        logger.LogInformation("[*] Validation layer setup complete - ready to capture validation messages!");
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

        // Enhanced logging with clear indicators
        var severityIcon = messageSeverity switch
        {
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => "[ERROR] VULKAN",
            DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => "[WARN] VULKAN", 
            DebugUtilsMessageSeverityFlagsEXT.InfoBitExt => "[INFO] VULKAN",
            DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt => "[DEBUG] VULKAN",
            _ => "[DEBUG] VULKAN"
        };

        logger.Log(logLevel, "{Icon} [{MessageType}] {Message}", severityIcon, messageTypes, message);


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