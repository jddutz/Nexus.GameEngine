# Geometry Resource Management System - Design

## Context
Simplifying `HelloQuad` from ~375 lines to ~50-75 lines by extracting manual Vulkan buffer management into a centralized resource system.

## Current State
- `HelloQuad` manually creates vertex buffers (~50 lines)
- Manually allocates and manages memory (~30 lines)
- Manually implements cleanup (~10 lines)
- No sharing of geometry between components
- Each component reimplements buffer management

## Design Decisions

### 1. Vertex Format Strategy ✅

**Decision: Generic Vertex Structs**

Single generic struct handles all attribute combinations:
- `Vertex<TPosition, TAttribute1>` for 2-component vertices
- `Vertex<TPosition, TAttribute1, TAttribute2>` for 3-component vertices
- Add more variants as needed

**Benefits:**
- Type-safe with compile-time checking
- No type explosion
- Self-documenting: `Vertex<Vector2D<float>, Vector3D<float>>`
- Reusable across all geometry
- `unmanaged` constraint ensures GPU-compatible memory layout

**Technical Validation:**
- Generic structs with `unmanaged` constraint have predictable sequential layout
- `Marshal.SizeOf<T>()` and `MemoryMarshal.AsBytes()` work correctly
- Same memory layout as non-generic structs from GPU perspective
- Proven pattern (Unity ECS, DirectX Math)

---

### 2. Resource Ownership & Lifecycle ✅

**Decision: Hierarchical Sub-Manager Architecture**

ResourceManager delegates to specialized sub-managers:
- `IResourceManager.Geometry` - GeometrySubManager
- `IResourceManager.Textures` - TextureSubManager  
- `IResourceManager.Shaders` - ShaderSubManager

**Access Pattern:**
```
resources.Geometry.GetOrCreate(Geometry.ColoredQuad)
resources.Textures.GetOrCreate(Textures.MainMenuBackground)
```

**Sub-Manager Responsibilities:**
- Create Vulkan resources (buffers, images, etc.)
- Reference counting
- Lifecycle management (destroy when ref count = 0, or persist if flagged)
- Caching by definition identity

**Definition Storage:**
- Static variables in definition classes
- Pure data: embedded resource paths, geometric properties
- No Vulkan objects, just descriptions

**Component Usage:**
- Components hold resource handles
- Pass handles to DrawCommands
- No manual cleanup (sub-managers handle it)

**Persistence Flag:**
- Definitions can be marked to persist even when ref count = 0
- Useful for frequently reused resources (standard primitives)

---

### 3. Buffer Creation Strategy ✅

**Decision: Separate Buffer Management Service**

Extract low-level Vulkan buffer operations into dedicated service:
- `IBufferManager` interface
- Handles buffer creation, memory allocation, binding
- Reusable across all resource types
- Sub-managers use BufferManager for buffer operations

**Benefits:**
- Separation of concerns
- Single source of truth for buffer operations
- Easier to optimize (staging buffers, memory pools)
- Testable in isolation

---

### 4. Geometry Definition Storage ✅

**Decision: Static Variables in Definition Classes**

Static readonly definitions for primitive geometry:
```
public static class Geometry {
    public static readonly ColoredQuadDefinition ColoredQuad = new(...);
    public static readonly ColoredCubeDefinition ColoredCube = new(...);
}
```

Support for complex geometry from files:
- Embedded resource paths (OBJ, FBX, custom formats)
- Details deferred to future implementation
- Sub-manager loads and caches on first access

**Benefits:**
- Centralized, discoverable
- Compile-time references (no string keys)
- Easy to extend with new primitives

---

### 5. VertexInputDescription Management ⚠️ OPEN QUESTION

**The Challenge:**
VertexInputDescription describes the vertex data layout and must be coordinated between:
- **Geometry** - Provides the actual vertex data (positions, colors, texcoords, etc.)
- **Shader** - Expects specific inputs at specific locations
- **Pipeline** - Needs VertexInputDescription for Vulkan pipeline creation

**Current HelloQuad Approach:**
Component manually creates VertexInputDescription matching its vertex struct, then passes it to PipelineDescriptor.

