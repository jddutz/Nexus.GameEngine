# IRenderer Implementation

We're still working through some major issues with the rendering system. For now, we're simply writing this implementation plan. Code should not be updated yet.

I reset the entire Renderer.RenderFrame implementation to mirror the Silk.NET HelloQuad example. We now have an orange quad drawn on the screen.

We are going to evolve this code by making very small incremental changes toward a fully component-based rendering system one step at a time.

Before we can implement any changes, we need to define the end goal. Let's start by assessing the current state and then discuss what the preferred state should be. We likely won't be able to completely define the preferred state all at once. We can update it incrementally as we proceed through the implementation plan.

## Current State

### Application Startup Flow

`Application.Window_OnLoad()` is called when the application window is first created. The current implementation:

```csharp
private void Window_OnLoad()
{
    // Validate startup template
    if (StartupTemplate == null)
    {
        logger.LogError("Application.StartupTemplate is required.");
        window?.Close();
        return;
    }

    // Create content from template
    var content = contentManager.Create(StartupTemplate);
    if (content == null)
    {
        logger.LogError("Failed to create content from StartupTemplate...");
        return;
    }

    // Assign content to renderer's viewport
    renderer.Viewport.Content = content;
    renderer.OnLoad();
}
```

### Renderer.OnLoad() Implementation

`Renderer.OnLoad()` is a direct copy from the Silk.NET HelloQuad example with minimal changes. It performs one-time OpenGL initialization:

- Creates and binds a Vertex Array Object (VAO)
- Creates and populates a Vertex Buffer Object (VBO) with hardcoded vertex data
- Creates and populates an Element Buffer Object (EBO) with hardcoded index data
- Compiles hardcoded vertex and fragment shaders (inline strings)
- Links shaders into a shader program
- Sets up vertex attribute pointers

This initialization is **completely static** - it sets up a single orange quad with no ability to render component trees or handle dynamic content.

### Renderer.OnRender() / RenderFrame Implementation

`Renderer.OnRender(double deltaTime)` fires pre/post render events but the actual rendering happens in `RenderFrame()`:

```csharp
private unsafe void RenderFrame()
{
    // Clear the color channel
    GL.Clear((uint)ClearBufferMask.ColorBufferBit);

    // Bind the geometry and shader
    GL.BindVertexArray(Vao);
    GL.UseProgram(Shader);

    // Draw the geometry
    GL.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
}
```

This is **not component-based** - it just renders the hardcoded quad. The extensive batching infrastructure documented in the Renderer class XML comments is not yet implemented.

### Existing Infrastructure (Not Yet Connected)

The following systems exist but are **not currently used** by the rendering pipeline:

- **`IViewport` / `Viewport`**: Fully implemented with content management, screen regions, cameras, framebuffer targets, render passes, and viewport priority
- **`IBatchStrategy` / `DefaultBatchStrategy`**: Complete batching algorithm that sorts `RenderData` objects by framebuffer, priority, shader program, textures, and VAO
- **`RenderData`**: Render state abstraction with properties for shader, textures, VAO, framebuffer, uniforms, and source viewport
- **Pre/Post Render Events**: `BeforeRenderFrame` and `AfterRenderFrame` events fire but have empty `EventArgs` classes

The Renderer **has** a `Viewport` property and `IBatchStrategy` injected via constructor, but `RenderFrame()` doesn't use either of them yet.

### Issues with Current State

1. **Static Example Code**: `OnLoad()` and `RenderFrame()` are example code, not production rendering
2. **No Component Tree Traversal**: Despite Viewport having a Content property, the renderer never walks it
3. **Hardcoded Geometry**: Vertex data, shaders, and draw calls are hardcoded in the Renderer class
4. **Unused Infrastructure**: Sophisticated batching, viewport, and state management systems exist but are disconnected
5. **No Middleware System**: Pre/post render events exist but no formal middleware interface or implementations
6. **Missing RenderState Collection**: Components can't generate `RenderData` objects - there's no `IRenderable` interface integration

## Preferred State

### Core Problem

The entire rendering system was broken - nothing would render. To fix this, we reset to the Silk.NET HelloQuad example which at least renders something (an orange quad). Now we need to move **incrementally** from this working baseline toward a fully component-based rendering system, testing at each step to ensure rendering continues to work.

