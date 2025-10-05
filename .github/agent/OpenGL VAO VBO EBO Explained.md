# Understanding VAO, VBO, EBO in OpenGL

## The Three Key Objects

### VBO (Vertex Buffer Object)

**What it is:** GPU memory that stores vertex data (positions, colors, normals, texture coordinates, etc.)

**What it does:** Uploads your vertex array from CPU RAM to GPU RAM once, so the GPU can access it quickly without repeated CPU→GPU transfers.

```csharp
Vbo = GL.GenBuffer();                          // Ask OpenGL to allocate a buffer ID
GL.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo); // Say "I'm working with this VBO now"

// Upload data to GPU
fixed (void* v = &GeometryDefinitions.BasicQuad.Vertices[0])
{
    GL.BufferData(
        BufferTargetARB.ArrayBuffer,           // Target: the currently bound VBO
        (nuint)(vertices.Length * sizeof(float)), // Size in bytes
        v,                                     // Pointer to CPU memory
        BufferUsageARB.StaticDraw);            // Hint: data won't change
}
// Data is now on the GPU. You can forget about the CPU array.
```

**After `OnLoad()`:** You don't need to bind it again unless you're updating the data. The VAO (see below) remembers which VBO to use.

---

### EBO (Element Buffer Object) / IBO (Index Buffer Object)

**What it is:** GPU memory that stores indices into your vertex array.

**Why it exists:** Let's you reuse vertices. Instead of duplicating vertex data, you reference the same vertex multiple times by index.

**Example:**

```
Square with 4 vertices (without indices):
Vertices: [v0, v1, v2,  v0, v2, v3]  // 6 vertices, v0 and v2 duplicated!

Square with indices:
Vertices: [v0, v1, v2, v3]           // 4 unique vertices
Indices:  [0, 1, 2,  0, 2, 3]        // 6 indices, reuse v0 and v2
```

```csharp
Ebo = GL.GenBuffer();
GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo);

fixed (void* i = &indices[0])
{
    GL.BufferData(
        BufferTargetARB.ElementArrayBuffer,
        (nuint)(indices.Length * sizeof(uint)),
        i,
        BufferUsageARB.StaticDraw);
}
```

**After `OnLoad()`:** You don't need to bind it again. The VAO remembers which EBO to use.

---

### VAO (Vertex Array Object)

**What it is:** A **configuration object** that remembers:

1. Which VBO(s) to use
2. Which EBO to use (if any)
3. How to interpret the VBO data (via `glVertexAttribPointer`)

**The "Magic" Happens Here:**

When you call:

```csharp
GL.BindVertexArray(Vao);              // 1. Start recording configuration

GL.BindBuffer(ArrayBuffer, Vbo);      // 2. VAO records: "use this VBO"
GL.BindBuffer(ElementArrayBuffer, Ebo); // 3. VAO records: "use this EBO"

GL.VertexAttribPointer(               // 4. VAO records: "interpret data like this"
    0,                                 //    - Attribute location 0
    3,                                 //    - 3 floats per vertex (x, y, z)
    VertexAttribPointerType.Float,     //    - Type is float
    false,                             //    - Don't normalize
    3 * sizeof(float),                 //    - Stride: 12 bytes between vertices
    null);                             //    - Offset: start at byte 0

GL.EnableVertexAttribArray(0);        // 5. VAO records: "enable attribute 0"
```

**All this configuration is STORED in the VAO.**

---

## Why You Never Use VBO/EBO Again

During `OnLoad()`:

1. Create VAO, bind it
2. Create VBO, bind it, upload data → **VAO remembers VBO**
3. Create EBO, bind it, upload data → **VAO remembers EBO**
4. Configure vertex attributes → **VAO remembers configuration**

During `RenderFrame()`:

```csharp
GL.BindVertexArray(Vao);  // Restores ALL the state:
                          // - Binds the VBO automatically
                          // - Binds the EBO automatically
                          // - Restores vertex attribute configuration

GL.DrawElements(...);     // GPU knows where to read data from
```

**You don't need to touch `Vbo` or `Ebo` again** because `BindVertexArray(Vao)` automatically binds them behind the scenes!

---

## The `fixed` Keyword

```csharp
fixed (void* v = &GeometryDefinitions.BasicQuad.Vertices[0])
{
    GL.BufferData(..., v, ...);
}
```

**What `fixed` does:** Temporarily **pins** the C# array in memory so the garbage collector won't move it.

**Why it's needed:**

- C# arrays can move in memory when the garbage collector runs (memory compaction)
- OpenGL needs a stable pointer to read from
- `fixed` tells the GC: "Don't move this array until I'm done with it"

**It's NOT `const`:** You could modify the array through `v` if you wanted. It just prevents the array's **memory address** from changing.

