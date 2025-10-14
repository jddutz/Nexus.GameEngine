# Architecture Diagrams: Current vs. Refactored

## Current Architecture (BROKEN)

```
┌─────────────────────────────────────────────────────────────────┐
│                         Renderer                                 │
│  - Walks component tree                                         │
│  - Calls GetElements() on each IRenderable                      │
│  - Calls Draw() with hardcoded GeometryDefinitions.BasicQuad   │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            │ calls GetElements(gl, vp)
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    HelloQuad (IRenderable)                       │
│                                                                  │
│  GetElements(gl, vp):                                           │
│    ❌ gl.GenVertexArray()                                       │
│    ❌ gl.GenBuffer()                                            │
│    ❌ gl.BufferData(vertices)                                   │
│    ❌ gl.CreateShader()                                         │
│    ❌ gl.CompileShader()                                        │
│    ❌ gl.CreateProgram()                                        │
│    ❌ gl.LinkProgram()                                          │
│    ❌ gl.VertexAttribPointer()                                  │
│    return DrawCommand { Vao, Vbo, Ebo, Shader }                │
│                                                                  │
│  Problems:                                                       │
│  - Creates resources every frame                                │
│  - No caching                                                   │
│  - Resource leaks                                               │
│  - Bypasses ResourceManager                                     │
│  - Too much complexity                                          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│              ResourceManager (UNUSED!)                           │
│  - Has GetOrCreateResource()                                    │
│  - Has caching                                                  │
│  - Has validation                                               │
│  - Has lifecycle management                                     │
│  ⚠️  But nobody calls it!                                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## Desired Architecture (FIXED)

```
┌─────────────────────────────────────────────────────────────────┐
│                         Renderer                                 │
│  Responsibility: Orchestrate rendering                          │
│  - Walks component tree                                         │
│  - Calls GetElements() on each IRenderable                      │
│  - Applies uniforms from DrawCommand                            │
│  - Issues GL.DrawElements() using DrawCommand properties        │
│  - NO hardcoded geometry references                             │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            │ calls GetElements(vp)
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    HelloQuad (IRenderable)                       │
│  Responsibility: Declare rendering requirements                 │
│                                                                  │
│  Constructor:                                                    │
│    HelloQuad(IResourceManager resourceManager)                  │
│                                                                  │
│  GetElements(vp):                                               │
│    ✅ geometry = resourceManager.GetOrCreateGeometry(...)       │
│    ✅ shader = resourceManager.GetOrCreateShader(...)           │
│    ✅ return DrawCommand {                                       │
│         Vao = geometry.VaoId,                                   │
│         Shader = shader.ProgramId,                              │
│         IndexCount = geometry.IndexCount,                       │
│         Uniforms = { ["color"] = BackgroundColor }              │
│       }                                                          │
│                                                                  │
│  Benefits:                                                       │
│  - Simple, declarative                                          │
│  - No GL knowledge needed                                       │
│  - Resources cached automatically                               │
│  - Easy to test (mock ResourceManager)                          │
│  - ~15 lines vs ~150 lines                                      │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            │ calls GetOrCreateGeometry/Shader
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│              ResourceManager (PROPERLY USED!)                    │
│  Responsibility: Create and manage GL resources                 │
│  - GetOrCreateGeometry(GeometryDefinition)                      │
│      → returns GeometryResource { VaoId, IndexCount, ... }      │
│  - GetOrCreateShader(ShaderDefinition)                          │
│      → returns ShaderResource { ProgramId, ... }                │
│  - Caches resources by name                                     │
│  - Validates resource definitions                               │
│  - Manages lifecycle (dispose, purge)                           │
│  - Handles GL context                                           │
└─────────────────────────────────────────────────────────────────┘
                            │
                            │ uses
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Resource Definitions                            │
│  Pure data - no behavior                                        │
│  - GeometryDefinitions.BasicQuad                                │
│  - ShaderDefinitions.BasicQuad                                  │
│  - Static, immutable, shared                                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Flow: Current vs. Refactored