**The Coordination Problem:**
```
Geometry has: Position(Vec2), Color(Vec3)
Shader expects: layout(location=0) in vec2 position; layout(location=1) in vec3 color;
Pipeline needs: VertexInputDescription to validate compatibility
```

All three must agree, or rendering fails. Who enforces this?

---

**Analysis of Ownership Options:**

**Option A: Geometry Owns VertexInputDescription**
- Geometry definition provides: vertex data + input description
- Pipeline pulls descriptor from geometry
- Shader must be written to match geometry format

*Pros:* Geometry is self-describing, easy to create new geometry
*Cons:* Shaders are rigid, geometry dictates shader inputs

**Option B: Shader Owns VertexInputDescription**  
- Shader provides: SPIR-V + input description (via reflection or manual spec)
- Geometry must match shader's expectations
- Pipeline validates compatibility

*Pros:* Shader-centric (matches Vulkan philosophy), flexible geometry
*Cons:* Complex reflection, shader dictates geometry format

**Option C: Pipeline Coordinates Both**
- Geometry provides: just vertex data
- Shader provides: expected inputs (location, format)
- Pipeline: queries both, validates compatibility, generates VertexInputDescription

*Pros:* Explicit validation, clear contracts
*Cons:* Most complex, requires coordination logic

**Option D: Explicit Binding Contract**
- Geometry + Shader both declare their contracts independently
- Pipeline configuration explicitly binds them together
- Validation at pipeline creation

*Pros:* Maximum flexibility, explicit control
*Cons:* Most verbose, requires manual binding

---

**Your Insight: Dynamic Mesh Manipulation**

Components may need to manipulate geometry in `OnUpdate()`:
- Procedural generation (terrain, particles)
- Animation (skeletal, morph targets)  
- Physics deformation

This suggests geometry needs to be mutable at runtime, which affects the design:
- Static geometry definitions (ColoredQuad) are immutable, loaded once
- Dynamic geometry needs update mechanisms
- But both need compatible VertexInputDescription for their shaders

---

**Proposed Clarification:**

**VertexInputDescription is shader metadata** - it describes what the shader expects as input.

Consider:
- **Geometry** = "a blob of vertex data" (just the raw numbers in a buffer)
- **Shader** = "code that interprets vertex data" + "schema describing what inputs it expects"
- **Pipeline** = connects them: "use this geometry buffer with this shader's interpretation"

Under this model:
1. **GeometrySubManager**: Creates buffers from raw vertex data, no interpretation
2. **ShaderSubManager**: Loads shader + provides VertexInputDescription (what shader expects)
3. **Pipeline creation**: Takes shader (with descriptor) + geometry (just buffer handle)
4. **Component**: References GeometryHandle + ShaderHandle, passes both to pipeline

---

**ShaderDefinition Structure:**

```
public class ShaderDefinition : IShaderDefinition {
    public string Name { get; }
    public string VertexShaderPath { get; }      // Path to SPIR-V
    public string FragmentShaderPath { get; }    // Path to SPIR-V
    public VertexInputDescription InputDescription { get; }  // What shader expects
}
```

The shader definition declares:
- Where to find the compiled shaders
- What vertex format the shaders expect (locations, formats, offsets)

---

**Example Usage:**

```
// Shader definition declares expected inputs
public static class Shaders {
    public static readonly ShaderDefinition ColoredGeometry = new() {
        Name = "ColoredGeometry",
        VertexShaderPath = "Shaders/colored.vert.spv",
        FragmentShaderPath = "Shaders/colored.frag.spv",
        InputDescription = new VertexInputDescription {
            // Location 0: vec2 position
            // Location 1: vec3 color
            ...
        }
    };
}

// Geometry definition is just data
public static class Geometry {
    public static readonly GeometryDefinition ColoredQuad = new() {
        Name = "ColoredQuad",
        VertexData = [/* Vertex<Vec2, Vec3> data */],
        VertexStride = 20  // 2*4 + 3*4 bytes
    };
}

// Component usage
protected override void OnActivate() {
    var geometry = resources.Geometry.GetOrCreate(Geometry.ColoredQuad);
    var shader = resources.Shaders.GetOrCreate(Shaders.ColoredGeometry);
    
    // Pipeline uses shader's input description to interpret geometry buffer
    var pipeline = pipelineManager.GetOrCreatePipeline(new PipelineDescriptor {
        Shader = shader,  // Includes VertexInputDescription
        // ... other pipeline config
    });
    
    // DrawCommand just needs the buffer handle
    yield return new DrawCommand {
        Pipeline = pipeline,
        VertexBuffer = geometry.Buffer,
        VertexCount = geometry.VertexCount
    };
}
```

