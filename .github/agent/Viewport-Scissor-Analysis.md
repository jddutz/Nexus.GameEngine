# Vulkan Viewport vs Game Engine Viewport - Architecture Analysis

## The Problem

Vulkan validation error indicates dynamic scissor state is enabled but never set:
```
vkCmdDraw(): Dynamic scissor(s) (0x1) are used by pipeline state object, 
but were not provided via calls to vkCmdSetScissor()
```

## Two Different Concepts: Same Name

### 1. **Vulkan Viewport** (Graphics API Concept)
**Purpose:** Transformation from normalized device coordinates (NDC) to framebuffer coordinates.

**Properties:**
- `x, y` - Pixel offset in framebuffer
- `width, height` - Pixel dimensions in framebuffer  
- `minDepth, maxDepth` - Depth range mapping (typically 0.0 to 1.0)

**When Set:** 
- During command buffer recording
- Via `vkCmdSetViewport()` for dynamic state
- Or baked into pipeline for static state

**Usage Pattern:**
```cpp
// Vulkan viewport transforms clip space [-1,1] to screen pixels
VkViewport viewport = {
    .x = 0.0f,
    .y = 0.0f,
    .width = 1920.0f,      // Actual pixel dimensions
    .height = 1080.0f,
    .minDepth = 0.0f,
    .maxDepth = 1.0f
};
vkCmdSetViewport(cmd, 0, 1, &viewport);
```

### 2. **Game Engine Viewport** (High-Level Scene Management)
**Purpose:** Logical rendering region with camera and content tree.

**Properties:**
- `X, Y` - Normalized position (0.0 to 1.0) relative to window
- `Width, Height` - Normalized size (0.0 to 1.0) of window
- `Camera` - View/projection matrices for 3D transformation
- `Content` - Root component tree to render
- `BackgroundColor` - Clear color

**When Created:**
- At application startup via `IContentManager.Viewport`
- Potentially multiple viewports in future (split-screen, minimaps, UI panels)

**Current Implementation:**
```csharp
public interface IViewport : IRuntimeComponent
{
    ICamera? Camera { get; set; }
    IRuntimeComponent? Content { get; set; }
    float X { get; set; }        // Normalized 0.0-1.0
    float Y { get; set; }        // Normalized 0.0-1.0  
    float Width { get; set; }    // Normalized 0.0-1.0
    float Height { get; set; }   // Normalized 0.0-1.0
    Vector4D<float> BackgroundColor { get; set; }
}
```

## Vulkan Scissor Rectangle

**Purpose:** Clipping region - fragments outside scissor are discarded.

**Properties:**
- `offset.x, offset.y` - Pixel offset in framebuffer
- `extent.width, extent.height` - Pixel dimensions

**Relationship to Viewport:**
- Usually matches viewport dimensions
- Can be smaller to clip rendering to sub-region
- Must be set when `VK_DYNAMIC_STATE_SCISSOR` is enabled

**Usage Pattern:**
```cpp
VkRect2D scissor = {
    .offset = {0, 0},
    .extent = {1920, 1080}  // Same as viewport
};
vkCmdSetScissor(cmd, 0, 1, &scissor);
```

## Current Architecture Issues

### Issue 1: Dynamic State Always Enabled
**Location:** `PipelineManager.cs` lines 440-455
```csharp
// Dynamic state - ALWAYS sets viewport and scissor as dynamic
var dynamicStates = stackalloc DynamicState[2];
dynamicStates[0] = DynamicState.Viewport;
dynamicStates[1] = DynamicState.Scissor;
```

**Problem:** All pipelines require viewport/scissor to be set per-frame, but:
- Currently only 1 fullscreen viewport exists
- Dimensions only change on window resize
- Setting per-frame is unnecessary overhead

### Issue 2: Viewport/Scissor Never Set
**Location:** `Renderer.cs` Draw() method
```csharp
private void Draw(CommandBuffer commandBuffer, DrawCommand drawCommand)
{
    context.VulkanApi.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, drawCommand.Pipeline);
    
    var vertexBuffers = stackalloc Silk.NET.Vulkan.Buffer[] { drawCommand.VertexBuffer };
    var offsets = stackalloc ulong[] { 0 };
    context.VulkanApi.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
    
    context.VulkanApi.CmdDraw(commandBuffer, drawCommand.VertexCount, drawCommand.InstanceCount, drawCommand.FirstVertex, 0);
    // ❌ Missing: vkCmdSetViewport() and vkCmdSetScissor()
}
```