### Current (BROKEN)

```
Frame N:
  Renderer.OnRender()
    → HelloQuad.GetElements(gl, vp)
        → ❌ Create VAO/VBO/EBO/Shader (100+ lines of GL code)
        → return DrawCommand { newly created IDs }
    → Renderer.Draw(element)
        → GL.DrawElements(hardcoded BasicQuad.Indices.Length)

Frame N+1:
  Renderer.OnRender()
    → HelloQuad.GetElements(gl, vp)
        → ❌ Create VAO/VBO/EBO/Shader AGAIN! (resource leak)
        → return DrawCommand { different IDs }
    → Renderer.Draw(element)
        → GL.DrawElements(hardcoded BasicQuad.Indices.Length)

Result: Resources leaked every frame, massive overhead
```

### Refactored (FIXED)

```
Frame N:
  Renderer.OnRender()
    → HelloQuad.GetElements(vp)
        → resourceManager.GetOrCreateGeometry(BasicQuad)
            → ✅ Cache miss - create VAO/VBO/EBO, store in cache
            → return GeometryResource { VaoId, IndexCount }
        → resourceManager.GetOrCreateShader(BasicQuad)
            → ✅ Cache miss - compile shader, store in cache
            → return ShaderResource { ProgramId }
        → return DrawCommand { VaoId, ProgramId, IndexCount, Uniforms }
    → Renderer.Draw(element)
        → GL.UseProgram(element.Shader)
        → ApplyUniforms(element.Uniforms)
        → GL.DrawElements(element.PrimitiveType, element.IndexCount, ...)

Frame N+1:
  Renderer.OnRender()
    → HelloQuad.GetElements(vp)
        → resourceManager.GetOrCreateGeometry(BasicQuad)
            → ✅ Cache hit - return cached GeometryResource
        → resourceManager.GetOrCreateShader(BasicQuad)
            → ✅ Cache hit - return cached ShaderResource
        → return DrawCommand { same VaoId, same ProgramId, ... }
    → Renderer.Draw(element)
        → GL.DrawElements(element.IndexCount)

Result: No leaks, minimal overhead, proper caching
```

---

## Responsibility Matrix

### Current Architecture

| Responsibility            | Renderer | HelloQuad | ResourceManager |
| ------------------------- | -------- | --------- | --------------- |
| Walk component tree       | ✅       | ❌        | ❌              |
| Call GetElements()        | ✅       | ❌        | ❌              |
| Create VAO/VBO            | ❌       | ❌ WRONG  | ✅              |
| Compile shaders           | ❌       | ❌ WRONG  | ✅              |
| Cache resources           | ❌       | ❌        | ✅ (unused)     |
| Declare what to render    | ❌       | ✅        | ❌              |
| Issue GL draw calls       | ✅       | ❌        | ❌              |
| Apply uniforms            | ❌ (not) | ❌        | ❌              |
| Manage resource lifecycle | ❌       | ❌        | ✅ (unused)     |

**Problems:**

- ❌ HelloQuad creates resources directly (wrong responsibility)
- ❌ ResourceManager exists but is unused
- ❌ Renderer has hardcoded geometry reference

### Refactored Architecture

| Responsibility            | Renderer | HelloQuad | ResourceManager |
| ------------------------- | -------- | --------- | --------------- |
| Walk component tree       | ✅       | ❌        | ❌              |
| Call GetElements()        | ✅       | ❌        | ❌              |
| Create VAO/VBO            | ❌       | ❌        | ✅              |
| Compile shaders           | ❌       | ❌        | ✅              |
| Cache resources           | ❌       | ❌        | ✅              |
| Declare what to render    | ❌       | ✅        | ❌              |
| Issue GL draw calls       | ✅       | ❌        | ❌              |
| Apply uniforms            | ✅       | ❌        | ❌              |
| Manage resource lifecycle | ❌       | ❌        | ✅              |