---

**Benefits:**
- ✅ Geometry and shaders are independent resources
- ✅ One shader can work with multiple geometry buffers (same format)
- ✅ One geometry buffer can be used with multiple shaders (if formats match)
- ✅ Clear separation: geometry = data, shader = interpretation
- ✅ Dynamic geometry works: update buffer data, shader interpretation unchanged
- ✅ Validation: pipeline can verify geometry stride matches shader expectations

**Validation Point:**

**Validation happens at binding time** (when geometry is used with a shader/pipeline), not at pipeline creation.

**Why binding-time validation?**
- ✅ Geometry can change over pipeline lifetime (dynamic meshes, swapping)
- ✅ One pipeline can be used with different geometries
- ✅ Validates actual runtime usage, not just creation-time configuration
- ✅ Catches errors when geometry changes

**ShaderDefinition provides validation method:**

```
public interface IShaderDefinition : IResourceDefinition {
    string VertexShaderPath { get; }
    string FragmentShaderPath { get; }
    VertexInputDescription InputDescription { get; }
    
    // Validates that geometry is compatible with this shader
    void ValidateGeometry(GeometryResource geometry);
}

// Implementation example
public class ColoredGeometryShader : IShaderDefinition {
    public void ValidateGeometry(GeometryResource geometry) {
        var expectedStride = InputDescription.Bindings[0].Stride;
        if (geometry.Stride != expectedStride) {
            throw new InvalidOperationException(
                $"Geometry '{geometry.Name}' has stride {geometry.Stride} bytes, " +
                $"but shader '{Name}' expects {expectedStride} bytes"
            );
        }
        
        // Could also validate attribute counts, formats, etc.
    }
}
```

**When validation is called:**

**Option 1: Renderer validates before each draw**
```
// In Renderer.Draw(commandBuffer, drawCommand)
drawCommand.Pipeline.Shader.ValidateGeometry(drawCommand.Geometry);
context.VulkanApi.CmdBindVertexBuffers(...);
```
- Pro: Catches all incompatibilities
- Con: Performance overhead (every draw call)

**Option 2: Component validates when geometry changes**
```
// In component when swapping geometry
protected override void OnUpdate(double deltaTime) {
    if (needsNewGeometry) {
        _geometry = resources.Geometry.GetOrCreate(Geometry.ColoredCube);
        _shader.ValidateGeometry(_geometry);  // Validate when changing
    }
}
```
- Pro: No per-frame overhead
- Con: Requires component discipline

**Option 3: Conditional validation (debug builds only)**
```
#if DEBUG
    drawCommand.Pipeline.Shader.ValidateGeometry(drawCommand.Geometry);
#endif
```
- Pro: Best of both worlds - safety during development, performance in release
- Con: Bugs might slip through to release

**Recommended: Option 3 (debug-only validation)**
- Validates during development/testing
- Zero overhead in release builds
- Can optionally enable in release via settings for troubleshooting

---

**Updated Architecture:**

**Pipeline uses Builder pattern for flexibility:**

```
// Build pipeline with fluent API
var pipeline = new PipelineBuilder()
    .WithShader(shader)  // ShaderResource with VertexInputDescription
    .WithRenderPass(renderPass)
    .WithTopology(PrimitiveTopology.TriangleList)
    .EnableDepthTest()
    .EnableBlending()
    .Build();

// Or with PipelineManager caching
var pipeline = pipelineManager.GetOrCreate(builder => builder
    .WithShader(shader)
    .WithRenderPass(renderPass)
    // ...
);
```

**Validation in Builder or at Draw:**