## Solution Options

### Option A: Set Once Per Render Pass (Immediate Fix)
**When:** After `vkCmdBeginRenderPass()`, before first draw
**Where:** `Renderer.cs` in render pass loop (lines ~180)

**Pros:**
- Minimal code change
- Handles current single-viewport case
- Efficient (set once per pass)

**Cons:**  
- Hardcoded to swapchain dimensions
- Doesn't leverage `IViewport` properties
- Won't scale to multiple viewports

**Implementation:**
```csharp
context.VulkanApi.CmdBeginRenderPass(cmd, &renderPassInfo, SubpassContents.Inline);

// Set viewport for this render pass
var viewport = new Silk.NET.Vulkan.Viewport
{
    X = 0.0f,
    Y = 0.0f,
    Width = swapChain.SwapchainExtent.Width,
    Height = swapChain.SwapchainExtent.Height,
    MinDepth = 0.0f,
    MaxDepth = 1.0f
};
context.VulkanApi.CmdSetViewport(cmd, 0, 1, &viewport);

var scissor = new Rect2D
{
    Offset = new Offset2D(0, 0),
    Extent = swapChain.SwapchainExtent
};
context.VulkanApi.CmdSetScissor(cmd, 0, 1, &scissor);
```

### Option B: Viewport-Aware Rendering (Future-Proof)
**When:** During render pass creation
**Where:** New method to convert `IViewport` properties to Vulkan viewport

**Pros:**
- Respects `IViewport.X/Y/Width/Height` properties
- Enables split-screen, minimaps, picture-in-picture
- Proper architectural separation

**Cons:**
- More complex implementation
- Requires viewport-to-pixel conversion
- May need scissor optimization per-viewport

**Implementation Sketch:**
```csharp
private (Silk.NET.Vulkan.Viewport, Rect2D) CreateVulkanViewport(IViewport gameViewport)
{
    var swapExtent = swapChain.SwapchainExtent;
    
    // Convert normalized coordinates to pixels
    var pixelX = gameViewport.X * swapExtent.Width;
    var pixelY = gameViewport.Y * swapExtent.Height;
    var pixelWidth = gameViewport.Width * swapExtent.Width;
    var pixelHeight = gameViewport.Height * swapExtent.Height;
    
    var vkViewport = new Silk.NET.Vulkan.Viewport
    {
        X = pixelX,
        Y = pixelY,
        Width = pixelWidth,
        Height = pixelHeight,
        MinDepth = 0.0f,
        MaxDepth = 1.0f
    };
    
    var scissor = new Rect2D
    {
        Offset = new Offset2D((int)pixelX, (int)pixelY),
        Extent = new Extent2D((uint)pixelWidth, (uint)pixelHeight)
    };
    
    return (vkViewport, scissor);
}
```

Then in render loop:
```csharp
context.VulkanApi.CmdBeginRenderPass(cmd, &renderPassInfo, SubpassContents.Inline);

var (vkViewport, scissor) = CreateVulkanViewport(contentManager.Viewport);
context.VulkanApi.CmdSetViewport(cmd, 0, 1, &vkViewport);
context.VulkanApi.CmdSetScissor(cmd, 0, 1, &scissor);
```

### Option C: Static Viewport State (Optimization)
**When:** Pipeline creation
**Where:** `PipelineManager.cs` - make viewport/scissor static instead of dynamic

**Pros:**
- Zero per-frame overhead
- Simplest solution for single fullscreen viewport
- Matches current usage pattern

**Cons:**
- Must recreate pipelines on window resize
- Blocks future multi-viewport features
- Less flexible for dynamic viewport changes

**Implementation:**
Remove dynamic state, add static viewport to pipeline creation:
```csharp
// In PipelineDescriptor
public Silk.NET.Vulkan.Viewport? StaticViewport { get; init; }
public Rect2D? StaticScissor { get; init; }

// In CreatePipeline()
var viewportState = new PipelineViewportStateCreateInfo
{
    SType = StructureType.PipelineViewportStateCreateInfo,
    ViewportCount = 1,
    PViewports = &viewport,  // Actual viewport, not null
    ScissorCount = 1,
    PScissors = &scissor     // Actual scissor, not null
};

// Remove viewport/scissor from dynamic states
var dynamicStates = stackalloc DynamicState[0];  // Empty or other states
```

## Recommended Solution

