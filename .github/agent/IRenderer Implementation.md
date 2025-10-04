# IRenderer Implementation

We're still working through some major issues with the rendering system. For now, we're simply writing this implementation plan, do not update any code yet.

I reset the entire Renderer.RenderFrame implementation to mirror the Silk.NET HelloQuad example. We now have an orange quad drawn on the screen.

We are going to evolve this code by making very small incremental changes toward a fully component-based rendering system one step at a time.

Before we can implement any changes, we need to define the end goal. Let's start by assessing the current state and then discuss what the preferred state should be. We likely won't be able to completely define the preferred state all at once. We can update it incrementally as we proceed through the implementation plan.

## Current State

In Application.Window_OnLoad() we currently call the following methods:

```csharp
   renderer.OnLoad();
```

Application.Window_OnLoad() is called when the application window is first created. This is where we set up the renderer (we need GL)

**Issues with the current state**:

Renderer.OnLoad is a copy of the example code with minimal changes to get it to work. It is just setting GL state to render. The example is static - it never changes. This is to keep the example as simple as possible, but we need quite a bit more functionality than this. Calling this method on startup feels unnecessary.

## Preferred State

# IRenderer / Renderer

The Renderer is responsible for orchestration of IWindow.Render events.

# IViewport / Viewport

A Viewport defines how a component tree is rendered.

# IBatchStrategy / DefaultBatchStrategy

Sorts GL state information into batches. Batches are automatically detected by the renderer by checking GLState properties such as Framebuffer or Shader as each state is processed.

# RenderFrame Execution

1. **Execute pre-render middleware**
2. **Walk component tree once** (leaf to root traversal):
   - Discover all `IViewport` components in the tree
   - For each viewport found:
     - Apply viewport's deferred updates
     - For each render pass in viewport:
       - Collect `GLState` objects from viewport's content tree
       - Tag render states with viewport context (framebuffer, screen region, etc.)
3. **Batch and sort all render states** using `IBatchStrategy`:
   - Strategy groups by framebuffer target, shader, render priority, etc.
   - Natural viewport separation occurs because each viewport has different GL state requirements
   - Results in batches that are effectively viewport-specific without explicit viewport knowledge
4. **Execute batched render states**:
   - For each batch, apply GL state changes only when different from current state
   - Render all states in the batch
   - Batch boundaries naturally handle viewport transitions
5. **Execute post-render middleware**
6. **Present frame** (handled by Silk.NET)