```
// Option A: Validate in PipelineBuilder.Build()
// - Can't validate yet (don't know geometry)

// Option B: Pipeline stores shader, validates at draw time
public class Pipeline {
    internal IShaderDefinition ShaderDefinition { get; }
    internal ShaderModule VertexShader { get; }
    internal ShaderModule FragmentShader { get; }
    // ... other pipeline state
    
    internal Pipeline(PipelineBuilder builder) {
        ShaderDefinition = builder.ShaderDefinition;
        // ...
    }
}

// Renderer validates before binding
private void Draw(CommandBuffer commandBuffer, DrawCommand drawCommand) {
    #if DEBUG
    drawCommand.Pipeline.ShaderDefinition.ValidateGeometry(drawCommand.Geometry);
    #endif
    
    context.VulkanApi.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, drawCommand.Pipeline);
    context.VulkanApi.CmdBindVertexBuffers(commandBuffer, 0, 1, drawCommand.VertexBuffer, ...);
    context.VulkanApi.CmdDraw(...);
}
```

**Benefits of Builder Pattern:**
- ✅ Flexible configuration (only specify what you need)
- ✅ Immutable pipelines after build
- ✅ Easy to add new configuration options
- ✅ Clear, readable component code
- ✅ PipelineManager can cache based on builder configuration

**Component Usage:**

```
protected override void OnActivate() {
    _shader = resources.Shaders.GetOrCreate(Shaders.ColoredGeometry);
    _geometry = resources.Geometry.GetOrCreate(Geometry.ColoredQuad);
    
    _pipeline = pipelineManager.GetOrCreate(builder => builder
        .WithShader(_shader)
        .WithRenderPass(swapChain.Passes[0])
        .WithTopology(PrimitiveTopology.TriangleFan)
        .Build()
    );
}

public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context) {
    yield return new DrawCommand {
        Pipeline = _pipeline,
        VertexBuffer = _geometry.Buffer,
        VertexCount = _geometry.VertexCount
    };
}
```

This separates concerns:
- **PipelineBuilder**: Configures pipeline state
- **Pipeline**: Immutable handle to Vulkan pipeline, stores ShaderDefinition for validation
- **ShaderDefinition**: Validates geometry compatibility at binding time
- **Component**: Builds pipeline, provides geometry to draw commands

---

**This means:**
- `IGeometryDefinition` → provides vertex data bytes + stride (no VertexInputDescription)
- `IShaderDefinition` → provides shader paths + VertexInputDescription
- Component's job: Choose compatible geometry + shader combinations
- Pipeline's job: Validate compatibility and use shader's descriptor

---

## Proposed Implementation Plan

## Implementation Plan

### Phase 1: Core Infrastructure
1. Define generic `Vertex<TPosition, TAttribute>` structs (1-attribute, 2-attribute variants)
2. Create `IBufferManager` interface and implementation
3. Implement `GeometryResource` class (handles for buffer, memory, metadata)
4. Design `IGeometrySubManager` interface with GetOrCreate, reference counting
5. Implement `GeometrySubManager` class
6. Wire GeometrySubManager into ResourceManager
7. Resolve VertexInputDescription ownership question

### Phase 2: Standard Geometry
8. Create static `Geometry` class with primitive definitions
9. Implement `ColoredQuadDefinition` using generic vertex
10. Test geometry loading and caching

### Phase 3: Integration
11. Update `HelloQuad` to use `resources.Geometry.GetOrCreate()`
12. Remove manual buffer management (~100 lines deleted)
13. Test rendering still works
14. Verify resource sharing between multiple components

### Phase 4: Documentation & Polish
15. Update `.docs/Project Structure.md`
16. Add XML documentation to all public APIs
17. Add unit tests for GeometrySubManager
18. Add unit tests for reference counting

---

## Open Questions

### 1. VertexInputDescription Ownership
**Status:** Needs discussion
**Question:** Should geometry or shader own the VertexInputDescription?
**Impact:** Affects how components coordinate geometry with shaders
**Considerations:**
- Dynamic mesh manipulation in OnUpdate
- Shader/geometry compatibility validation
- Where does the coordination happen?

### 2. Index Buffer Support
**Status:** Deferred
**Decision:** Skip in first iteration, HelloQuad doesn't need it
**Future:** Can add without breaking changes

