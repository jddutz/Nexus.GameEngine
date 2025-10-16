# Cleanup: VulkanSettings.RenderPasses Removal

## Date: October 15, 2025

## Overview
Completed cleanup of VulkanSettings.RenderPasses configuration property and updated all dependent components to use the static RenderPasses class instead.

## Changes Made

### 1. VulkanSettings.cs
**Removed:**
```csharp
public RenderPassConfiguration[] RenderPasses { get; set; } = 
[
    new RenderPassConfiguration 
    { 
        Name = "Main",
        ColorFormat = Format.Undefined,
        DepthFormat = Format.D32Sfloat,
        // ... etc
    }
];
```

**Rationale:**
- Render passes are now defined centrally in `RenderPasses.Configurations` static array
- No need for per-instance configuration
- Simplifies VulkanSettings to focus only on device/swapchain preferences

### 2. SwapChain.cs Documentation
**Updated XML comments:**
- `Create render passes from VulkanSettings.RenderPasses[] configuration`
  → `Create render passes from RenderPasses.Configurations static array`
- Updated ownership hierarchy documentation
- Updated initialization sequence documentation

### 3. RenderableBase.cs

#### Constructor Simplification
**Before:**
```csharp
private readonly VulkanSettings _vulkanSettings;

protected RenderableBase(IOptions<VulkanSettings> vulkanSettings)
{
    _vulkanSettings = vulkanSettings.Value;
}
```

**After:**
```csharp
protected RenderableBase()
{
}
```

#### ComputeRenderMaskFromNames() Method
**Before:**
```csharp
private uint ComputeRenderMaskFromNames(string[] passNames)
{
    foreach (var passName in passNames)
    {
        var index = Array.FindIndex(_vulkanSettings.RenderPasses, p => p.Name == passName);
        if (index >= 0)
        {
            mask |= (1u << index);
        }
    }
}
```

**After:**
```csharp
private uint ComputeRenderMaskFromNames(string[] passNames)
{
    foreach (var passName in passNames)
    {
        // Use reflection to get constant value from static RenderPasses class
        var field = typeof(RenderPasses).GetField(passName, 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        
        if (field?.GetValue(null) is uint passValue)
        {
            mask |= passValue;
            Logger?.LogTrace("Found render pass constant '{PassName}': 0x{Value:X}", passName, passValue);
        }
    }
}
```

**Improvements:**
- Now supports both individual passes (e.g., "Main", "Shadow") and combined masks (e.g., "All", "Opaque")
- Direct access to static constants via reflection
- Better error messages listing available constants
- No dependency injection needed

### 4. Derived Classes - Constructor Updates

All classes inheriting from `RenderableBase` were updated to remove `IOptions<VulkanSettings>` parameter:

#### HelloQuadTestComponent.cs
```csharp
// BEFORE
public class HelloQuad(
    IOptions<VulkanSettings> vulkanSettings,
    IGraphicsContext context,
    IPipelineManager pipelineManager)
    : RenderableBase(vulkanSettings), IRenderable, ITestComponent

// AFTER
public class HelloQuad(
    IGraphicsContext context,
    IPipelineManager pipelineManager)
    : RenderableBase(), IRenderable, ITestComponent
```

#### RenderableTestComponent.cs
```csharp
// BEFORE
public class RenderableTestComponent(IOptions<VulkanSettings> vulkanSettings)
    : RenderableBase(vulkanSettings), IRenderable, ITestComponent

// AFTER
public class RenderableTestComponent()
    : RenderableBase(), IRenderable, ITestComponent
```

#### ValidationTestComponent.cs
```csharp
// BEFORE
public class ValidationTestComponent(IGraphicsContext context, IOptions<VulkanSettings> vulkanSettings)
    : RenderableBase(vulkanSettings), IRenderable, ITestComponent

// AFTER
public class ValidationTestComponent(IGraphicsContext context)
    : RenderableBase(), IRenderable, ITestComponent
```

#### TextElement.cs (GUI Component)
```csharp
// BEFORE
public partial class TextElement(IOptions<VulkanSettings> vulkanSettings)
    : RenderableBase(vulkanSettings), IRenderable, ITextController

// AFTER
public partial class TextElement()
    : RenderableBase(), IRenderable, ITextController
```

#### BackgroundLayer.cs (GUI Component)
```csharp
// BEFORE
public partial class BackgroundLayer(IOptions<VulkanSettings> vulkanSettings)
    : RenderableBase(vulkanSettings), IRenderable, IBackgroundController

// AFTER
public partial class BackgroundLayer()
    : RenderableBase(), IRenderable, IBackgroundController
```

### 5. Using Directive Cleanup

Removed unnecessary `using Microsoft.Extensions.Options;` from:
- `HelloQuadTestComponent.cs`
- `RenderableTestComponent.cs`
- `ValidationTestComponent.cs`
- `TextElement.cs`
- `BackgroundLayer.cs`
- `RenderableBase.cs`

## Usage Examples

### How Components Specify Render Passes

#### Option 1: Named Passes (Preferred for Readability)
```csharp
public new record Template : RenderableBase.Template
{
    public string[]? RenderPasses { get; init; } = ["Main"];
}
```

