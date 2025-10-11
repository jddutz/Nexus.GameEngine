# VulkanContext Architecture Diagram

## Dependency Graph

```
┌─────────────────────────────────────────────────────────────┐
│                    ServiceCollection                         │
│                   (DI Container)                             │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ Registers Singletons
                              ▼
        ┌─────────────────────────────────────────┐
        │                                         │
        │  IWindowService (Singleton)             │
        │  └─> WindowService                      │
        │       Creates IWindow                   │
        │                                         │
        └────────────────┬────────────────────────┘
                         │
                         │ Injected into
                         ▼
        ┌─────────────────────────────────────────┐
        │                                         │
        │  VulkanContext (Singleton)              │
        │  └─> Lazy initialization               │
        │      on first property access           │
        │                                         │
        └────────────────┬────────────────────────┘
                         │
                         │ Injected into
                         ▼
        ┌─────────────────────────────────────────┐
        │                                         │
        │  IRenderer (Singleton)                  │
        │  └─> Renderer                           │
        │      Uses VulkanContext for rendering   │
        │                                         │
        └─────────────────────────────────────────┘
```

## Initialization Sequence

```
┌─────────────┐
│   Startup   │
└──────┬──────┘
       │
       │ 1. Build DI Container
       ▼
┌─────────────────────┐
│ Register Services   │
│ - WindowService     │
│ - VulkanContext     │───────► NOT initialized yet
│ - Renderer          │
└──────┬──────────────┘
       │
       │ 2. Application.Run()
       ▼
┌─────────────────────┐
│  Window.Create()    │
│  via WindowService  │
└──────┬──────────────┘
       │
       │ 3. Window.Load event
       ▼
┌─────────────────────┐
│ Create startup      │
│ content             │
└──────┬──────────────┘
       │
       │ 4. First render frame
       ▼
┌─────────────────────┐
│ Renderer.OnRender() │
└──────┬──────────────┘
       │
       │ 5. Access VulkanContext property
       ▼
┌─────────────────────┐
│ VulkanContext.      │
│ EnsureInitialized() │───────► INITIALIZES HERE
└──────┬──────────────┘
       │
       │ 6. Full initialization
       ▼
┌─────────────────────────────────────┐
│  VulkanContext fully initialized    │
│  ✓ Instance created                 │
│  ✓ Surface created from window      │
│  ✓ Physical device selected         │
│  ✓ Logical device created           │
│  ✓ Queue handles retrieved          │
└──────┬──────────────────────────────┘
       │
       │ 7. Ready for rendering
       ▼
┌─────────────────────┐
│  Render frames...   │
└─────────────────────┘
```

## Thread-Safe Initialization

```
Thread 1                    Thread 2
   │                           │
   │ Access VulkanContext.Vk   │ Access VulkanContext.Device
   ▼                           ▼
EnsureInitialized()         EnsureInitialized()
   │                           │
   ├─> if (_initialized)       ├─> if (_initialized)
   │   return; ✓ FAST PATH     │   return; ✓ FAST PATH
   │                           │
   ├─> lock (_initLock) {      ├─> lock (_initLock) {
   │   if (_initialized)       │   Wait for lock...
   │     return;               │   Get lock
   │                           │   if (_initialized)
   │   // Initialize...        │     return; ✓ ALREADY DONE
   │   _initialized = true;    │   }
   │   }                       │
   ▼                           ▼
  Done                        Done
```

## Property Access Flow

```
Component calls: renderer.VulkanContext.Device
                                        │
                                        ▼
                            ┌───────────────────────┐
                            │ VulkanContext.Device  │
                            │ {                     │
                            │   get                 │
                            │   {                   │
                            └────────┬──────────────┘
                                     │
                                     ▼
                            ┌───────────────────────┐
                            │  EnsureInitialized(); │
                            └────────┬──────────────┘
                                     │
                      ┌──────────────┴──────────────┐
                      │                             │
              Already init?                   Not initialized
                  (fast)                          (slow)
                      │                             │
                      │                             ▼
                      │              ┌────────────────────────┐
                      │              │ lock (_initLock)       │
                      │              │ {                      │
                      │              │   CreateInstance();    │
                      │              │   CreateSurface();     │
                      │              │   SelectDevice();      │
                      │              │   CreateQueues();      │
                      │              │   _initialized = true; │
                      │              │ }                      │
                      │              └────────┬───────────────┘
                      │                       │
                      └───────────┬───────────┘
                                  │
                                  ▼
                      ┌───────────────────────┐
                      │  return _device;      │
                      └───────────────────────┘
```