### 3. Dynamic Geometry
**Status:** Deferred  
**Decision:** Static geometry only for now
**Future:** May need `IDynamicGeometryDefinition` or update mechanisms

### 4. Complex Mesh Loading
**Status:** Deferred
**Decision:** Focus on primitive geometry first
**Future:** Add support for OBJ, FBX, custom formats

---

## What's Needed to Remove Geometry from HelloQuad?

**If we leave pipeline implementation as-is**, here's what needs to be implemented to remove geometry definition and memory management:

### Required Components:

**1. Generic Vertex Struct**

**Decision: Use generic attribute names (TAttr1, TAttr2, etc.) NOT semantic names (TColor, TTexCoord)**

**Rationale:**

❌ **Semantic naming approach:**
```csharp
Vertex<TPosition, TColor>
Vertex<TPosition, TTexCoord>
Vertex<TPosition, TColor, TTexCoord>
Vertex<TPosition, TNormal, TTexCoord>
// etc... still leads to type explosion
```
Problems:
- Attributes can be used for different purposes (Attribute1 could be color OR normal OR tangent)
- Type explosion returns: need combinations for every semantic use case
- Rigid: forces semantics into type names
- Doesn't match Vulkan's location-based model

✅ **Generic attribute approach (RECOMMENDED):**
```csharp
// Position + 1 attribute
public struct Vertex<TPosition, TAttr1>
    where TPosition : unmanaged
    where TAttr1 : unmanaged
{
    public TPosition Position;
    public TAttr1 Attribute1;
}

// Position + 2 attributes
public struct Vertex<TPosition, TAttr1, TAttr2>
    where TPosition : unmanaged
    where TAttr1 : unmanaged
    where TAttr2 : unmanaged
{
    public TPosition Position;
    public TAttr1 Attribute1;
    public TAttr2 Attribute2;
}

// Position + 3 attributes
public struct Vertex<TPosition, TAttr1, TAttr2, TAttr3>
    where TPosition : unmanaged
    where TAttr1 : unmanaged
    where TAttr2 : unmanaged
    where TAttr3 : unmanaged
{
    public TPosition Position;
    public TAttr1 Attribute1;
    public TAttr2 Attribute2;
    public TAttr3 Attribute3;
}
```

**Benefits:**
- ✅ **Flexible**: Attribute1 can be color, normal, texcoord, tangent - whatever shader expects
- ✅ **Minimal types**: Just 3-4 structs covers all cases (1-4 attributes)
- ✅ **Matches Vulkan**: Shaders use location 0, 1, 2, 3 - attributes map naturally
- ✅ **No explosion**: Adding new semantic use cases doesn't require new types
- ✅ **Clear ordering**: Position always first, then attributes in order

**Usage Examples:**

```csharp
// Colored geometry: Position + Color
Vertex<Vector2D<float>, Vector3D<float>>
// Position at location 0, Color at location 1

// Textured geometry: Position + TexCoord
Vertex<Vector3D<float>, Vector2D<float>>
// Position at location 0, TexCoord at location 1

// Lit textured geometry: Position + Normal + TexCoord
Vertex<Vector3D<float>, Vector3D<float>, Vector2D<float>>
// Position at location 0, Normal at location 1, TexCoord at location 2

// Full PBR: Position + Normal + TexCoord + Tangent
Vertex<Vector3D<float>, Vector3D<float>, Vector2D<float>, Vector3D<float>>
// Position at location 0, Normal at 1, TexCoord at 2, Tangent at 3
```

**Documentation makes semantics clear:**
```csharp
public class ColoredQuadDefinition : IGeometryDefinition
{
    // Vertex format: Position(Vec2) + Color(Vec3)
    private static readonly Vertex<Vector2D<float>, Vector3D<float>>[] _vertices = [...];
}

public class TexturedQuadDefinition : IGeometryDefinition
{
    // Vertex format: Position(Vec3) + TexCoord(Vec2)
    private static readonly Vertex<Vector3D<float>, Vector2D<float>>[] _vertices = [...];
}
```

