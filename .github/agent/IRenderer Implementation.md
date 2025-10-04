# IRenderer Implementation

We're working through some bugs in the rendering system.

I reset the entire Renderer.RenderFrame implementation to the Silk.NET HelloQuad example. We now have an orange quad drawn on the screen.

We are going to make very small incremental changes to this code to evolve it to fully support our component-based rendering system.

In Application.Window_OnLoad() we call the following methods:

```csharp
        renderer.Viewport.Activate();
        renderer.OnLoad();
```

OnLoad is a copy of the example code with minimal changes to get it to work.

# IRenderer / Renderer

The Renderer is responsible for orchestration of IWindow.Render events.

# IViewport / Viewport

A Viewport defines how a component tree is rendered.

# RenderFrame Execution

1. **Execute pre-render middleware**
2. **Walk component tree once** (leaf to root traversal):
   - Discover all `IViewport` components in the tree
   - For each viewport found:
     - Apply viewport's deferred updates
     - For each render pass in viewport:
       - Collect `RenderState` objects from viewport's content tree
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