## Lifecycle Management

```
┌──────────────────────────────────────────────────────────┐
│                   Application Lifecycle                   │
└──────────────────────────────────────────────────────────┘
        │
        │ Startup
        ▼
┌─────────────────┐
│  DI Container   │
│  registers      │
│  VulkanContext  │
│  as Singleton   │
└────────┬────────┘
         │
         │ Instance created (constructor)
         ▼
┌─────────────────┐
│  VulkanContext  │
│  ._windowService│─────► Holds reference to WindowService
│  ._vk = GetApi()│─────► Loads Vulkan API
│  (not init)     │
└────────┬────────┘
         │
         │ First property access
         ▼
┌─────────────────┐
│ EnsureInit()    │
│ - Instance      │
│ - Surface       │
│ - Device        │
│ - Queues        │
└────────┬────────┘
         │
         │ Active lifetime (rendering)
         ▼
┌─────────────────┐
│  Render frames  │
│  using Vulkan   │
│  resources      │
└────────┬────────┘
         │
         │ Shutdown
         ▼
┌─────────────────┐
│  DI Container   │
│  disposes       │
│  singleton      │
└────────┬────────┘
         │
         │ VulkanContext.Dispose()
         ▼
┌─────────────────┐
│  Cleanup in     │
│  reverse order: │
│  - Device       │
│  - Instance     │
│  - Vk API       │
└─────────────────┘
```

## Benefits Visualization

```
┌─────────────────────────────────────────────────────────────┐
│                    OLD PATTERN (Broken)                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  VulkanContext ctx = new VulkanContext();                   │
│    └─> Partial init (Instance only)                         │
│                                                              │
│  // Window created...                                       │
│                                                              │
│  ctx.InitializeWithSurface(handle); ❌ NEVER CALLED         │
│                                                              │
│  ctx.Device.Handle  ❌ CRASH - Device not initialized       │
│                                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    NEW PATTERN (Works!)                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  [Service registered as singleton with DI]                  │
│                                                              │
│  VulkanContext ctx = services.Get<VulkanContext>();         │
│    └─> Constructor called, not yet initialized              │
│                                                              │
│  // Window created...                                       │
│                                                              │
│  var device = ctx.Device; ✅ Auto-initializes              │
│    └─> EnsureInitialized() called                          │
│    └─> Gets window from WindowService                       │
│    └─> Creates surface, device, queues                      │
│    └─> Returns fully initialized device                     │
│                                                              │
│  device.Handle  ✅ SUCCESS - Everything ready               │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Key Design Decisions

### 1. Lazy Initialization ✅

- **Why**: Window must exist before Vulkan surface creation
- **Benefit**: No timing dependencies in DI registration
- **Trade-off**: Small one-time cost on first access

### 2. Thread-Safe Double-Check Locking ✅

- **Why**: Multiple threads might access properties simultaneously
- **Benefit**: Fast path for already-initialized case
- **Trade-off**: Slight complexity in implementation

### 3. Dependency Injection ✅

- **Why**: Clear dependencies, testable, lifecycle managed
- **Benefit**: Compiler-enforced availability, easy testing
- **Trade-off**: None - pure improvement

### 4. Singleton Lifetime ✅

- **Why**: One Vulkan context per application
- **Benefit**: Shared resources, centralized state
- **Trade-off**: None for this use case

### 5. Property-Based Initialization ✅

- **Why**: Any property access triggers initialization
- **Benefit**: Can't accidentally use uninitialized context
- **Trade-off**: Every property has a getter method (not auto-property)