### Conceptual Architecture

The rendering system is built on a clear separation of responsibilities:

#### Components Own Their Rendering Resources

**Components are responsible for their own geometry and resources.** Each renderable component manages:

- Its own vertex data and buffer objects
- Shader programs and compilation
- Texture loading and binding
- Uniform values to pass to shaders

Components don't render directly. Instead, they **declare their rendering requirements** by producing `RenderData` objects.

#### RenderData: Declarative Rendering Requirements

`RenderData` is the component's way of saying:

- "Set the target framebuffer to X"
- "Get or compile shader program Y"
- "Buffer these vertices to a VBO"
- "Load and bind these textures"
- "Set these uniform values"
- "Use this VAO for vertex attributes"

This is a **declarative API** - components state what GL state they need, not how to apply it. The renderer handles the actual OpenGL calls.

#### Viewports Define Rendering Output

**Viewports define how to produce the final rendering.** A viewport manages:

- Screen region (where on screen to render)
- Render passes (multiple passes for post-processing effects)
- Framebuffer targets (off-screen rendering)
- Content tree (what components to render in this viewport)
- Render priority (layering multiple viewports)

Viewports orchestrate the "what" and "where" of rendering, but not the "how" - that's the component's job via RenderData.

#### Cameras Define Spatial Transforms

**Cameras define transforms between world space and screen space.** A camera is an object in game world space that defines:

- View point (position in world)
- View direction (where the camera looks)
- Projection (perspective, orthographic, etc.)
- Frustum (what's visible)
- View and projection matrices

Cameras provide the transformation context that components use to position their geometry correctly.

#### The Renderer Orchestrates Everything

The renderer's job is **orchestration and optimization**:

1. Walk the component tree collecting `RenderData` objects from renderable components
2. Sort states using `IBatchStrategy` to minimize expensive OpenGL state changes
3. Apply GL state changes only when batch boundaries are crossed
4. Execute draw calls when state is properly configured

The renderer doesn't know about game logic, geometry, or resources - it only knows how to efficiently apply GL state and coordinate the rendering pipeline.

### RenderFrame Execution Flow

1. **Execute pre-render middleware** (debugging, profiling, state capture)

2. **Walk component tree once** to collect rendering requirements:

   - Discover all `IViewport` components in the tree
   - For each viewport found:
     - Apply viewport's deferred content updates
     - For each render pass in the viewport:
       - Walk viewport's content tree to find `IRenderable` components
       - Call `OnRender()` on each renderable to collect `RenderData` objects
       - Tag each state with viewport context (framebuffer, camera, screen region)

3. **Batch and sort all render states** using `IBatchStrategy`:

   - Strategy sorts by expensive state changes first (framebuffer, shader program, textures, VAO)
   - Groups compatible states together to minimize OpenGL API calls
   - Natural viewport separation occurs because different viewports have different GL state requirements
   - Results in batches that minimize state transitions

4. **Execute batched render states**:

   - For each batch:
     - Query current GL state to avoid redundant API calls
     - Apply framebuffer, shader program, texture, and VAO changes only when needed
     - Set uniforms (they change frequently and don't batch well)
     - Execute draw calls for all states in the batch
   - Batch boundaries naturally handle viewport transitions

5. **Execute post-render middleware** (validation, screenshots, metrics)

6. **Present frame** (swap buffers, handled by Silk.NET)

### Integration: How Components Work Together

A typical rendering scenario shows how these systems integrate:

1. **Scene Setup**: A viewport contains a content tree with a 3D model component and a camera
2. **Component Declares Needs**: The model component's `OnRender()` produces a `RenderData` saying:
   - "Use shader program for PBR materials"
   - "Bind diffuse texture to unit 0, normal map to unit 1"
   - "Set uniforms: model matrix, material properties"
   - "Use VAO with vertex positions, normals, UVs"
3. **Camera Provides Context**: Camera calculates view and projection matrices as uniforms
4. **Viewport Defines Output**: Viewport specifies render target and screen region
5. **Renderer Optimizes**: Batching groups this model with others using the same shader
6. **Draw**: All GL state is applied, uniforms set, draw call executed

This separation allows each system to evolve independently while maintaining a clean, testable architecture.