**Type Aliases for Readability (optional):**
```csharp
// If desired, can create aliases for common patterns
using ColoredVertex2D = Vertex<Vector2D<float>, Vector3D<float>>;
using TexturedVertex = Vertex<Vector3D<float>, Vector2D<float>>;
using LitTexturedVertex = Vertex<Vector3D<float>, Vector3D<float>, Vector2D<float>>;

// Usage becomes:
private static readonly ColoredVertex2D[] _vertices = [...];
```

---

**Implementation Plan:**

Create 4 vertex struct variants:
1. `Vertex<TPosition, TAttr1>` - position + 1 attribute
2. `Vertex<TPosition, TAttr1, TAttr2>` - position + 2 attributes  
3. `Vertex<TPosition, TAttr1, TAttr2, TAttr3>` - position + 3 attributes
4. `Vertex<TPosition, TAttr1, TAttr2, TAttr3, TAttr4>` - position + 4 attributes (if needed)

This covers 99% of use cases while keeping type count minimal.

---

**File Location:**
```csharp
// In: GameEngine/Resources/Geometry/Vertex.cs
namespace Nexus.GameEngine.Resources.Geometry;

public struct Vertex<TPosition, TAttr1> { ... }
public struct Vertex<TPosition, TAttr1, TAttr2> { ... }
public struct Vertex<TPosition, TAttr1, TAttr2, TAttr3> { ... }
public struct Vertex<TPosition, TAttr1, TAttr2, TAttr3, TAttr4> { ... }
```

**2. GeometryResource (Handle)**
```csharp
// In: GameEngine/Resources/Geometry/GeometryResource.cs
public class GeometryResource
{
    public Buffer Buffer { get; }
    public DeviceMemory Memory { get; }
    public uint VertexCount { get; }
    public uint Stride { get; }
    public string Name { get; }
}
```

**3. IGeometryDefinition (Data)**
```csharp
// In: GameEngine/Resources/Geometry/IGeometryDefinition.cs
public interface IGeometryDefinition : IResourceDefinition
{
    ReadOnlySpan<byte> GetVertexData();
    uint VertexCount { get; }
    uint Stride { get; }
}
```

**4. ColoredQuadDefinition (Concrete)**
```csharp
// In: GameEngine/Resources/Geometry/StandardGeometry.cs
public class ColoredQuadDefinition : IGeometryDefinition
{
    public string Name => "ColoredQuad";
    
    private static readonly Vertex<Vector2D<float>, Vector3D<float>>[] _vertices = [
        new() { Position = new(-0.5f, -0.5f), Attribute1 = new(1.0f, 0.0f, 0.0f) },
        new() { Position = new(-0.5f,  0.5f), Attribute1 = new(0.0f, 1.0f, 0.0f) },
        new() { Position = new( 0.5f,  0.5f), Attribute1 = new(0.0f, 0.0f, 1.0f) },
        new() { Position = new( 0.5f, -0.5f), Attribute1 = new(1.0f, 1.0f, 1.0f) },
    ];
    
    public uint VertexCount => (uint)_vertices.Length;
    public uint Stride => (uint)Marshal.SizeOf<Vertex<Vector2D<float>, Vector3D<float>>>();
    
    public ReadOnlySpan<byte> GetVertexData()
        => MemoryMarshal.AsBytes(_vertices.AsSpan());
}

public static class Geometry
{
    public static readonly ColoredQuadDefinition ColoredQuad = new();
}
```

**5. IBufferManager (Vulkan Operations)**
```csharp
// In: GameEngine/Graphics/Buffers/IBufferManager.cs
public interface IBufferManager
{
    (Buffer, DeviceMemory) CreateVertexBuffer(ReadOnlySpan<byte> data);
    void DestroyBuffer(Buffer buffer, DeviceMemory memory);
}

// Implementation extracts all the Vulkan code from HelloQuad.CreateVertexBuffer()
```

**6. IGeometrySubManager (Lifecycle)**
```csharp
// In: GameEngine/Resources/Geometry/IGeometrySubManager.cs
public interface IGeometrySubManager
{
    GeometryResource GetOrCreate(IGeometryDefinition definition);
    void Release(IGeometryDefinition definition);
}

// Implementation:
// - Caches by definition.Name
// - Uses IBufferManager to create buffers
// - Tracks reference counts
// - Handles cleanup
```

