# Vulkan Validation Layers Configuration

## Overview

The Vulkan validation layers system supports flexible pattern matching for layer selection, allowing precise control over which validation layers are enabled.

## Configuration

Validation layers are configured via `VkSettings` in your application settings:

```json
{
  "Graphics": {
    "Vulkan": {
      "ValidationEnabled": true,
      "MinLogSeverity": "Warning",
      "EnabledValidationLayers": ["*"]
    }
  }
}
```

### Configuration Properties

| Property                  | Type       | Default | Description                                    |
| ------------------------- | ---------- | ------- | ---------------------------------------------- |
| `ValidationEnabled`       | `bool`     | `false` | Master switch to enable/disable validation     |
| `MinLogSeverity`          | `LogLevel` | `Trace` | Minimum severity level for validation messages |
| `EnabledValidationLayers` | `string[]` | `["*"]` | Patterns to match validation layers            |

---

## Pattern Matching

### Wildcard: `["*"]` (Default)

Enables all available validation layers using priority-based selection:

1. **Modern**: `VK_LAYER_KHRONOS_validation` (preferred)
2. **Legacy**: `VK_LAYER_LUNARG_standard_validation`
3. **Individual**: Old SDK versions with separate layers

**Example:**

```json
{
  "EnabledValidationLayers": ["*"]
}
```

**Result:** Automatically selects the best available layer set for your SDK version.

---

### Exact Match: Specific Layer Names

Specify exact layer names to enable only those layers:

**Example:**

```json
{
  "EnabledValidationLayers": ["VK_LAYER_KHRONOS_validation"]
}
```

**Result:** Only enables the specified layer if available. Warns if not found.

---

### Wildcard Patterns: `*` Operator

Use `*` as a wildcard to match multiple layers:

**Examples:**

#### Match all KHRONOS layers:

```json
{
  "EnabledValidationLayers": ["VK_LAYER_KHRONOS_*"]
}
```

**Matches:**

- `VK_LAYER_KHRONOS_validation`
- `VK_LAYER_KHRONOS_synchronization2`
- Any other KHRONOS layers

#### Match all validation layers:

```json
{
  "EnabledValidationLayers": ["*_validation"]
}
```

**Matches:**

- `VK_LAYER_KHRONOS_validation`
- `VK_LAYER_LUNARG_standard_validation`
- Any layer ending with "\_validation"

#### Match all LUNARG layers:

```json
{
  "EnabledValidationLayers": ["VK_LAYER_LUNARG_*"]
}
```

---

### Regex Patterns: Advanced Matching

Use regular expressions for complex matching:

**Examples:**

#### Match any validation layer (regex):

```json
{
  "EnabledValidationLayers": ["VK_LAYER_.*_validation"]
}
```

**Matches:**

- `VK_LAYER_KHRONOS_validation`
- `VK_LAYER_LUNARG_standard_validation`
- Any layer with "\_validation" in the name

#### Match specific vendors:

```json
{
  "EnabledValidationLayers": ["VK_LAYER_(KHRONOS|LUNARG)_.*"]
}
```

**Matches:** Any layer from KHRONOS or LUNARG vendors

#### Complex pattern:

```json
{
  "EnabledValidationLayers": [
    "^VK_LAYER_KHRONOS_.*$",
    "^VK_LAYER_GOOGLE_threading$"
  ]
}
```

**Matches:** All KHRONOS layers plus the specific Google threading layer

---

## Multiple Patterns

You can specify multiple patterns - all matching layers will be enabled:

```json
{
  "EnabledValidationLayers": [
    "VK_LAYER_KHRONOS_validation",
    "VK_LAYER_LUNARG_*"
  ]
}
```

**Result:** Enables KHRONOS validation layer AND all LUNARG layers.

---

## Common Use Cases

### Development - Maximum Validation

```json
{
  "ValidationEnabled": true,
  "MinLogSeverity": "Trace",
  "EnabledValidationLayers": ["*"]
}
```

Enables all available validation with verbose output.

---

### Testing - Standard Validation

```json
{
  "ValidationEnabled": true,
  "MinLogSeverity": "Warning",
  "EnabledValidationLayers": ["VK_LAYER_KHRONOS_validation"]
}
```

Modern validation layer only, warnings and errors only.

---

### Production Debugging - Minimal Overhead

```json
{
  "ValidationEnabled": true,
  "MinLogSeverity": "Error",
  "EnabledValidationLayers": ["VK_LAYER_KHRONOS_validation"]
}
```

Errors only for production debugging.

---

### Legacy SDK Support

```json
{
  "ValidationEnabled": true,
  "MinLogSeverity": "Warning",
  "EnabledValidationLayers": [
    "VK_LAYER_KHRONOS_validation",
    "VK_LAYER_LUNARG_standard_validation"
  ]
}
```

Falls back to legacy layer if modern one isn't available.

---

### Specific Debugging - Threading Issues