**Benefits:**

- ✅ Clear separation of concerns
- ✅ Each class has single responsibility
- ✅ Easy to test in isolation
- ✅ Easy to extend with new renderables

---

## Code Complexity Comparison

### HelloQuad.GetElements() - Current

```csharp
// ~150 lines, high complexity
public IEnumerable<DrawCommand> GetElements(GL gl, IViewport vp)
{
    yield return GetRenderData(gl);
}

public unsafe DrawCommand GetRenderData(GL gl)
{
    // Create VAO (3 lines)
    uint vao = gl.GenVertexArray();
    gl.BindVertexArray(vao);

    // Create VBO (7 lines)
    uint vbo = gl.GenBuffer();
    gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
    fixed (void* v = &GeometryDefinitions.BasicQuad.Vertices[0])
    {
        gl.BufferData(
            BufferTargetARB.ArrayBuffer,
            (nuint)(GeometryDefinitions.BasicQuad.Vertices.Length * sizeof(uint)),
            v,
            BufferUsageARB.StaticDraw);
    }

    // Create EBO (8 lines)
    uint ebo = gl.GenBuffer();
    gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
    fixed (void* i = &GeometryDefinitions.BasicQuad.Indices[0])
    {
        gl.BufferData(
            BufferTargetARB.ElementArrayBuffer,
            (nuint)(GeometryDefinitions.BasicQuad.Indices.Length * sizeof(uint)),
            i,
            BufferUsageARB.StaticDraw);
    }

    // Create vertex shader (5 lines)
    uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
    var vertexShaderSource = ShaderDefinitions.BasicQuad.VertexShader.Load();
    gl.ShaderSource(vertexShader, vertexShaderSource);
    gl.CompileShader(vertexShader);
    string infoLog = gl.GetShaderInfoLog(vertexShader);

    // Create fragment shader (5 lines)
    uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
    var fragmentShaderSource = ShaderDefinitions.BasicQuad.FragmentShader.Load();
    gl.ShaderSource(fragmentShader, fragmentShaderSource);
    gl.CompileShader(fragmentShader);
    infoLog = gl.GetShaderInfoLog(fragmentShader);

    // Create shader program (10 lines)
    uint shader = gl.CreateProgram();
    gl.AttachShader(shader, vertexShader);
    gl.AttachShader(shader, fragmentShader);
    gl.LinkProgram(shader);
    gl.GetProgram(shader, GLEnum.LinkStatus, out var status);
    gl.DetachShader(shader, vertexShader);
    gl.DetachShader(shader, fragmentShader);
    gl.DeleteShader(vertexShader);
    gl.DeleteShader(fragmentShader);

    // Set up vertex attributes (8 lines)
    gl.VertexAttribPointer(
        0,
        3,
        VertexAttribPointerType.Float,
        false,
        3 * sizeof(float),
        null);
    gl.EnableVertexAttribArray(0);

    return new() { Vao = vao, Vbo = vbo, Ebo = ebo, Shader = shader };
}

// Total: ~150 lines of GL-specific code
// Cyclomatic complexity: High (many GL calls, error paths)
// Maintainability: Low (requires GL expertise)
// Testability: Low (requires GL context)
```

### HelloQuad.GetElements() - Refactored

```csharp
// ~15 lines, low complexity
public IEnumerable<DrawCommand> GetElements(IViewport vp)
{
    if (!IsVisible)
        yield break;

    // Get resources (2 lines, cached automatically)
    var geometry = _resourceManager.GetOrCreateGeometry(GeometryDefinitions.BasicQuad);
    var shader = _resourceManager.GetOrCreateShader(ShaderDefinitions.BasicQuad);

    // Declare what to render (10 lines)
    yield return new DrawCommand
    {
        Vao = geometry.VaoId,
        Shader = shader.ProgramId,
        IndexCount = geometry.IndexCount,
        PrimitiveType = geometry.PrimitiveType,
        Priority = RenderPriority,
        SourceViewport = vp,
        Uniforms = new Dictionary<string, object>
        {
            ["backgroundColor"] = BackgroundColor
        }
    };
}

// Total: ~15 lines of declarative code
// Cyclomatic complexity: Low (simple control flow)
// Maintainability: High (no GL knowledge needed)
// Testability: High (mock ResourceManager)
```