**Scope:** The array is only pinned within the `fixed { }` block. After that, the GC can move it again (but it doesn't matter—OpenGL already copied the data to the GPU).

---

## What `GL.BufferData` Does

```csharp
GL.BufferData(
    BufferTargetARB.ArrayBuffer,           // Which buffer to upload to
    (nuint)(vertices.Length * sizeof(float)), // Size in bytes
    v,                                     // Pointer to source data
    BufferUsageARB.StaticDraw);            // Usage hint
```

**Step by step:**

1. **Target:** Which buffer slot to use
   - `ArrayBuffer` = VBO (vertex data)
   - `ElementArrayBuffer` = EBO (index data)
2. **Size:** How many bytes to allocate on the GPU

3. **Data Pointer:** Where to copy from (CPU memory)

   - `v` points to your C# array
   - OpenGL copies data from CPU → GPU

4. **Usage Hint:** Tells OpenGL how you plan to use the data (so it can optimize storage)

**After this call:** The data lives on the GPU. You can delete your CPU array if you want (though you're keeping it in `GeometryDefinitions`).

---

## BufferUsageARB: Static vs Dynamic vs Stream

The format is `{Frequency}{Access}`:

### Frequency (how often data changes):

- **Static:** Set once, never (or rarely) changed
- **Dynamic:** Modified occasionally (every few frames)
- **Stream:** Modified every frame (e.g., particles, dynamic text)

### Access (how it's used):

- **Draw:** You write data, GPU reads it (most common)
- **Read:** GPU writes data, you read it (e.g., query results)
- **Copy:** GPU writes data, GPU reads it (e.g., transform feedback)

### Common Patterns:

**StaticDraw** (your current code):

```csharp
// Upload once in OnLoad, never change
GL.BufferData(..., BufferUsageARB.StaticDraw);
// Use case: Static level geometry, UI quads
```

**DynamicDraw** (animated character):

```csharp
// Upload in OnLoad with initial pose
GL.BufferData(..., BufferUsageARB.DynamicDraw);

// Later, in RenderFrame (when animation updates):
GL.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
GL.BufferSubData(                        // Update part of the buffer
    BufferTargetARB.ArrayBuffer,
    0,                                   // Offset
    (nuint)(newVertices.Length * sizeof(float)),
    newVerticesPointer);
```

**StreamDraw** (particle system):

```csharp
// Recreate every frame
foreach (frame)
{
    GL.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
    GL.BufferData(..., particleVertices, BufferUsageARB.StreamDraw);
    GL.DrawArrays(...);
}
```

---

## Your Player Character Example

For an animated player with the **same number of vertices** every frame (just different positions):

### Option 1: Update the Buffer (DynamicDraw)

```csharp
// In OnLoad:
Vbo = GL.GenBuffer();
GL.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
GL.BufferData(
    BufferTargetARB.ArrayBuffer,
    (nuint)(maxVertices * sizeof(float)),
    null,                                // Reserve space, no initial data
    BufferUsageARB.DynamicDraw);         // Will be updated

// In RenderFrame (when skeleton updates):
float[] animatedVertices = CalculateAnimatedPose();

GL.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
fixed (void* v = &animatedVertices[0])
{
    GL.BufferSubData(                    // Update existing buffer
        BufferTargetARB.ArrayBuffer,
        0,
        (nuint)(animatedVertices.Length * sizeof(float)),
        v);
}

GL.BindVertexArray(Vao);
GL.DrawElements(...);
```

### Option 2: Skeletal Animation (Better Performance)

```csharp
// Upload base mesh once (static vertices)
// Upload bone matrices as uniforms every frame
// Let vertex shader deform the mesh on the GPU

// Vertex shader:
// vec4 skinnedPosition = bones[boneIndex0] * weight0 * position +
//                        bones[boneIndex1] * weight1 * position + ...;
```

**Skeletal animation is preferred** because:

- Vertices stay on GPU (StaticDraw)
- Only bone matrices are uploaded (much less data)
- GPU does the math in parallel

---

## BufferTargetARB: What Are These?

```csharp
BufferTargetARB.ArrayBuffer         // VBO - vertex data
BufferTargetARB.ElementArrayBuffer  // EBO - index data
```

**These are "binding points"** - think of them as slots:

```
[ArrayBuffer slot]         → Currently holds VBO #42
[ElementArrayBuffer slot]  → Currently holds EBO #17
```

When you call:

```csharp
GL.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
```

You're saying: "Put `Vbo` into the `ArrayBuffer` slot. All future operations on `ArrayBuffer` affect this buffer."

Then:

```csharp
GL.BufferData(BufferTargetARB.ArrayBuffer, ...);
```

Operates on whichever buffer is currently in the `ArrayBuffer` slot.

---

## Summary

**VAO:** Configuration container that remembers VBO + EBO + vertex layout

- Bind once per object when setting up
- Bind once per draw call when rendering
- **Automatically** restores VBO/EBO bindings

**VBO:** GPU memory for vertices

- Create and upload data once (StaticDraw) or update as needed (DynamicDraw)
- Don't need to touch it again if data doesn't change

**EBO:** GPU memory for indices

- Same as VBO, but for index data
- Avoids duplicating vertex data

**fixed:** Pins C# array in memory temporarily so OpenGL can safely read it

**GL.BufferData:** Copies data from CPU → GPU memory

**Usage hints:** Tell OpenGL how you'll use the buffer so it can optimize:

- **Static:** Set once (level geometry)
- **Dynamic:** Update sometimes (character animation)
- **Stream:** Update every frame (particles)

**Why you never see VBO/EBO after OnLoad:**

```
BindVertexArray(Vao) → Automatically binds VBO + EBO + restores layout
```

All the configuration is baked into the VAO!