```json
{
  "ValidationEnabled": true,
  "MinLogSeverity": "Debug",
  "EnabledValidationLayers": [
    "VK_LAYER_KHRONOS_validation",
    "VK_LAYER_GOOGLE_threading"
  ]
}
```

Standard validation plus extra threading validation.

---

## Pattern Resolution Logic

1. **"\*" Wildcard**: Use priority-based selection (preferred method)
2. **Wildcard Patterns**: Convert to regex (`*` → `.*`)
3. **Regex Detection**: Patterns with `.`, `[`, `^` treated as regex
4. **Exact Match**: Plain strings matched exactly
5. **Deduplication**: Duplicate matches removed while preserving order

---

## Logging

The validation layer system logs detailed information about layer selection:

```
[DEBUG] VkValidationLayers: Available validation layers on system: VK_LAYER_KHRONOS_validation, VK_LAYER_LUNARG_monitor
[DEBUG] VkValidationLayers: Wildcard '*' detected - using priority-based layer selection
[INFO]  VkValidationLayers: Selected validation layers (priority match): VK_LAYER_KHRONOS_validation
[INFO]  VkValidationLayers: Debug messenger created (Handle: 1234567890)
```

### Pattern Matching Logs

```
[DEBUG] VkValidationLayers: Pattern 'VK_LAYER_KHRONOS_*' converted to regex: ^VK_LAYER_KHRONOS_.*$
[DEBUG] VkValidationLayers: Pattern 'VK_LAYER_KHRONOS_*' matched 2 layer(s): VK_LAYER_KHRONOS_validation, VK_LAYER_KHRONOS_synchronization2
[INFO]  VkValidationLayers: Selected validation layers (pattern match): VK_LAYER_KHRONOS_validation, VK_LAYER_KHRONOS_synchronization2
```

### Error Cases

```
[WARNING] VkValidationLayers: Requested layer 'VK_LAYER_DOES_NOT_EXIST' not available on this system
[WARNING] VkValidationLayers: Pattern 'VK_LAYER_FAKE_*' did not match any available layers
[ERROR]   VkValidationLayers: Invalid regex pattern 'VK_LAYER_[': Unterminated [] set
```

---

## Requirements

- **Vulkan SDK**: Must be installed on the target machine
- **Debug Build**: Recommended for development (can be enabled in Release via config)
- **VK_EXT_debug_utils**: Extension required for message capture

---

## Performance Impact

Validation layers add overhead:

| Severity | Overhead | Use Case             |
| -------- | -------- | -------------------- |
| Trace    | ~20-30%  | Development only     |
| Debug    | ~15-20%  | Active debugging     |
| Info     | ~10-15%  | Testing              |
| Warning  | ~5-10%   | Production debugging |
| Error    | ~3-5%    | Minimal impact       |

**Recommendation:** Disable validation in production (`ValidationEnabled: false`) or use `Error` level only.

---

## Troubleshooting

### No Validation Layers Available

```
[WARNING] VkValidationLayers: Validation layers requested but none are available. Install Vulkan SDK for validation support.
```

**Solution:** Install the Vulkan SDK from https://vulkan.lunarg.com/

---

### Debug Messenger Not Created

```
[WARNING] VkValidationLayers: VK_EXT_debug_utils extension not available - validation messages will not be captured
```

**Solution:** Ensure Vulkan instance was created with validation layers enabled. This is automatically handled by `VkContext`.

---

### Pattern Not Matching

```
[WARNING] VkValidationLayers: Pattern 'VK_LAYER_MY_*' did not match any available layers
```

**Solutions:**

1. Check available layers: Run `vulkaninfo` or check DEBUG logs
2. Verify pattern syntax
3. Use wildcard `["*"]` to see what's available

---

## Examples for Different Scenarios

### Scenario: GPU Driver Debugging

```json
{
  "ValidationEnabled": true,
  "MinLogSeverity": "Trace",
  "EnabledValidationLayers": ["*"]
}
```

### Scenario: Synchronization Issues

```json
{
  "ValidationEnabled": true,
  "MinLogSeverity": "Debug",
  "EnabledValidationLayers": [
    "VK_LAYER_KHRONOS_validation",
    "VK_LAYER_KHRONOS_synchronization2"
  ]
}
```

### Scenario: Performance Profiling

```json
{
  "ValidationEnabled": true,
  "MinLogSeverity": "Warning",
  "EnabledValidationLayers": ["VK_LAYER_KHRONOS_validation"]
}
```

### Scenario: Continuous Integration

```json
{
  "ValidationEnabled": true,
  "MinLogSeverity": "Warning",
  "EnabledValidationLayers": ["VK_LAYER_KHRONOS_validation"]
}
```

---

## See Also

- [Vulkan Architecture Documentation](.docs/Vulkan Architecture.md)
- [Silk.NET Vulkan Capabilities](.docs/Silk.NET Vulkan Capabilities.md)
- [LunarG Validation Layers Documentation](https://vulkan.lunarg.com/doc/sdk/latest/windows/validation_layers.html)