Supported names:
- Individual: `"Shadow"`, `"Depth"`, `"Background"`, `"Main"`, `"Lighting"`, `"Transparent"`, `"PostProcess"`, `"Sky"`, `"Particles"`, `"UI"`, `"Debug"`
- Combined: `"All"`, `"Opaque"`, `"AlphaBlended"`, `"Scene"`

#### Option 2: Explicit Bit Mask (Preferred for Performance)
```csharp
public new record Template : RenderableBase.Template
{
    public uint? RenderMask { get; init; } = RenderPasses.Main;
}
```

Or combine multiple passes:
```csharp
RenderMask = RenderPasses.Shadow | RenderPasses.Main;
```

#### Option 3: Override GetDefaultRenderMask()
```csharp
protected override uint GetDefaultRenderMask()
{
    return RenderPasses.UI; // This component only renders in UI pass
}
```

## Benefits of This Cleanup

### 1. **Simplified Configuration**
- No need to configure render passes in VulkanSettings
- All pass definitions in one place: `RenderPasses.Configurations`
- Consistent pass definitions across all instances

### 2. **Reduced Dependencies**
- `RenderableBase` no longer depends on `IOptions<VulkanSettings>`
- Fewer constructor parameters for derived classes
- Cleaner dependency injection

### 3. **Type Safety**
- Using constants like `RenderPasses.Main` provides compile-time checking
- Reflection-based name lookup still supports string-based templates
- Better IntelliSense support

### 4. **Performance**
- No runtime lookup of pass indices from configuration
- Direct constant values
- Reflection only used during component configuration, not per-frame

### 5. **Maintainability**
- Single source of truth for render pass definitions
- Easier to add/remove/modify passes
- Clear separation of concerns (settings vs constants)

## Testing

### Build Verification
✅ **PASSED** - `dotnet build Nexus.GameEngine.sln --configuration Debug`

### Manual Testing Required
1. Launch TestApp
2. Verify components render correctly
3. Check that render pass names resolve properly in templates
4. Test both explicit RenderMask and named RenderPasses in templates

### Unit Tests Needed
1. Test `RenderableBase.ComputeRenderMaskFromNames()` with:
   - Valid single pass name: `["Main"]` → `0x00000008`
   - Valid multiple passes: `["Shadow", "Main"]` → `0x00000009`
   - Combined mask: `["All"]` → `0x000007FF`
   - Invalid pass name: `["Invalid"]` → `0x00000000` (with warning log)
   
2. Test template configuration:
   - Explicit RenderMask takes priority
   - Named RenderPasses used if RenderMask not set
   - GetDefaultRenderMask() used if neither set

## Architecture Notes

### Reflection Usage
The `ComputeRenderMaskFromNames()` method uses reflection to look up static field values:
```csharp
var field = typeof(RenderPasses).GetField(passName, 
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

if (field?.GetValue(null) is uint passValue)
{
    mask |= passValue;
}
```

**Performance Impact:**
- Reflection only during component configuration (OnConfigure)
- Not called per-frame
- Cached in `_cachedRenderMask` field
- Acceptable overhead for configuration phase

**Alternative Approaches Considered:**
1. **Dictionary lookup:** Would require maintaining separate dictionary
2. **Switch statement:** Would require updating for each new pass
3. **String parsing:** Error-prone and inflexible

**Chosen Approach:** Reflection provides best balance of:
- Automatic synchronization with constants
- No manual maintenance
- Acceptable performance for configuration code

### Future Enhancements

1. **Compile-Time Validation:**
   - Source generator could validate pass names in templates
   - Generate warnings for invalid pass names
   - Autocomplete for pass names in templates

2. **Pass Dependencies:**
   - Could add validation that dependent passes are enabled
   - E.g., "Lighting" pass requires "Depth" pass
   - Automatic dependency resolution

3. **Dynamic Pass Configuration:**
   - Could add ability to enable/disable passes at runtime
   - Quality settings could control active passes
   - Debug builds could enable Debug pass automatically

## Files Modified

- ✅ `src/GameEngine/Graphics/VulkanSettings.cs`
- ✅ `src/GameEngine/Graphics/SwapChain.cs`
- ✅ `src/GameEngine/Graphics/RenderableBase.cs`
- ✅ `TestApp/TestComponents/HelloQuadTestComponent.cs`
- ✅ `TestApp/TestComponents/RenderableTestComponent.cs`
- ✅ `TestApp/TestComponents/ValidationTestComponent.cs`
- ✅ `src/GameEngine/GUI/Components/TextElement.cs`
- ✅ `src/GameEngine/GUI/Components/BackgroundLayer.cs`

## Completion Status

- ✅ Remove VulkanSettings.RenderPasses property
- ✅ Update SwapChain documentation
- ✅ Simplify RenderableBase constructor
- ✅ Update ComputeRenderMaskFromNames to use reflection
- ✅ Update all derived classes
- ✅ Remove unused using directives
- ✅ Build verification passed
- ⏭️ Manual testing (next step)
- ⏭️ Unit test creation (next step)
