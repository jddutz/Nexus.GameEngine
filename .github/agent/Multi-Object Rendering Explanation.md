# Multi-Object Rendering: How it Works

## Current Implementation (Single Object)

Right now, the Renderer has these static variables:

```csharp
private static uint Vbo;   // Vertex Buffer Object
private static uint Ebo;   // Element Buffer Object
private static uint Vao;   // Vertex Array Object
private static uint Shader; // Shader Program
```

These are **shared across all rendering** - we create them once in `OnLoad()` and use them in every `RenderFrame()`. This only works because we're rendering exactly one object (the BasicQuad).

## What Happens with Multiple Objects?

Let's say you have a scene with:

- 10 trees
- 5 rocks
- 1 player character
- 1 ground plane

### Each Object Has Its Own Resources

**Trees:**

- Share the same VAO (tree mesh geometry)
- Share the same Shader (foliage shader)
- Have different VBOs if they're instanced, or share one VBO and render with different transforms

**Rocks:**

- Different VAO (rock mesh geometry)
- Different Shader (rock material shader)
- Different VBO

**Player:**

- Different VAO (character mesh)
- Different Shader (character/skin shader)
- Animated - VBO might update every frame

**Ground:**

- Different VAO (plane mesh)
- Different Shader (terrain shader)
- Massive VBO with terrain data

### The Rendering Process

Here's what happens frame by frame:

#### 1. **Collection Phase** (not yet implemented)

```
Walk component tree → Find all IRenderable components
For each renderable:
  - Call OnRender(viewport, deltaTime)
  - Component returns RenderData object describing what it needs:
    * Which VAO to use
    * Which shader program to use
    * Which textures to bind
    * Which uniforms to set (position, color, etc.)
    * Draw call parameters (how many vertices, indices, etc.)
```

#### 2. **Sorting Phase** (IBatchStrategy)

```
Sort all RenderData objects to minimize state changes:
  - Group by Framebuffer (off-screen renders first)
  - Group by Shader (minimize shader switches)
  - Group by VAO (minimize geometry switches)
  - Group by Texture (minimize texture switches)

Result: Batches of similar render states
```

**Example sorted batches:**

```
Batch 1: Ground plane (terrain shader, terrain VAO)
  → GL.UseProgram(terrainShader)
  → GL.BindVertexArray(terrainVAO)
  → GL.DrawElements(...)

Batch 2: All rocks (rock shader, rock VAO)
  → GL.UseProgram(rockShader)
  → GL.BindVertexArray(rockVAO)
  → GL.DrawElements(...) x5 (with different uniforms for position/rotation)

Batch 3: All trees (foliage shader, tree VAO)
  → GL.UseProgram(foliageShader)
  → GL.BindVertexArray(treeVAO)
  → GL.DrawElements(...) x10 (with different uniforms)

Batch 4: Player (character shader, character VAO)
  → GL.UseProgram(characterShader)
  → GL.BindVertexArray(characterVAO)
  → GL.DrawElements(...)
```

#### 3. **Execution Phase**

```csharp
uint currentShader = 0;
uint currentVAO = 0;
uint currentTexture = 0;

foreach (var batch in sortedBatches)
{
    // Only change shader if different from current
    if (batch.ShaderProgram != currentShader)
    {
        GL.UseProgram(batch.ShaderProgram);
        currentShader = batch.ShaderProgram;
    }

    // Only change VAO if different from current
    if (batch.VertexArray != currentVAO)
    {
        GL.BindVertexArray(batch.VertexArray);
        currentVAO = batch.VertexArray;
    }

    // Set uniforms (these change for every object)
    SetUniforms(batch.Uniforms);

    // Draw
    GL.DrawElements(batch.PrimitiveType, batch.IndexCount, ...);
}
```

## Why Static Variables Don't Work

Your current code:

```csharp
private static uint Vao;   // Only holds ONE VAO
private static uint Shader; // Only holds ONE shader

void RenderFrame()
{
    GL.BindVertexArray(Vao);      // Always binds the SAME VAO
    GL.UseProgram(Shader);         // Always uses the SAME shader
    GL.DrawElements(...);          // Draws the same thing every time
}
```

With multiple objects you need:

```csharp
// No static variables - each object has its own resources

void RenderFrame()
{
    // Collected from components during the frame
    var renderStates = CollectRenderStates();

    // Sorted by batch strategy
    var batches = batchStrategy.Sort(renderStates);

    // Execute each batch
    foreach (var batch in batches)
    {
        GL.BindVertexArray(batch.VAO);        // Different per batch
        GL.UseProgram(batch.ShaderProgram);   // Different per batch

        // Set uniforms specific to this draw call
        foreach (var uniform in batch.Uniforms)
            SetUniform(uniform.Name, uniform.Value);

        GL.DrawElements(batch.PrimitiveType, batch.Count, ...);
    }
}
```

## The Key Insight

**Each object in the scene needs its own set of GL resources:**

```
Tree Component #1:
  - VAO: treeGeometryVAO (shared with other trees)
  - Shader: foliageShader (shared with other trees)
  - Uniforms: position=(10, 0, 5), scale=1.2, rotation=45°

Tree Component #2:
  - VAO: treeGeometryVAO (SAME as tree #1)
  - Shader: foliageShader (SAME as tree #1)
  - Uniforms: position=(15, 0, 8), scale=0.9, rotation=120°
```

They share the **expensive** resources (VAO, Shader) but have different **uniforms** for positioning.

## What Needs to Change

1. **Remove static variables** - they're per-object, not per-renderer

2. **Components create their own resources** - each component creates/references its VAO and Shader during initialization

3. **Components produce RenderData** - during `OnRender()`, components create a `RenderData` object describing what they need

4. **Renderer collects, sorts, executes** - the renderer orchestrates but doesn't own the resources

5. **ResourceManager caches** - prevents creating the same VAO/Shader multiple times (10 trees share 1 VAO)

## The Transformation

**Before (current):**

```
Renderer owns everything → Creates one quad → Renders it forever
```

**After (target):**

```
Components own their resources → Declare rendering needs → Renderer executes efficiently
```

This is why the static variables are temporary - they're a limitation of the example code, not the final architecture!