**Improvement:** 90% reduction in code, 10x easier to understand

---

## Testing Strategy Comparison

### Current - Hard to Test

```csharp
[Fact]
public void HelloQuad_GetElements_ReturnsValidData()
{
    // ❌ Problem: Requires full OpenGL context
    var gl = CreateOpenGLContext(); // Complex setup
    var viewport = CreateMockViewport();

    var helloQuad = new HelloQuad();

    // ❌ Problem: Creates resources, can't verify caching
    var elements = helloQuad.GetElements(gl, viewport).ToList();

    // ❌ Problem: Can only test if it doesn't crash
    Assert.NotEmpty(elements);
    // Can't verify correctness without GL state inspection
}
```

### Refactored - Easy to Test

```csharp
[Fact]
public void HelloQuad_GetElements_RequestsCorrectResources()
{
    // ✅ Simple: Mock ResourceManager
    var mockResourceManager = new Mock<IResourceManager>();
    mockResourceManager
        .Setup(m => m.GetOrCreateGeometry(GeometryDefinitions.BasicQuad))
        .Returns(new GeometryResource { VaoId = 1, IndexCount = 6 });
    mockResourceManager
        .Setup(m => m.GetOrCreateShader(ShaderDefinitions.BasicQuad))
        .Returns(new ShaderResource { ProgramId = 2 });

    var viewport = CreateMockViewport();
    var helloQuad = new HelloQuad(mockResourceManager.Object);

    // ✅ Test declarative output
    var elements = helloQuad.GetElements(viewport).ToList();

    // ✅ Verify correct resources requested
    mockResourceManager.Verify(
        m => m.GetOrCreateGeometry(GeometryDefinitions.BasicQuad),
        Times.Once);

    // ✅ Verify DrawCommand is correct
    Assert.Equal(1u, elements[0].Vao);
    Assert.Equal(2u, elements[0].Shader);
    Assert.Equal(6u, elements[0].IndexCount);
    Assert.Contains("backgroundColor", elements[0].Uniforms.Keys);
}

[Fact]
public void HelloQuad_GetElements_WhenInvisible_ReturnsEmpty()
{
    var mockResourceManager = new Mock<IResourceManager>();
    var helloQuad = new HelloQuad(mockResourceManager.Object);
    helloQuad.SetVisible(false);

    var elements = helloQuad.GetElements(CreateMockViewport()).ToList();

    // ✅ No resources requested when invisible
    Assert.Empty(elements);
    mockResourceManager.Verify(
        m => m.GetOrCreateGeometry(It.IsAny<GeometryDefinition>()),
        Times.Never);
}
```

---

## Summary

| Aspect                 | Current (Broken) | Refactored (Fixed) |
| ---------------------- | ---------------- | ------------------ |
| HelloQuad LOC          | ~150 lines       | ~15 lines          |
| Complexity             | High             | Low                |
| GL knowledge required  | Expert level     | None               |
| Resource caching       | ❌ None          | ✅ Automatic       |
| Resource leaks         | ❌ Yes           | ✅ No              |
| Separation of concerns | ❌ Violated      | ✅ Clean           |
| Testability            | ❌ Low           | ✅ High            |
| Extensibility          | ❌ Hard          | ✅ Easy            |
| Time to add renderable | ~2 hours         | ~15 minutes        |
| Bugs per renderable    | Many             | Few                |

**Recommendation:** Proceed with refactoring plan.
