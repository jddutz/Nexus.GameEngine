# Separation of Concerns Analysis

## Current Architecture Overview

### Three Key Interfaces

1. **IRenderer** - Orchestrates rendering, walks component tree, executes draw calls
2. **IResourceManager** - Creates and manages shared GL resources (geometry, shaders, textures)
3. **IRenderable** - Components that provide element data for rendering

## Problems Identified

### 1. **HelloQuad.GetElements() Does Too Much** ⚠️ MAJOR VIOLATION

**Current Implementation** (c:\Users\jddut\Source\Nexus.GameEngine\src\GameEngine\GUI\Components\HelloQuad.cs):

```csharp
public IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp)
{
    yield return GetRenderData(gl);
}

public unsafe DrawCommand GetRenderData(GL gl)
{
    // ❌ CREATES OpenGL resources directly
    uint vao = gl.GenVertexArray();
    gl.BindVertexArray(vao);

    uint vbo = gl.GenBuffer();
    gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
    fixed (void* v = &GeometryDefinitions.BasicQuad.Vertices[0])
    {
        gl.BufferData(...); // ❌ Uploads data directly
    }

    // ❌ Creates and compiles shaders directly
    uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
    var vertexShaderSource = ShaderDefinitions.BasicQuad.VertexShader.Load();
    gl.ShaderSource(vertexShader, vertexShaderSource);
    gl.CompileShader(vertexShader);

    // ... more shader creation
    uint shader = gl.CreateProgram();
    gl.AttachShader(shader, vertexShader);
    // ... etc

    // ❌ Sets up vertex attributes directly
    gl.VertexAttribPointer(...);
    gl.EnableVertexAttribArray(0);

    return new DrawCommand { Vao = vao, Vbo = vbo, Ebo = ebo, Shader = shader };
}
```

**Problems:**

- ✘ IRenderable components should **declare** what they need, not **create** resources
- ✘ Violates single responsibility - HelloQuad manages resource creation AND rendering requirements
- ✘ Bypasses IResourceManager completely - no caching, no lifecycle management
- ✘ Resource leaks - these resources are created every frame/call and never disposed
- ✘ No validation or error handling
- ✘ Impossible to test without OpenGL context
- ✘ Makes it hard to add new renderables - each must duplicate all this GL code

**Expected Declarative Pattern:**

```csharp
public IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp)
{
    // Should look something like:
    var geometryId = resourceManager.GetOrCreateResource(GeometryDefinitions.BasicQuad);
    var shaderId = resourceManager.GetOrCreateResource(ShaderDefinitions.BasicQuad);

    yield return new DrawCommand
    {
        Vao = geometryId,
        Shader = shaderId,
        // ... other declarative properties
        Uniforms = new Dictionary<string, object>
        {
            ["backgroundColor"] = BackgroundColor
        }
    };
}
```

---

### 2. **ResourceManager Exists But Is Not Used** ⚠️ MAJOR VIOLATION

**Current State:**

- `ResourceManager` implements full resource lifecycle management (create, cache, validate, dispose)
- Has methods `GetOrCreateResource()`, `ValidateResource()`, `ReleaseResource()`
- Implements caching to avoid duplicate resource creation
- **BUT**: HelloQuad bypasses it completely

**Evidence:**

- `ResourceManager.CreateGeometry()` - properly creates VAO/VBO/EBO with caching
- `ResourceManager.CreateShader()` - properly compiles and links shaders with caching
- HelloQuad does the exact same operations inline without using ResourceManager

**Impact:**

- Duplicate resource creation code
- No resource caching
- Resource leaks
- Can't leverage ResourceManager features (validation, statistics, purging)

---

### 3. **Renderer.Draw() Has Hardcoded Geometry Reference** ⚠️ MINOR VIOLATION

**Current Implementation** (c:\Users\jddut\Source\Nexus.GameEngine\src\GameEngine\Graphics\Renderer.cs:358):

