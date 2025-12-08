using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages Vulkan validation layers and debug messenger for runtime error detection and diagnostics.
/// </summary>
public unsafe class VkValidation : IVkValidation
{
    private readonly VulkanSettings _vkSettings;
    private readonly string[] _layerNames;

    private Vk? _vk;
    private Instance _instance;
    private ExtDebugUtils? _debugUtils;
    private DebugUtilsMessengerEXT _debugMessenger;
    private bool _isInitialized;

    public VkValidation(IOptions<VulkanSettings> options)
    {
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

                Log.Debug(sb.ToString());
            }
            
            // In production/release mode without debugger, be completely quiet
            return;
        }

        // Handle validation enabled case
        if (_vkSettings.EnableValidationLayers)
        {
            if (!debuggerAttached)
            {
                Log.Warning("=== VALIDATION LAYERS ENABLED (NO DEBUGGER) ===");
                Log.Warning("Performance impact: Validation layers cause significant performance degradation");
                Log.Warning("For production deployment, disable validation layers:");
                Log.Warning("1. Set 'EnableValidationLayers: false' in GraphicsSettings configuration");
                Log.Warning("2. This improves runtime performance substantially");
            }
            else
            {
                Log.Debug("=== VALIDATION LAYERS ENABLED (DEBUGGER DETECTED) ===");
                Log.Debug("Development mode: Validation layers active for debugging");
            }

            // Check if SDK validation layers are available
            if (_layerNames.Length == 0)
            {
                Log.Error("=== VULKAN SDK VALIDATION LAYERS REQUIRED ===");
                Log.Error("Validation is enabled but no SDK validation layers are available.");
                Log.Error("Available layers on this system: GPU driver layers only");
                Log.Error("SDK validation layers (like VK_LAYER_KHRONOS_validation) are required for validation.");
                Log.Error("");
                Log.Error("To install SDK validation layers:");
                Log.Error("1. Download Vulkan SDK from: https://vulkan.lunarg.com/sdk/home");
                Log.Error("2. Install the SDK for your platform");
                Log.Error("3. Restart your development environment");
                Log.Error("4. Verification: Run 'vulkaninfo' and check for validation layers");
                Log.Error("");
                Log.Error("Note: GPU driver layers alone cannot provide validation functionality.");
                return;
            }

            // Configure validation layers
            Log.Debug($"Configuration: Available layers: [{string.Join(", ", _layerNames)}]");

            _vk = vk;
            _instance = instance;

            Log.Debug("Setting up debug messenger...");
            
            ExtDebugUtils debugUtils;
            if (!vk.TryGetInstanceExtension(instance, out debugUtils))
            {
                Log.Error("VK_EXT_debug_utils extension not available");
                Log.Error("Validation messages will not be captured!");
                return;
            }
            
            _debugUtils = debugUtils;
            Log.Debug("VK_EXT_debug_utils extension acquired");

            CreateDebugMessenger();
            _isInitialized = true;
            Log.Debug("Validation layers initialized successfully - debug messenger active");
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
            Log.Debug("=== VULKAN SDK VALIDATION LAYER DETECTION ===");
            Log.Debug($"Configuration: EnableValidationLayers={_vkSettings.EnableValidationLayers}");
            Log.Debug($"Requested layer patterns: [{string.Join(", ", _vkSettings.EnabledValidationLayers)}]");
        }

        var vk = Vk.GetApi();

        uint layerCount = 0;
        var enumResult = vk.EnumerateInstanceLayerProperties(&layerCount, null);
        
        if (shouldLogDetails)
        {
            Log.Debug($"EnumerateInstanceLayerProperties (count query) returned: {enumResult}, LayerCount: {layerCount}");
        }

        if (layerCount == 0)
        {
            if (_vkSettings.EnableValidationLayers)
            {
                Log.Error("[X] NO VULKAN LAYERS FOUND - SDK installation required for validation");
                Log.Error("To install the Vulkan SDK:");
                Log.Error("  1. Download from: https://vulkan.lunarg.com/sdk/home");
                Log.Error("  2. Install for your platform (Windows/Linux/macOS)");
                Log.Error("  3. Restart your development environment");
                Log.Error("  4. Verify installation with 'vulkaninfo' command");
            }
            // Be completely quiet if validation disabled and no debugger
            return [];
        }

        if (shouldLogDetails)
        {
            Log.Debug($"Found {layerCount} total Vulkan layers on system");
        }

        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* pAvailableLayers = availableLayers)
        {
            enumResult = vk.EnumerateInstanceLayerProperties(&layerCount, pAvailableLayers);
            Log.Debug($"EnumerateInstanceLayerProperties (data query) returned: {enumResult}");
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
                    Log.Debug($"[+] Found VALIDATION layer: {layerName} (v{version}) - {description}");
                }
            }
            else
            {
                driverLayers.Add(layerName);
                if (shouldLogDetails)
                {
                    Log.Debug($"[D] Found DRIVER layer: {layerName} (v{version}) - {description}");
                }
            }
        }

        var availableLayerNamesSet = availableLayerNames.ToHashSet();

        if (shouldLogDetails)
        {
            Log.Debug("=== LAYER ANALYSIS ===");
            Log.Debug($"Total layers found: {layerCount}");
            Log.Debug($"Validation layers: {validationLayers.Count} [{string.Join(", ", validationLayers)}]");
            Log.Debug($"Driver/GPU layers: {driverLayers.Count} [{string.Join(", ", driverLayers)}]");
        }

        // Check for SDK installation indicators
        bool hasKhronosValidation = validationLayers.Any(l => l == "VK_LAYER_KHRONOS_validation");
        bool hasLegacyValidation = validationLayers.Any(l => l == "VK_LAYER_LUNARG_standard_validation");
        bool hasSdkLayers = hasKhronosValidation || hasLegacyValidation;

        if (hasSdkLayers && shouldLogDetails)
        {
            Log.Debug("[+] VULKAN SDK DETECTED: Standard validation layers are available");
        }
        else if (_vkSettings.EnableValidationLayers)
        {
            // Only warn about missing SDK when validation is enabled
            Log.Warning("[!] VULKAN SDK NOT DETECTED: Only GPU driver layers found");
            Log.Warning("For development, install the Vulkan SDK from: https://vulkan.lunarg.com/sdk/home");
        }
        // Be completely quiet if validation disabled and no debugger

        // Handle wildcard "*" - use priority-based selection
        if (_vkSettings.EnabledValidationLayers.Length == 1 && _vkSettings.EnabledValidationLayers[0] == "*")
        {
            if (shouldLogDetails)
            {
                Log.Debug("=== PRIORITY-BASED LAYER SELECTION ===");
                Log.Debug("Wildcard '*' detected - using priority-based layer selection");
            }

            // Try each priority set until we find one where all layers are available
            int priorityIndex = 1;
            foreach (var layerSet in ValidationLayerPriority)
            {
                if (shouldLogDetails)
                {
                    Log.Debug($"Trying priority set {priorityIndex}: [{string.Join(", ", layerSet)}]");
                }
                
                var missingLayers = layerSet.Where(layer => !availableLayerNamesSet.Contains(layer)).ToList();
                
                if (missingLayers.Count == 0)
                {
                    if (shouldLogDetails)
                    {
                        Log.Debug($"[+] PRIORITY SET {priorityIndex} MATCHED: [{string.Join(", ", layerSet)}]");
                        Log.Debug($"Selected validation layers (priority match): {string.Join(", ", layerSet)}");
                    }
                    return layerSet;
                }
                else
                {
                    if (shouldLogDetails)
                    {
                        Log.Debug($"[-] Priority set {priorityIndex} incomplete - missing: [{string.Join(", ", missingLayers)}]");
                    }
                }
                
                priorityIndex++;
            }

            if (_vkSettings.EnableValidationLayers)
            {
                Log.Warning("[-] NO PRIORITY VALIDATION LAYER SET FOUND");
                Log.Warning("Available layers: [{Available}]", string.Join(", ", availableLayerNames));
                Log.Warning("None of the predefined validation layer sets are completely available on this system.");
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
                    Log.Debug($"Pattern '{pattern}' converted to regex: {regexPattern}");
                }
                else if (pattern.Contains('.') || pattern.Contains('[') || pattern.Contains('^'))
                {
                    // Looks like a regex pattern
                    regexPattern = pattern;
                    Log.Debug($"Using pattern as regex: {pattern}");
                }
                else
                {
                    // Exact match
                    if (availableLayerNames.Contains(pattern))
                    {
                        selectedLayers.Add(pattern);
                    }
                    continue;
                }
                    var regex = new Regex(regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                    var matches = availableLayerNames.Where(name => regex.IsMatch(name)).ToList();

                    if (matches.Count > 0)
                    {
                        selectedLayers.AddRange(matches);
                        Log.Debug($"Pattern '{pattern}' matched {matches.Count} layer(s): {string.Join(", ", matches)}");
                    }
                    else
                    {
                        Log.Warning($"Pattern '{pattern}' did not match any available layers");
                    }
        }            // Remove duplicates while preserving order
            var distinctLayers = selectedLayers.Distinct().ToArray();

            if (distinctLayers.Length > 0)
            {
                Log.Debug($"Selected validation layers (pattern match): {string.Join(", ", distinctLayers)}");
                return distinctLayers;
            }
            else
            {
                Log.Warning("No validation layers matched the configured patterns");
                return [];
            }
        }

        // No configuration provided - use priority-based selection as fallback
                Log.Debug("No validation layer configuration - using priority-based selection");
        foreach (var layerSet in ValidationLayerPriority)
        {
            if (layerSet.All(availableLayerNames.Contains))
            {
                Log.Debug($"Selected validation layers (default priority): {string.Join(", ", layerSet)}");
                return layerSet;
            }
        }

        Log.Warning("No validation layers available");
        return [];
    }

    /// <summary>
    /// Creates the debug messenger to receive validation callbacks.
    /// </summary>
    private void CreateDebugMessenger()
    {
        Log.Debug("=== CREATING VULKAN DEBUG MESSENGER ===");
        
        var severityFlags = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        
        Log.Debug($"Debug messenger severity flags: {severityFlags}");

        var messageTypes = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                           DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                           DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt;
                          
        Log.Debug($"Debug messenger message types: {messageTypes}");
            Log.Debug("  - GENERAL: System-level messages");
            Log.Debug("  - VALIDATION: API usage validation errors");
            Log.Debug("  - PERFORMANCE: Performance-related warnings");

        var createInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = severityFlags,
            MessageType = messageTypes,
            PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback
        };

        Log.Debug("Creating debug messenger with Vulkan API...");
        fixed (DebugUtilsMessengerEXT* pMessenger = &_debugMessenger)
        {
            var result = _debugUtils!.CreateDebugUtilsMessenger(_instance, &createInfo, null, pMessenger);

            if (result != Result.Success)
            {
                Log.Error($"[-] FAILED TO CREATE DEBUG MESSENGER: {result}");
                Log.Error("Validation messages will not be captured!");
                throw new Exception($"Failed to create debug messenger: {result}");
            }
        }

        Log.Debug($"[+] DEBUG MESSENGER CREATED SUCCESSFULLY (Handle: {_debugMessenger.Handle})");
        Log.Debug("[*] Validation layer setup complete - ready to capture validation messages!");
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

        // Enhanced logging with clear indicators
        var severityIcon = messageSeverity switch
        {
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => "[ERROR] VULKAN",
            DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => "[WARN] VULKAN", 
            DebugUtilsMessageSeverityFlagsEXT.InfoBitExt => "[INFO] VULKAN",
            DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt => "[DEBUG] VULKAN",
            _ => "[DEBUG] VULKAN"
        };

        Log.Debug($"{severityIcon} [{messageTypes}] {message}");

        return Vk.False;
    }

    public void Dispose()
    {
        if (_isInitialized && _debugMessenger.Handle != 0)
        {
            _debugUtils!.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
            Log.Debug("Debug messenger destroyed");
        }

        _debugUtils?.Dispose();
        _isInitialized = false;
    }
}