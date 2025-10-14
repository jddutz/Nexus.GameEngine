# Render Passes - Implementation Plan

**Date:** October 13, 2025  
**Status:** 🔍 Planning Phase

---

## Current State Analysis

### ✅ What We Have
1. **Context** - Device, queues, surface, capability queries
2. **Swapchain** - Swap chain, images, image views (created)
3. **GraphicsSettings** - Configuration for formats, present modes, etc.

### ❌ What We Need
1. **Render Pass** - Defines rendering operations and attachments
2. **Framebuffers** - Binds image views to render pass attachments
3. **Integration** - Connect render pass to swap chain

---

## Conceptual Understanding Review

### Images, ImageViews, Framebuffers, RenderPass

```
┌─────────────┐
│   Image     │  Raw pixel data in GPU memory (owned by swap chain)
│  (2D array) │  Example: 1920×1080 RGBA pixels
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ ImageView   │  "How to interpret this image"
│ (metadata)  │  - Format: BGRA8_SRGB
│             │  - Type: 2D texture
│             │  - Aspect: Color
│             │  - Mip levels, array layers
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Framebuffer │  "These ImageViews map to RenderPass attachments"
│ (bindings)  │  - Attachment 0 → ImageView (swap chain image)
│             │  - Attachment 1 → ImageView (depth buffer, if used)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ RenderPass  │  "Contract: I need these attachment types"
│ (template)  │  - Attachment 0: Color (BGRA8_SRGB, load=clear, store=store)
│             │  - Attachment 1: Depth (D32_FLOAT, load=clear, store=don't care)
│             │  
│             │  Defines: Load ops, store ops, layouts, subpasses
└─────────────┘
```

**Key Insight:** 
- **RenderPass** = Template/contract (what attachments and operations)
- **Framebuffer** = Instance (which actual images to use)
- **ImageView** = Interpretation (how to read/write the image)
- **Image** = Data (the actual pixels)

---

## Architectural Questions

### Q1: Who Creates ImageViews?

**Current State:** Swapchain creates ImageViews in its constructor.

**Analysis:**
```csharp
// Swapchain.cs - Current implementation
private void CreateImageViews()
{
    _swapchainImageViews = new ImageView[_swapchainImages.Length];
    // Creates image views for swap chain images
}
```

**Decision:** ✅ **Keep image view creation in Swapchain**

**Reasoning:**
- ImageViews are tightly coupled to swap chain lifecycle
- When swap chain recreates, image views must recreate
- Swap chain owns the images, so it should own the views
- Single responsibility: "Manage presentation infrastructure"

**Responsibility Boundary:**
- Swapchain: Create image views for **swap chain images** (color attachments)
- Future services: Create image views for **depth buffers, textures, etc.**

---

### Q2: Who Creates Framebuffers?

**Options:**

#### Option A: Swapchain Creates Framebuffers ❌
```csharp
public Swapchain(IGraphicsContext context, IVkRenderPass renderPass, ...)
{
    CreateSwapchain();
    CreateImageViews();
    CreateFramebuffers(renderPass);  // ← Add this
}
```

**Pros:**
- Framebuffers lifecycle matches swap chain
- Recreated together on window resize

**Cons:**
- Swapchain depends on IVkRenderPass
- Circular dependency: RenderPass needs swap chain format, SwapChain needs render pass
- Violates SRP: Framebuffers are render pass concern, not presentation concern

#### Option B: VkRenderPass Creates Framebuffers ❌
```csharp
public VkRenderPass(IGraphicsContext context, ISwapChain swapchain, ...)
{
    CreateRenderPass(swapchain.SwapchainFormat);
    CreateFramebuffers(swapchain.SwapchainImageViews);  // ← Add this
}
```

**Pros:**
- Framebuffers are render pass concern

**Cons:**
- RenderPass depends on specific swap chain
- Can't reuse render pass for offscreen rendering
- Framebuffers need recreation on window resize, but render pass doesn't

#### Option C: Separate VkFramebufferManager ✅ **RECOMMENDED**
```csharp
public interface IVkFramebufferManager
{
    Framebuffer[] CreateFramebuffers(
        RenderPass renderPass, 
        ImageView[] imageViews, 
        Extent2D extent);
    
    void RecreateFramebuffers();
    void Dispose();
}
```