```csharp
private unsafe void Draw(DrawCommand element)
{
    GL.BindVertexArray(element.Vao);
    GL.UseProgram(element.Shader);

    // ❌ HARDCODED reference to GeometryDefinitions.BasicQuad
    GL.DrawElements(
        PrimitiveType.Triangles,
        (uint)GeometryDefinitions.BasicQuad.Indices.Length,  // ⚠️ WRONG!
        DrawElementsType.UnsignedInt,
        null);
}
```

**Problems:**

- ✘ Can only draw BasicQuad geometry
- ✘ DrawCommand should contain the index count, not hardcode it
- ✘ Renderer shouldn't know about specific geometry definitions

**Expected Pattern:**

```csharp
private unsafe void Draw(DrawCommand element)
{
    GL.BindVertexArray(element.Vao);
    GL.UseProgram(element.Shader);

    GL.DrawElements(
        element.PrimitiveType,
        element.IndexCount,  // Should be in DrawCommand
        element.IndexType,   // Should be in DrawCommand
        null);
}
```

---

### 4. **DrawCommand Missing Critical Properties** ⚠️ MODERATE VIOLATION

**Current DrawCommand** (c:\Users\jddut\Source\Nexus.GameEngine\src\GameEngine\Graphics\DrawCommand.cs):

```csharp
public class DrawCommand
{
    public required uint Vbo { get; init; }
    public required uint Ebo { get; init; }
    public required uint Vao { get; init; }
    public required uint Shader { get; init; }

    public uint? Priority { get; set; }
    public uint? ShaderProgram { get; set; }
    public uint?[] BoundTextures { get; private set; } = new uint?[16];
    // ... other state tracking
}
```

**Problems:**

- ✘ Has both `Shader` (required) and `ShaderProgram` (optional) - redundant/confusing
- ✘ Missing draw call properties: `IndexCount`, `PrimitiveType`, `IndexType`
- ✘ Has `Vbo` and `Ebo` but they're never used (only `Vao` is bound)
- ✘ Mixes "resource IDs" with "GL state tracking" (BoundTextures, ActiveTextureUnit, Framebuffer)
- ✘ `Uniforms` dictionary exists but not consistently used

**Expected Structure:**

```csharp
public class DrawCommand
{
    // Resource references (what to render)
    public required uint Vao { get; init; }
    public required uint ShaderProgram { get; init; }

    // Draw call parameters (how to render)
    public required uint IndexCount { get; init; }
    public PrimitiveType PrimitiveType { get; init; } = PrimitiveType.Triangles;
    public DrawElementsType IndexType { get; init; } = DrawElementsType.UnsignedInt;

    // Uniforms to apply
    public Dictionary<string, object> Uniforms { get; init; } = [];

    // Render ordering
    public uint Priority { get; set; }
}
```

---

### 5. **Renderer Has Unused Batch Processing Code** ⚠️ MINOR ISSUE

**Current Implementation:**

- Renderer has methods: `UpdateFramebuffer()`, `UpdateShaderProgram()`, `UpdateVertexArray()`, `UpdateTextures()`
- Has `ApplyUniforms()` and `ApplyUniform()` methods
- Documentation mentions IBatchStrategy and batch processing
- **BUT**: `OnRender()` doesn't use any of this - just calls `Draw()` directly

**Evidence** (Renderer.cs:337-363):

```csharp
public void OnRender(double deltaTime) => RenderViewport(contentManager.Viewport);

private void RenderViewport(IViewport viewport)
{
    // ... setup ...

    foreach (var element in viewport.Content
        .GetChildren<IRenderable>()
        .SelectMany(r => r.GetElements(GL, viewport)))
    {
        Draw(element);  // ⚠️ No batching, no state optimization
    }
}

private unsafe void Draw(DrawCommand element)
{
    GL.BindVertexArray(element.Vao);  // ⚠️ Doesn't use UpdateVertexArray()
    GL.UseProgram(element.Shader);     // ⚠️ Doesn't use UpdateShaderProgram()
    GL.DrawElements(...);              // ⚠️ Doesn't apply uniforms
}
```