### Phase 1: **Option A** (Immediate)
Fix the validation error with minimal risk:
- Set viewport/scissor once per render pass
- Use full swapchain dimensions
- Simple, safe, testable

### Phase 2: **Option B** (Near Future)  
Enable proper multi-viewport support:
- Honor `IViewport.X/Y/Width/Height` properties
- Support split-screen rendering
- Add viewport-to-pixel conversion helper

### Phase 3: **Option C** (Optimization)
When viewport usage patterns are stable:
- Profile dynamic vs static viewport overhead
- Consider static viewports for fullscreen-only pipelines
- Keep dynamic viewports for UI/overlay pipelines

## Frequency of Changes

### Q: Does viewport/scissor need to be set per-frame?

**A: No, only when these change:**

1. **Window Resize** (rare)
   - Swapchain is recreated
   - New dimensions available
   
2. **Viewport Configuration Change** (rare)
   - User changes split-screen layout
   - Minimap toggled on/off
   - Picture-in-picture mode
   
3. **Multi-Viewport Rendering** (per-viewport, not per-frame)
   - Render main viewport
   - Render minimap viewport  
   - Render UI overlay viewport
   - Each viewport sets once before its content

**Current Reality:**
- Single fullscreen viewport
- Only changes on window resize (maybe 1-2 times per session)
- Setting per-frame is ~60 FPS * 2 calls = 120 unnecessary API calls/sec

**Vulkan Best Practice:**
Set viewport/scissor once per render pass, not per draw call.

## Multi-Viewport Future Scenarios

### Split-Screen Co-op
```csharp
// Player 1 viewport (left half)
var viewport1 = new Viewport.Template
{
    X = 0.0f, Y = 0.0f, Width = 0.5f, Height = 1.0f,
    Camera = player1Camera,
    Content = gameWorld
};

// Player 2 viewport (right half)
var viewport2 = new Viewport.Template
{
    X = 0.5f, Y = 0.0f, Width = 0.5f, Height = 1.0f,
    Camera = player2Camera,
    Content = gameWorld
};
```

### Minimap Overlay
```csharp
// Main viewport (fullscreen)
var mainViewport = new Viewport.Template
{
    X = 0.0f, Y = 0.0f, Width = 1.0f, Height = 1.0f,
    Camera = playerCamera,
    Content = gameWorld
};

// Minimap (top-right corner, 20% size)
var minimapViewport = new Viewport.Template
{
    X = 0.75f, Y = 0.05f, Width = 0.2f, Height = 0.2f,
    Camera = topDownCamera,
    Content = minimapContent
};
```

### Implementation Considerations for Multi-Viewport

**Render Pass Strategy:**
- Option A: One render pass, multiple viewport switches
  - `vkCmdSetViewport()` between viewport renders
  - Efficient for same render pass configuration
  
- Option B: Multiple render passes, one per viewport
  - Each render pass has its own framebuffer region
  - Better for different clear colors, depth buffers
  
**Current Limitation:**
`Renderer.cs` assumes single viewport in `contentManager.Viewport`

**Required Changes:**
```csharp
// IContentManager would need:
IReadOnlyList<IViewport> Viewports { get; }

// Renderer.OnRender() would iterate:
foreach (var viewport in contentManager.Viewports)
{
    var (vkViewport, scissor) = CreateVulkanViewport(viewport);
    context.VulkanApi.CmdSetViewport(cmd, 0, 1, &vkViewport);
    context.VulkanApi.CmdSetScissor(cmd, 0, 1, &scissor);
    
    // Render viewport.Content with viewport.Camera
    RenderViewportContent(cmd, viewport);
}
```

## Conclusion

**Immediate Action:**
Implement **Option A** to fix validation error. This is a 5-line change in `Renderer.cs`.

**Architectural Decision:**
Keep dynamic viewport/scissor state for future flexibility, but set once per render pass (not per draw call).

**Future Work:**
- Design multi-viewport API in `IContentManager`  
- Implement viewport-to-pixel conversion helper
- Add viewport switching to render loop
- Consider static viewports as optimization later

**Where to Set:**
```
✅ Renderer.cs after vkCmdBeginRenderPass() - Best practice location
❌ Per draw call in Draw() - Too frequent, unnecessary overhead
❌ HelloQuadTestComponent.OnActivate() - Wrong abstraction level (test component shouldn't touch Vulkan state)
❌ Pipeline creation as static state - Blocks multi-viewport future
```