**7. Wire into IResourceManager**
```csharp
// In: GameEngine/Resources/IResourceManager.cs
public interface IResourceManager
{
    IGeometrySubManager Geometry { get; }
    // IShaderSubManager Shaders { get; }  // Future
    // ITextureSubManager Textures { get; } // Future
}
```

---

### HelloQuad Changes:

**Remove these (~100 lines):**
```csharp
// DELETE: Custom vertex struct
private struct Vertex { ... }

// DELETE: Vertex array data
private Vertex[] verts = [...];

// DELETE: Buffer fields
private Silk.NET.Vulkan.Buffer _vertexBuffer;
private DeviceMemory _vertexBufferMemory;
private bool _initialized;

// DELETE: CreateVertexBuffer() method (~50 lines)
private unsafe void CreateVertexBuffer() { ... }

// DELETE: FindMemoryType() method (~15 lines)
private uint FindMemoryType(...) { ... }

// DELETE: Cleanup in OnDeactivate (~10 lines)
protected override void OnDeactivate() {
    if (_initialized) {
        context.VulkanApi.DestroyBuffer(...);
        context.VulkanApi.FreeMemory(...);
    }
}
```

**Add these (~10 lines):**
```csharp
// NEW: Add IResourceManager dependency
public class HelloQuad(
    IGraphicsContext context,
    IPipelineManager pipelineManager,
    ISwapChain swapChain,
    IResourceManager resources)  // NEW
    : RenderableBase(), IRenderable, ITestComponent

// NEW: Hold geometry resource
private GeometryResource? _geometry;

// NEW: Get geometry in OnActivate
protected override void OnActivate()
{
    base.OnActivate();
    CreatePipeline();  // Keep as-is for now
    _geometry = resources.Geometry.GetOrCreate(Geometry.ColoredQuad);  // NEW
}

// MODIFY: Use geometry buffer in GetDrawCommands
public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
{
    if (_geometry == null) yield break;
    
    yield return new DrawCommand
    {
        RenderMask = RenderMask,
        Pipeline = _pipeline,
        VertexBuffer = _geometry.Buffer,        // Changed from _vertexBuffer
        VertexCount = _geometry.VertexCount,    // Changed from (uint)verts.Length
        InstanceCount = 1
    };
    
    FramesRendered++;
}

// DELETE: OnDeactivate cleanup (no longer needed)
```

---

### Net Result:

**Lines Removed:** ~100
- Vertex struct definition
- Vertex data array
- CreateVertexBuffer() implementation
- FindMemoryType() implementation
- Buffer/memory fields
- Cleanup code

**Lines Added:** ~10
- IResourceManager dependency
- GeometryResource field
- GetOrCreate call

**HelloQuad goes from ~375 lines to ~285 lines** (just by removing geometry management)

---

### Files to Create:

1. `GameEngine/Resources/Geometry/Vertex.cs` - Generic vertex struct
2. `GameEngine/Resources/Geometry/GeometryResource.cs` - Resource handle
3. `GameEngine/Resources/Geometry/IGeometryDefinition.cs` - Update interface
4. `GameEngine/Resources/Geometry/StandardGeometry.cs` - Definitions + static class
5. `GameEngine/Resources/Geometry/IGeometrySubManager.cs` - Interface
6. `GameEngine/Resources/Geometry/GeometrySubManager.cs` - Implementation
7. `GameEngine/Graphics/Buffers/IBufferManager.cs` - Interface
8. `GameEngine/Graphics/Buffers/BufferManager.cs` - Implementation (extract from HelloQuad)
9. `GameEngine/Resources/IResourceManager.cs` - Update interface
10. `GameEngine/Resources/ResourceManager.cs` - Update implementation

### DI Registration:

```csharp
// In service configuration
services.AddSingleton<IBufferManager, BufferManager>();
services.AddSingleton<IGeometrySubManager, GeometrySubManager>();
services.AddSingleton<IResourceManager, ResourceManager>();
```

---

**Ready to proceed with implementation?**