**Pros:**
- ✅ Pure SRP: One responsibility = "Create and manage framebuffers"
- ✅ No circular dependencies
- ✅ Decoupled: Can create framebuffers for any render pass + image views
- ✅ Flexible: Supports offscreen rendering, multiple render passes
- ✅ Testable: Easy to mock and test independently

**Cons:**
- ⚠️ More services to manage (but worth it for clean architecture)

**Decision:** ✅ **Create separate VkFramebufferManager**

---

### Q3: Who Defines the RenderPass Contract?

**Options:**

#### Option A: Single "Main" RenderPass (Tutorial Approach) ⚠️
```csharp
public interface IVkRenderPass
{
    RenderPass RenderPass { get; }  // The one and only render pass
}
```

**Pros:**
- Simple for initial triangle rendering
- Matches tutorial structure

**Cons:**
- Not extensible (what about post-processing? shadows? UI overlay?)
- Hardcodes assumptions about attachments

#### Option B: Named RenderPass Registry ✅ **RECOMMENDED**
```csharp
public interface IVkRenderPass
{
    RenderPass GetRenderPass(string name);  // "main", "shadow", "postprocess", etc.
    RenderPass MainRenderPass { get; }      // Convenience for default
}
```

**Pros:**
- ✅ Extensible: Easy to add new render passes
- ✅ Flexible: Different passes for different purposes
- ✅ Future-proof: Supports deferred rendering, post-processing

**Cons:**
- ⚠️ Slightly more complex initial implementation

**Decision:** ✅ **Use named registry pattern, start with "main" render pass**

---

### Q4: How Does RenderPass Know What Format to Use?

**The Chicken-and-Egg Problem:**
```
RenderPass needs: Swap chain format (for color attachment)
SwapChain needs: Render pass (for framebuffer creation)
```

**Solution: Query Format from Swapchain** ✅

```csharp
public VkRenderPass(IGraphicsContext context, ISwapChain swapchain, ...)
{
    // Query format, don't create dependency
    var format = swapchain.SwapchainFormat;
    CreateRenderPass(format);
}
```

**Key Insight:** Render pass only needs the **format** (metadata), not the swap chain instance itself. This breaks the circular dependency.

---

## Proposed Service Design

### 1. IVkRenderPass (New Service)

**Responsibility:** Define rendering operations and attachment requirements

```csharp
namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages Vulkan render passes that define rendering operations and attachments.
/// </summary>
public interface IVkRenderPass : IDisposable
{
    /// <summary>
    /// Gets the main render pass used for rendering to the swap chain.
    /// </summary>
    RenderPass MainRenderPass { get; }
    
    /// <summary>
    /// Gets a render pass by name.
    /// </summary>
    /// <param name="name">Name of the render pass (e.g., "main", "shadow", "postprocess")</param>
    /// <returns>The render pass handle</returns>
    RenderPass GetRenderPass(string name);
    
    /// <summary>
    /// Gets the format of the main render pass color attachment.
    /// </summary>
    Format ColorAttachmentFormat { get; }
}
```

**Implementation Notes:**
- Constructor parameters: `IGraphicsContext`, `ISwapChain` (for format query), `ILoggerFactory`
- Creates render pass with color attachment matching swap chain format
- Start with single "main" render pass, expand registry later
- No depth buffer initially (add later when needed)