**Impact:**

- Wasted code (Update\* methods are never called)
- No actual batching/optimization
- State changes happen every draw call

---

### 6. **IResourceManager Interface Doesn't Match Usage Pattern** ⚠️ DESIGN ISSUE

**Current Interface:**

```csharp
public interface IResourceManager
{
    uint GetOrCreateResource(IResourceDefinition definition, IAssetService? assetService = null);
    ResourceValidationResult ValidateResource(IResourceDefinition definition);
    bool ReleaseResource(string resourceName);
    // ...
}
```

**Problems:**

- ✘ Returns `uint` (OpenGL ID) - ties interface to OpenGL implementation
- ✘ Components would need to pass full `IResourceDefinition` objects
- ✘ No way to get index count or other metadata back after resource creation
- ✘ Can't support resource handles/wrappers for better type safety

**Better Pattern:**

```csharp
public interface IResourceManager
{
    GeometryResource GetOrCreateGeometry(GeometryDefinition definition);
    ShaderResource GetOrCreateShader(ShaderDefinition definition);
    // ...
}

public record GeometryResource
{
    public uint VaoId { get; init; }
    public uint IndexCount { get; init; }
    public PrimitiveType PrimitiveType { get; init; }
    // ... other metadata needed for rendering
}
```

---

## Summary of Required Changes

### Priority 1: Critical (Blocks New Renderables)

1. **Refactor HelloQuad.GetElements()** to use ResourceManager

   - Remove all GL resource creation code
   - Use `resourceManager.GetOrCreateResource()` for geometry and shaders
   - Return DrawCommand with resource IDs only

2. **Enhance DrawCommand** to include draw call parameters

   - Add `IndexCount`, `PrimitiveType`, `IndexType`
   - Remove redundant fields (`Vbo`, `Ebo`, duplicate shader references)
   - Clarify separation between "resource IDs" and "GL state tracking"

3. **Fix Renderer.Draw()** to use DrawCommand properties
   - Remove hardcoded `GeometryDefinitions.BasicQuad.Indices.Length`
   - Use `element.IndexCount`, `element.PrimitiveType`, etc.

### Priority 2: Important (Improves Architecture)

4. **Inject IResourceManager into components** that need it

   - Add IResourceManager to HelloQuad constructor/factory
   - Remove GL parameter from GetElements() (not needed for resource lookup)

5. **Consider ResourceManager interface improvements**
   - Return resource metadata wrappers instead of raw uint IDs
   - Add type-safe methods per resource type

### Priority 3: Nice to Have (Performance)

6. **Implement actual batching in Renderer**

   - Use the `Update*` methods that already exist
   - Apply uniforms using `ApplyUniforms()`
   - Sort DrawCommand by state to minimize GL calls

7. **Remove unused state tracking from DrawCommand**
   - `BoundTextures`, `ActiveTextureUnit`, `Framebuffer` aren't used
   - Or implement them properly if needed for batching

---

## Decision Required

**Should GL parameter be removed from IRenderable.GetElements()?**

Current signature:

```csharp
IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp);
```

If we use ResourceManager properly, components don't need direct GL access. They just need to:

1. Reference resource definitions (already have via static classes)
2. Call ResourceManager to get resource IDs
3. Return DrawCommand with those IDs

**Recommendation:** Remove GL parameter, inject IResourceManager instead.

```csharp
// New signature:
IEnumerable<DrawCommand> GetElements(IViewport vp);

// Component constructor:
public HelloQuad(IResourceManager resourceManager) { ... }
```

This would:

- ✓ Prevent components from making direct GL calls
- ✓ Enforce proper separation of concerns
- ✓ Make components easier to test (mock IResourceManager)
- ✓ Align with declarative rendering philosophy
