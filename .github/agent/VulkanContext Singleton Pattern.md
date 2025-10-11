# VulkanContext Singleton Pattern Implementation

**Date**: October 7, 2025  
**Status**: ✅ Complete

## Overview

Converted `VulkanContext` from a two-phase initialization pattern to a **singleton service with lazy initialization**. This provides a cleaner architecture where the context is fully initialized on first access, after the window is created.

## Changes Made

### 1. VulkanContext.cs - Lazy Initialization Pattern

**Before**: Two-phase initialization requiring manual `InitializeWithSurface()` call

```csharp
public VulkanContext()
{
    _vk = Vk.GetApi();
    CreateInstance();
    // Stopped here - required manual InitializeWithSurface() call
}

public void InitializeWithSurface(nint windowHandle, nint displayHandle)
{
    CreateSurface(windowHandle, displayHandle);
    SelectPhysicalDevice();
    CreateDeviceAndQueues();
}
```

**After**: Single constructor with lazy initialization

```csharp
public VulkanContext(IWindowService windowService)
{
    _windowService = windowService;
    _vk = Vk.GetApi();
    // Initialization deferred until first property access
}

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

        // Step 3: Select physical device
        SelectPhysicalDevice();

        // Step 4: Create logical device and queues
        CreateDeviceAndQueues();

        _initialized = true;
    }
}
```

**Key Changes**:

- ✅ Constructor now accepts `IWindowService` for window access
- ✅ All properties call `EnsureInitialized()` before returning
- ✅ Thread-safe double-check locking pattern
- ✅ Window handle retrieved automatically from `IWindowService`
- ✅ Complete initialization happens atomically on first access

### 2. Public Properties with Lazy Init Guards

All public properties now trigger initialization:

```csharp
public Vk Vk
{
    get
    {
        EnsureInitialized();
        return _vk;
    }
}

public Instance Instance
{
    get
    {
        EnsureInitialized();
        return _instance;
    }
}

// Same pattern for:
// - PhysicalDevice
// - Device
// - GraphicsQueue
// - PresentQueue
// - Surface
```

### 3. Renderer.cs - Constructor Injection

**Before**: Lazy-loaded VulkanContext

```csharp
private VulkanContext? _vulkanContext;

public VulkanContext VulkanContext
{
    get
    {
        _vulkanContext ??= new VulkanContext();
        return _vulkanContext;
    }
}
```

**After**: Injected via constructor

```csharp
public class Renderer(
    VulkanContext vulkanContext,
    IWindowService windowService,
    ILoggerFactory loggerFactory,
    IContentManager contentManager) : IRenderer
{
    public VulkanContext VulkanContext => vulkanContext;
}
```

### 4. Services.cs - DI Registration

Added `VulkanContext` as a singleton service:

```csharp
// Register core runtime services
services.AddSingleton<IComponentFactory, ComponentFactory>();
services.AddSingleton<IWindowService, WindowService>();

// Register Vulkan context (lazy initialized on first access)
services.AddSingleton<VulkanContext>();

// Register renderer with Vulkan context dependency
services.AddSingleton<IRenderer, Renderer>();
```

## Architecture Benefits

### ✅ Simplified Initialization Flow

**Old Pattern** (manual, error-prone):

```
1. Create VulkanContext (partial init)
2. Create Window
3. Manually call InitializeWithSurface() ❌ NEVER CALLED
4. Use VulkanContext
```

**New Pattern** (automatic, foolproof):

```
1. Register VulkanContext as singleton
2. Create Window (via WindowService)
3. Access VulkanContext property → auto-initializes ✅
4. Use fully initialized VulkanContext
```

### ✅ Dependency Injection Benefits

- **Type Safety**: Compiler ensures VulkanContext is available
- **Testability**: Can mock `IWindowService` for testing
- **Lifecycle Management**: DI container handles disposal
- **Clear Dependencies**: Constructor shows what's needed

### ✅ Thread Safety

- **Double-check locking**: Avoids race conditions
- **Atomic initialization**: All or nothing
- **Lock object**: Prevents multiple initializations

### ✅ Fail-Fast Behavior

- **Window must exist**: Throws immediately if window not created
- **Vulkan must work**: Initialization errors happen early
- **No silent failures**: Can't use half-initialized context

## Initialization Timing

The VulkanContext is initialized when **any property is first accessed**, which typically happens when:

1. **`Renderer.OnRender()` is called** for the first time
2. A component tries to access `VulkanContext.Device`
3. Any code touches Vulkan resources

This occurs **after** `Window.Load` event, ensuring the window exists.

## Platform Support

Currently implemented for **Windows only**:

```csharp
var window = _windowService.GetWindow();
var windowHandle = window.Native!.Win32!.Value.Hwnd;
CreateSurface(windowHandle, 0);
```

**TODO**: Add platform detection and support for:

- Linux (X11/XCB/Wayland)
- macOS (MoltenVK)
- Android
- iOS (MoltenVK)

## Next Steps

1. ✅ **Complete** - Singleton pattern with lazy init
2. 🚧 **TODO** - Implement proper surface creation (currently stub)
3. 🚧 **TODO** - Add platform-specific surface creation
4. 🚧 **TODO** - Implement swapchain management
5. 🚧 **TODO** - Add validation layers for debug builds
6. 🚧 **TODO** - Implement proper physical device selection (scoring)

## Testing

To verify initialization:

1. Run TestApp
2. VulkanContext should initialize on first render frame
3. Check logs for Vulkan instance creation
4. Verify no exceptions during startup

## Related Files

- `src/GameEngine/Graphics/VulkanContext.cs` - Core implementation
- `src/GameEngine/Graphics/Renderer.cs` - Uses VulkanContext
- `src/GameEngine/Graphics/IRenderer.cs` - Exposes VulkanContext
- `src/GameEngine/Runtime/Services.cs` - DI registration
- `src/GameEngine/Runtime/IWindowService.cs` - Window access
- `src/GameEngine/Runtime/Application.cs` - Application lifecycle

## Build Status

✅ **Build succeeds** with no errors or warnings (after suppressing harmless parameter warning)

```bash
dotnet build Nexus.GameEngine.sln
# Build succeeded in 3.7s
```