**Lifecycle:**
- Created once at startup
- Never recreated (format doesn't change)
- Disposed at shutdown

**Does NOT:**
- ❌ Create framebuffers
- ❌ Manage image views
- ❌ Store swap chain images

---

### 2. IVkFramebufferManager (New Service)

**Responsibility:** Create and manage framebuffers for render passes

```csharp
namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Manages Vulkan framebuffers that bind image views to render pass attachments.
/// </summary>
public interface IVkFramebufferManager : IDisposable
{
    /// <summary>
    /// Gets the framebuffers for the main render pass (one per swap chain image).
    /// </summary>
    Framebuffer[] Framebuffers { get; }
    
    /// <summary>
    /// Recreates framebuffers (e.g., after swap chain recreation).
    /// </summary>
    void Recreate();
}
```

**Implementation Notes:**
- Constructor parameters: `IGraphicsContext`, `ISwapChain`, `IVkRenderPass`, `ILoggerFactory`
- Creates one framebuffer per swap chain image view
- Framebuffer extent matches swap chain extent
- Automatically recreated when swap chain recreates

**Lifecycle:**
- Created after render pass and swap chain
- Recreated on window resize (via `Recreate()`)
- Disposed at shutdown

**Does NOT:**
- ❌ Create render passes
- ❌ Create image views
- ❌ Manage rendering commands

---

### 3. Swapchain (Existing - Minimal Changes)

**Current Responsibility:** Manage presentation infrastructure

**Changes Needed:**
```csharp
public interface ISwapChain : IDisposable
{
    // Existing properties
    SwapchainKHR Swapchain { get; }
    Format SwapchainFormat { get; }
    Extent2D SwapchainExtent { get; }
    Image[] SwapchainImages { get; }
    ImageView[] SwapchainImageViews { get; }
    
    // REMOVE: Framebuffers property (moved to VkFramebufferManager)
    // Framebuffer[] Framebuffers { get; }  ← DELETE THIS
    
    // Existing methods
    void Recreate();
    uint AcquireNextImage(Semaphore imageAvailableSemaphore, out Result result);
    void Present(uint imageIndex, Semaphore renderFinishedSemaphore);
}
```

**Rationale:** Framebuffers are not presentation infrastructure, they're render pass bindings.

---

## Dependency Graph

```
┌─────────────┐
│  Context  │  Foundation: Device, queues, surface
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Swapchain │  Presentation: Swap chain, images, image views
└──────┬──────┘
       │
       ├─────────────────┐
       │                 │
       ▼                 ▼
┌──────────────┐  ┌─────────────┐
│ VkRenderPass │  │VkFramebuffer│
│              │  │Manager      │
│ Queries      │  │             │
│ format from  │◄─┤ Depends on  │
│ swap chain   │  │ both        │
└──────────────┘  └─────────────┘
```

**Initialization Order:**
1. Context (device, queues, surface)
2. Swapchain (swap chain, images, image views)
3. VkRenderPass (render pass using swap chain format)
4. VkFramebufferManager (framebuffers using render pass + image views)

**On Window Resize:**
1. Swapchain.Recreate() (new images, new image views, new extent)
2. VkFramebufferManager.Recreate() (new framebuffers with new image views)
3. VkRenderPass is NOT recreated (format unchanged)

---

## Implementation Specification

### VkRenderPass.cs

**Constructor:**
```csharp
public VkRenderPass(
    IGraphicsContext context,
    ISwapChain swapchain,
    ILoggerFactory loggerFactory)
{
    _context = context;
    _logger = loggerFactory.CreateLogger(nameof(VkRenderPass));
    
    // Query format from swap chain (no dependency on swap chain instance)
    _colorAttachmentFormat = swapchain.SwapchainFormat;
    
    // Create main render pass
    CreateMainRenderPass();
}
```

**CreateMainRenderPass() Logic:**
```csharp
private void CreateMainRenderPass()
{
    // Color attachment description
    var colorAttachment = new AttachmentDescription
    {
        Format = _colorAttachmentFormat,      // From swap chain
        Samples = SampleCountFlags.Count1Bit, // No MSAA (for now)
        LoadOp = AttachmentLoadOp.Clear,      // Clear framebuffer at start
        StoreOp = AttachmentStoreOp.Store,    // Store result for presentation
        StencilLoadOp = AttachmentLoadOp.DontCare,
        StencilStoreOp = AttachmentStoreOp.DontCare,
        InitialLayout = ImageLayout.Undefined,              // Don't care about previous
        FinalLayout = ImageLayout.PresentSrcKhr             // Ready for presentation
    };
    
    // Attachment reference for subpass
    var colorAttachmentRef = new AttachmentReference
    {
        Attachment = 0,  // Index into attachments array
        Layout = ImageLayout.ColorAttachmentOptimal
    };
    
    // Subpass description
    var subpass = new SubpassDescription
    {
        PipelineBindPoint = PipelineBindPoint.Graphics,
        ColorAttachmentCount = 1,
        PColorAttachments = &colorAttachmentRef
    };
    
    // Subpass dependency (for layout transitions)
    var dependency = new SubpassDependency
    {
        SrcSubpass = Vk.SubpassExternal,
        DstSubpass = 0,
        SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
        SrcAccessMask = 0,
        DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
        DstAccessMask = AccessFlags.ColorAttachmentWriteBit
    };
    
    // Create render pass
    var renderPassInfo = new RenderPassCreateInfo
    {
        SType = StructureType.RenderPassCreateInfo,
        AttachmentCount = 1,
        PAttachments = &colorAttachment,
        SubpassCount = 1,
        PSubpasses = &subpass,
        DependencyCount = 1,
        PDependencies = &dependency
    };
    
    RenderPass renderPass;
    var result = _vk.CreateRenderPass(_context.Device, &renderPassInfo, null, &renderPass);
    
    if (result != Result.Success)
    {
        throw new Exception($"Failed to create render pass: {result}");
    }
    
    _mainRenderPass = renderPass;
}
```

**Properties:**
```csharp
public RenderPass MainRenderPass => _mainRenderPass;
public Format ColorAttachmentFormat => _colorAttachmentFormat;

public RenderPass GetRenderPass(string name)
{
    if (name == "main") return _mainRenderPass;
    throw new ArgumentException($"Unknown render pass: {name}");
}
```

---

### VkFramebufferManager.cs

**Constructor:**
```csharp
public VkFramebufferManager(
    IGraphicsContext context,
    ISwapChain swapchain,
    IVkRenderPass renderPass,
    ILoggerFactory loggerFactory)
{
    _context = context;
    _swapchain = swapchain;
    _renderPass = renderPass;
    _logger = loggerFactory.CreateLogger(nameof(VkFramebufferManager));
    
    CreateFramebuffers();
}
```

**CreateFramebuffers() Logic:**
```csharp
private void CreateFramebuffers()
{
    var imageViews = _swapchain.SwapchainImageViews;
    var extent = _swapchain.SwapchainExtent;
    
    _framebuffers = new Framebuffer[imageViews.Length];
    
    for (int i = 0; i < imageViews.Length; i++)
    {
        var attachments = stackalloc ImageView[] { imageViews[i] };
        
        var framebufferInfo = new FramebufferCreateInfo
        {
            SType = StructureType.FramebufferCreateInfo,
            RenderPass = _renderPass.MainRenderPass,
            AttachmentCount = 1,
            PAttachments = attachments,
            Width = extent.Width,
            Height = extent.Height,
            Layers = 1
        };
        
        Framebuffer framebuffer;
        var result = _vk.CreateFramebuffer(_context.Device, &framebufferInfo, null, &framebuffer);
        
        if (result != Result.Success)
        {
            throw new Exception($"Failed to create framebuffer {i}: {result}");
        }
        
        _framebuffers[i] = framebuffer;
    }
}
```

**Recreate() Logic:**
```csharp
public void Recreate()
{
    _logger.LogDebug("Recreating framebuffers");
    
    // Wait for device idle
    _vk.DeviceWaitIdle(_context.Device);
    
    // Destroy old framebuffers
    CleanupFramebuffers();
    
    // Create new framebuffers
    CreateFramebuffers();
}

private void CleanupFramebuffers()
{
    foreach (var framebuffer in _framebuffers)
    {
        if (framebuffer.Handle != 0)
        {
            _vk.DestroyFramebuffer(_context.Device, framebuffer, null);
        }
    }
    _framebuffers = [];
}
```

---

## Window Resize Handling

**Flow:**
```
Window Resize Event
    ↓
1. Swapchain.Recreate()
   - Destroys old swap chain, image views
   - Creates new swap chain, image views (new extent)
    ↓
2. VkFramebufferManager.Recreate()
   - Destroys old framebuffers
   - Creates new framebuffers (new extent, new image views)
    ↓
3. VkRenderPass unchanged (format didn't change)
```

**Orchestration (Future VkRenderer):**
```csharp
public void HandleWindowResize()
{
    _logger.LogDebug("Handling window resize");
    
    // Wait for device idle
    _vk.DeviceWaitIdle(_context.Device);
    
    // Recreate swap chain
    _swapchain.Recreate();
    
    // Recreate framebuffers
    _framebufferManager.Recreate();
    
    _logger.LogDebug("Window resize complete");
}
```

---

## Service Registration (DI)

```csharp
// Program.cs or service configuration
services.AddSingleton<IGraphicsContext, Context>();
services.AddSingleton<ISwapChain, Swapchain>();
services.AddSingleton<IVkRenderPass, VkRenderPass>();
services.AddSingleton<IVkFramebufferManager, VkFramebufferManager>();
```

**Dependency Injection ensures correct initialization order automatically.**

---

## Testing Strategy

### Unit Tests (with Mocks)

**VkRenderPass:**
- ✅ Creates render pass with correct format
- ✅ Color attachment has correct load/store ops
- ✅ Layout transitions configured correctly
- ✅ GetRenderPass("main") returns main render pass

**VkFramebufferManager:**
- ✅ Creates one framebuffer per swap chain image
- ✅ Framebuffer dimensions match swap chain extent
- ✅ Framebuffers reference correct image views
- ✅ Recreate destroys old and creates new framebuffers

### Integration Tests (with Real Vulkan)

**Full Stack:**
1. Create Context
2. Create Swapchain
3. Create VkRenderPass (verify format matches swap chain)
4. Create VkFramebufferManager (verify framebuffer count matches image count)
5. Simulate window resize
6. Verify framebuffers recreated with new dimensions

---

## Implementation Checklist

### Phase 1: RenderPass (Core Functionality)
- [ ] Create `IVkRenderPass.cs` interface
- [ ] Implement `VkRenderPass.cs`
  - [ ] Constructor with format query
  - [ ] `CreateMainRenderPass()` method
  - [ ] Properties: `MainRenderPass`, `ColorAttachmentFormat`
  - [ ] `GetRenderPass(name)` method (returns "main" for now)
  - [ ] `Dispose()` method
- [ ] Add logging (debug level)
- [ ] Build and verify no errors

### Phase 2: FramebufferManager
- [ ] Create `IVkFramebufferManager.cs` interface
- [ ] Implement `VkFramebufferManager.cs`
  - [ ] Constructor with dependencies
  - [ ] `CreateFramebuffers()` method
  - [ ] `Recreate()` method
  - [ ] `CleanupFramebuffers()` method
  - [ ] `Dispose()` method
- [ ] Add logging (debug level)
- [ ] Build and verify no errors

### Phase 3: Swapchain Integration
- [ ] Remove `Framebuffer[] Framebuffers` property from ISwapChain
- [ ] Remove framebuffer creation logic from Swapchain.cs
- [ ] Update Swapchain.cs comments/documentation
- [ ] Build and verify no errors

### Phase 4: DI Registration
- [ ] Register IVkRenderPass → VkRenderPass
- [ ] Register IVkFramebufferManager → VkFramebufferManager
- [ ] Verify correct initialization order via DI

### Phase 5: Testing
- [ ] Create integration test for full initialization
- [ ] Test window resize flow
- [ ] Verify framebuffer recreation works

### Phase 6: Documentation
- [ ] Update architecture diagrams
- [ ] Document render pass attachment configuration
- [ ] Document framebuffer recreation flow
- [ ] Update #file:Context.cs  checklist (mark Image Views, Render Pass complete)

---

## Success Criteria

✅ **VkRenderPass creates valid render pass with swap chain format**  
✅ **VkFramebufferManager creates framebuffers for all swap chain images**  
✅ **Framebuffers correctly bind image views to render pass**  
✅ **Window resize recreates framebuffers without memory leaks**  
✅ **No circular dependencies**  
✅ **Single Responsibility Principle maintained**  
✅ **Comprehensive logging for diagnostics**  
✅ **Solution builds with no errors**  

---

## Future Enhancements

### Depth Buffering (Next Phase)
- Add depth attachment to render pass
- Create depth image + image view
- Update framebuffer creation to include depth attachment

### Multiple Render Passes (Later)
- Shadow mapping render pass
- Post-processing render passes
- UI overlay render pass
- Expand `GetRenderPass(name)` registry

### MSAA Support (Later)
- Multisampled render pass
- Resolve attachment
- Multisampled framebuffers

---

## Open Questions

### Q1: Should VkFramebufferManager be notified of swap chain recreation?

**Option A: Manual orchestration (recommended for now)**
```csharp
// In renderer or application
swapchain.Recreate();
framebufferManager.Recreate();
```

**Option B: Event-driven**
```csharp
// Swapchain fires event
public event EventHandler<SwapchainRecreatedEventArgs>? SwapchainRecreated;

// VkFramebufferManager subscribes
swapchain.SwapchainRecreated += (s, e) => Recreate();
```

**Decision:** Start with **Option A** (explicit orchestration). Add events later if needed.

### Q2: Should render pass support multiple formats?

**Current:** Single format (from swap chain)  
**Future:** Different render passes may need different formats (HDR, shadows, etc.)

**Decision:** Keep simple for now, extend later when needed.

---

**Status:** Implementation plan complete. Ready for review and execution! 🚀