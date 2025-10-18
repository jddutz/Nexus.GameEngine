# Pipeline Builder Design Analysis
*Generated: October 17, 2025*

## Executive Summary

The Pipeline Builder design document proposes a fluent builder pattern for creating Vulkan graphics pipelines. This analysis compares the design against the current codebase implementation and identifies gaps, dependencies, and implementation considerations.

---

## Current State Analysis

### 1. Pipeline Type

**Current Implementation:**
- `Pipeline` is **Silk.NET.Vulkan.Pipeline** struct (not a custom class)
- Direct Vulkan handle used throughout the codebase
- No wrapper class with validation or metadata

**Design Document Assumption:**
```csharp
public class Pipeline
{
    internal Handle VulkanPipeline { get; }
    internal IShaderDefinition ShaderDefinition { get; }
    // ...
}
```

**Gap:**
The design assumes a custom `Pipeline` class wrapper, but the codebase uses Silk.NET's Pipeline struct directly. This is a **fundamental architectural difference**.

---

### 2. Shader Resource System

**Current Implementation Status:**

**✅ IShaderDefinition Interface:** Fully implemented
```csharp
public interface IShaderDefinition : IResourceDefinition
{
    string VertexShaderPath { get; }
    string FragmentShaderPath { get; }
    VertexInputDescription InputDescription { get; }
    void ValidateGeometry(GeometryResource geometry);
}
```

**✅ ShaderResource Class:** Partially implemented
```csharp
public class ShaderResource
{
    public ShaderModule VertexShader { get; }
    public ShaderModule FragmentShader { get; }
    public IShaderDefinition Definition { get; }
    public string Name { get; }
}
```

**❌ ShaderResourceManager:** Stub implementation only
```csharp
public ShaderResource GetOrCreate(IShaderDefinition definition)
{
    // TODO: Implement shader loading from SPIR-V files
    throw new NotImplementedException("Shader loading not yet implemented");
}
```

**Gap:**
- Shader resources structure exists but loading functionality is not implemented
- Cannot load SPIR-V files from embedded resources
- Pipeline builder would depend on non-functional shader system

---

### 3. PipelineManager Current Architecture

**Current Implementation:**
```csharp
public unsafe class PipelineManager : IPipelineManager
{
    // Uses PipelineDescriptor for configuration
    public Pipeline GetOrCreatePipeline(PipelineDescriptor descriptor);
    
    // Caching by name (string key)
    private readonly ConcurrentDictionary<string, CachedPipeline> _pipelines;
    
    // Loads shaders from embedded resources directly
    private ShaderModule CreateShaderModule(string shaderPath);
}
```

**Key Characteristics:**
- ✅ Thread-safe caching with ConcurrentDictionary
- ✅ Shader hot-reload support via shader dependency tracking
- ✅ Error pipeline fallback (placeholder)
- ✅ Loads SPIR-V from embedded resources
- ✅ Statistics tracking (cache hits, misses, creation time)
- ❌ Uses PipelineDescriptor (record) not builder pattern
- ❌ Creates shader modules internally (not from ShaderResourceManager)

---

### 4. PipelineDescriptor vs PipelineBuilder

**Current PipelineDescriptor:**
```csharp
public record PipelineDescriptor
{
    // Required
    public required string Name { get; init; }
    public required string VertexShaderPath { get; init; }
    public required string FragmentShaderPath { get; init; }
    public required VertexInputDescription VertexInputDescription { get; init; }
    public required RenderPass RenderPass { get; init; }
    
    // Optional with defaults
    public PrimitiveTopology Topology { get; init; } = PrimitiveTopology.TriangleList;
    public bool EnableDepthTest { get; init; } = true;
    public bool EnableDepthWrite { get; init; } = true;
    public bool EnableBlending { get; init; } = false;
    public CullModeFlags CullMode { get; init; } = CullModeFlags.BackBit;
    // ... many more properties
}
```

**Proposed PipelineBuilder:**
```csharp
public class PipelineBuilder
{
    public PipelineBuilder WithShader(ShaderResource shader);
    public PipelineBuilder WithRenderPass(RenderPass renderPass);
    public PipelineBuilder WithTopology(PrimitiveTopology topology);
    // ... fluent methods
    public Pipeline Build();
}
```

**Comparison:**

| Aspect | PipelineDescriptor | PipelineBuilder |
|--------|-------------------|-----------------|
| Pattern | Data transfer object | Builder pattern |
| Immutability | Immutable record | Mutable during build |
| Shader Reference | String paths | ShaderResource object |
| Validation | At pipeline creation | At Build() time |
| Caching | By name string | By configuration hash |
| VertexInputDescription | Explicit property | Implicit from shader |
| Usage | Create descriptor, pass to manager | Fluent method chain |

---

### 5. Current Usage Pattern (HelloQuad)

```csharp
protected override void OnActivate()
{
    // 1. Create geometry
    _geometry = resources.Geometry.GetOrCreate(Geometry.ColorQuad);
    
    // 2. Create pipeline descriptor
    var descriptor = new PipelineDescriptor
    {
        Name = "HelloQuadPipeline",
        VertexShaderPath = "Shaders/vert.spv",
        FragmentShaderPath = "Shaders/frag.spv",
        VertexInputDescription = Geometry.ColorQuad.GetVertexInputDescription(),
        Topology = PrimitiveTopology.TriangleFan,
        RenderPass = swapChain.Passes[(int)Math.Log2(RenderPasses.Main)],
        EnableDepthTest = true,
        EnableDepthWrite = true,
        CullMode = CullModeFlags.None,
    };
    
    // 3. Get or create pipeline
    _pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
}
```

**Observations:**
- VertexInputDescription comes from geometry definition (not shader)
- Shader paths are string literals
- Requires bit manipulation to get render pass from mask
- Descriptor creation is verbose but explicit

---

## Design Document vs Implementation Gaps

### Gap 1: Pipeline Type Mismatch
**Issue:** Design assumes custom Pipeline wrapper class  
**Reality:** Uses Silk.NET.Vulkan.Pipeline struct directly  
**Impact:** Cannot add ShaderDefinition property or ValidateGeometry method to Pipeline  
**Resolution Options:**
1. Create custom Pipeline wrapper (breaking change, affects all rendering code)
2. Keep Vulkan Pipeline, move validation to PipelineBuilder or PipelineManager
3. Store shader validation metadata separately in PipelineManager cache

**Recommendation:** Option 3 - Store IShaderDefinition in CachedPipeline alongside Pipeline handle

### Gap 2: Shader Loading Not Implemented
**Issue:** ShaderResourceManager is a stub  
**Reality:** PipelineManager loads shaders directly via CreateShaderModule()  
**Impact:** Cannot use ShaderResource in PipelineBuilder yet  
**Resolution:** Implement shader loading in ShaderResourceManager first, OR delay builder until shader system is complete

**Recommendation:** Extract PipelineManager's CreateShaderModule() logic into ShaderResourceManager

### Gap 3: Validation Location
**Design:** Pipeline.ValidateGeometry() called at draw time  
**Reality:** No validation infrastructure exists  
**Impact:** Cannot validate geometry-shader compatibility  
**Resolution:** Implement validation in:
- PipelineBuilder.Build() (at pipeline creation)
- Renderer.Draw() (at draw time, debug only)
- Requires storing IShaderDefinition with pipeline

**Recommendation:** Add debug validation in Renderer.Draw() using stored shader definitions

### Gap 4: Caching Strategy Difference
**Design:** Cache by builder configuration hash  
**Reality:** Cache by name string  
**Impact:** Cannot detect equivalent configurations with different names  
**Resolution:** 
- Keep name-based caching for explicit control
- Add configuration-based lookup for deduplication
- Builder can generate deterministic names from configuration

**Recommendation:** Keep current name-based caching, add optional config hash fallback

### Gap 5: RenderPass Access Pattern
**Current:** Awkward bit manipulation to get pass from mask  
```csharp
swapChain.Passes[(int)Math.Log2(RenderPasses.Main)]
```
**Better:** Direct pass retrieval method  
```csharp
swapChain.GetPass(RenderPasses.Main)
```

**Recommendation:** Add helper method to ISwapChain for cleaner pass access

---

## Dependencies for Implementation

### Must Have Before PipelineBuilder:
1. ✅ IShaderDefinition interface (exists)
2. ✅ ShaderResource class (exists)
3. ❌ ShaderResourceManager.GetOrCreate() implementation (BLOCKER)
4. ❌ Decision on Pipeline wrapper vs Silk.NET struct (DESIGN)

### Should Have:
1. Validation infrastructure (can be added incrementally)
2. Better RenderPass access API (quality of life)
3. Example shader definitions (for testing)

### Nice to Have:
1. Configuration-based caching
2. Pipeline statistics/debugging tools
3. Hot-reload integration

---

## Proposed Implementation Path

### Phase 1: Foundation (Required First)
1. **Implement ShaderResourceManager.GetOrCreate()**
   - Extract CreateShaderModule() from PipelineManager
   - Move to ShaderResourceManager
   - Add caching and reference counting
   - Return ShaderResource with modules

2. **Decide Pipeline Architecture**
   - Option A: Create Pipeline wrapper class (breaking change)
   - Option B: Store shader metadata in PipelineManager cache (minimal change)
   - **Recommend Option B** for incremental migration

3. **Update CachedPipeline to Store Shader Definition**
   ```csharp
   private record CachedPipeline(
       Pipeline Handle,
       PipelineLayout Layout,
       PipelineDescriptor Descriptor,
       IShaderDefinition ShaderDefinition,  // NEW
       DateTime CreatedAt,
       DateTime LastAccessedAt,
       int AccessCount
   );
   ```

### Phase 2: Builder Implementation
1. **Create PipelineBuilder Class**
   ```csharp
   public class PipelineBuilder
   {
       private ShaderResource? _shader;
       private RenderPass? _renderPass;
       private PrimitiveTopology _topology = PrimitiveTopology.TriangleList;
       // ... other state
       
       public PipelineBuilder WithShader(ShaderResource shader)
       {
           _shader = shader;
           return this;
       }
       
       // ... other fluent methods
       
       public PipelineDescriptor Build()
       {
           ValidateRequiredFields();
           return new PipelineDescriptor
           {
               Name = GenerateName(),
               VertexShaderPath = _shader!.Definition.VertexShaderPath,
               FragmentShaderPath = _shader!.Definition.FragmentShaderPath,
               VertexInputDescription = _shader!.Definition.InputDescription,
               RenderPass = _renderPass!,
               Topology = _topology,
               // ... map all fields
           };
       }
   }
   ```

2. **Add Builder Support to IPipelineManager**
   ```csharp
   public interface IPipelineManager
   {
       // Existing
       Pipeline GetOrCreatePipeline(PipelineDescriptor descriptor);
       
       // New
       Pipeline GetOrCreate(Func<PipelineBuilder, PipelineDescriptor> configure);
   }
   ```

### Phase 3: Validation
1. **Add Debug Validation to Renderer**
   ```csharp
   private void Draw(CommandBuffer commandBuffer, DrawCommand drawCommand)
   {
       #if DEBUG
       var shaderDef = _pipelineManager.GetShaderDefinition(drawCommand.Pipeline);
       if (shaderDef != null)
       {
           shaderDef.ValidateGeometry(/* get from draw command */);
       }
       #endif
       
       // ... existing draw code
   }
   ```

2. **Add Geometry Parameter to DrawCommand** (if not exists)
   - Need to pass GeometryResource for validation
   - Or pass stride/format info

### Phase 4: Migration & Refinement
1. Update HelloQuad to use builder pattern
2. Add convenience methods for common patterns
3. Implement hot-reload with builder
4. Add configuration-based caching

---

## Key Design Decisions Needed

### Decision 1: Pipeline Wrapper
**Question:** Should we wrap Silk.NET.Vulkan.Pipeline in a custom class?

**Option A: Custom Wrapper**
- ✅ Can add metadata (ShaderDefinition, validation)
- ✅ Better encapsulation
- ❌ Breaking change to all rendering code
- ❌ Extra allocation per pipeline

**Option B: Struct with Metadata Storage**
- ✅ No breaking changes
- ✅ Same memory footprint
- ❌ Less type-safe
- ❌ Metadata separate from handle

**Recommendation:** **Option B** - Store metadata in PipelineManager's cache. The Vulkan Pipeline handle is used in performance-critical rendering paths, adding a wrapper would impact hot paths.

### Decision 2: Builder Returns What?
**Question:** Should Build() return Pipeline or PipelineDescriptor?

**Option A: Build() → Pipeline**
```csharp
var pipeline = new PipelineBuilder()
    .WithShader(shader)
    .Build();  // Returns Pipeline directly
```
- ✅ Matches design document
- ❌ Builder needs access to PipelineManager
- ❌ Can't cache by configuration

**Option B: Build() → PipelineDescriptor**
```csharp
var descriptor = new PipelineBuilder()
    .WithShader(shader)
    .Build();  // Returns descriptor
    
var pipeline = pipelineManager.GetOrCreate(descriptor);
```
- ✅ Separation of concerns
- ✅ Descriptor can still be cached
- ✅ Builder is pure data transformation
- ❌ Extra step

**Option C: Manager Method with Builder**
```csharp
var pipeline = pipelineManager.GetOrCreate(builder => builder
    .WithShader(shader)
    .WithRenderPass(pass)
);
```
- ✅ Fluent and direct
- ✅ Manager controls pipeline creation
- ✅ Caching still works
- ✅ Matches design document's "PipelineManager Integration" section

**Recommendation:** **Option C** - Best of both worlds. Builder creates configuration, manager creates pipeline.

### Decision 3: Shader Loading Strategy
**Question:** Should ShaderResourceManager load shaders, or PipelineManager?

**Current:** PipelineManager loads shaders via CreateShaderModule()

**Proposed:** ShaderResourceManager loads, PipelineManager uses ShaderResource

**Benefits:**
- ✅ Separation of concerns
- ✅ Shader caching independent of pipelines
- ✅ Shader hot-reload easier
- ✅ Multiple pipelines can share same shader

**Migration:**
1. Move CreateShaderModule() to ShaderResourceManager
2. PipelineManager calls ShaderResourceManager.GetOrCreate()
3. Store ShaderResource reference in CachedPipeline
4. Use stored ShaderModules in CreatePipeline()

**Recommendation:** Migrate shader loading to ShaderResourceManager

---

## API Design Proposal

### PipelineBuilder (Final Design)

```csharp
public class PipelineBuilder
{
    private ShaderResource? _shader;
    private RenderPass? _renderPass;
    private string? _name;
    
    // Optional with defaults
    private PrimitiveTopology _topology = PrimitiveTopology.TriangleList;
    private CullModeFlags _cullMode = CullModeFlags.BackBit;
    private FrontFace _frontFace = FrontFace.CounterClockwise;
    private bool _enableDepthTest = true;
    private bool _enableDepthWrite = true;
    private bool _enableBlending = false;
    private uint _subpass = 0;
    
    // More options...
    private BlendFactor _srcBlend = BlendFactor.One;
    private BlendFactor _dstBlend = BlendFactor.Zero;
    private CompareOp _depthCompare = CompareOp.Less;
    
    // Required
    public PipelineBuilder WithShader(ShaderResource shader)
    {
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        return this;
    }
    
    public PipelineBuilder WithRenderPass(RenderPass renderPass)
    {
        _renderPass = renderPass;
        return this;
    }
    
    public PipelineBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    // Optional with fluent defaults
    public PipelineBuilder WithTopology(PrimitiveTopology topology)
    {
        _topology = topology;
        return this;
    }
    
    public PipelineBuilder WithCullMode(CullModeFlags cullMode)
    {
        _cullMode = cullMode;
        return this;
    }
    
    public PipelineBuilder WithFrontFace(FrontFace frontFace)
    {
        _frontFace = frontFace;
        return this;
    }
    
    public PipelineBuilder EnableDepthTest(bool enable = true)
    {
        _enableDepthTest = enable;
        return this;
    }
    
    public PipelineBuilder EnableDepthWrite(bool enable = true)
    {
        _enableDepthWrite = enable;
        return this;
    }
    
    public PipelineBuilder EnableBlending(bool enable = true)
    {
        _enableBlending = enable;
        return this;
    }
    
    public PipelineBuilder WithBlendFactors(BlendFactor src, BlendFactor dst)
    {
        _srcBlend = src;
        _dstBlend = dst;
        return this;
    }
    
    public PipelineBuilder WithDepthCompare(CompareOp compare)
    {
        _depthCompare = compare;
        return this;
    }
    
    public PipelineBuilder WithSubpass(uint subpass)
    {
        _subpass = subpass;
        return this;
    }
    
    /// <summary>
    /// Builds a PipelineDescriptor from the configured builder.
    /// Internal method used by PipelineManager.
    /// </summary>
    internal PipelineDescriptor Build()
    {
        // Validate required fields
        if (_shader == null)
            throw new InvalidOperationException("Shader is required. Call WithShader().");
        if (_renderPass == null)
            throw new InvalidOperationException("RenderPass is required. Call WithRenderPass().");
        
        // Generate name if not provided
        var name = _name ?? GenerateAutomaticName();
        
        return new PipelineDescriptor
        {
            Name = name,
            VertexShaderPath = _shader.Definition.VertexShaderPath,
            FragmentShaderPath = _shader.Definition.FragmentShaderPath,
            VertexInputDescription = _shader.Definition.InputDescription,
            RenderPass = _renderPass.Value,
            Topology = _topology,
            CullMode = _cullMode,
            FrontFace = _frontFace,
            EnableDepthTest = _enableDepthTest,
            EnableDepthWrite = _enableDepthWrite,
            EnableBlending = _enableBlending,
            SrcBlendFactor = _srcBlend,
            DstBlendFactor = _dstBlend,
            DepthCompareOp = _depthCompare,
            Subpass = _subpass,
        };
    }
    
    private string GenerateAutomaticName()
    {
        // Generate deterministic name from configuration
        var hash = ComputeConfigurationHash();
        return $"Pipeline_{_shader!.Name}_{hash:X8}";
    }
    
    private int ComputeConfigurationHash()
    {
        var hashCode = new HashCode();
        hashCode.Add(_shader?.Name);
        hashCode.Add(_renderPass);
        hashCode.Add(_topology);
        hashCode.Add(_cullMode);
        hashCode.Add(_enableDepthTest);
        hashCode.Add(_enableBlending);
        // ... add all configuration
        return hashCode.ToHashCode();
    }
}
```

### IPipelineManager Extension

```csharp
public interface IPipelineManager : IDisposable
{
    // Existing methods
    Pipeline GetOrCreatePipeline(PipelineDescriptor descriptor);
    Pipeline Get(string name);
    
    // New builder method
    Pipeline GetOrCreate(Func<PipelineBuilder, PipelineBuilder> configure);
    
    // Validation support
    IShaderDefinition? GetShaderDefinition(Pipeline pipeline);
}
```

### Implementation in PipelineManager

```csharp
public Pipeline GetOrCreate(Func<PipelineBuilder, PipelineBuilder> configure)
{
    var builder = new PipelineBuilder();
    builder = configure(builder);
    var descriptor = builder.Build();
    return GetOrCreatePipeline(descriptor);
}

public IShaderDefinition? GetShaderDefinition(Pipeline pipeline)
{
    // Find pipeline in cache and return stored shader definition
    foreach (var cached in _pipelines.Values)
    {
        if (cached.Handle.Handle == pipeline.Handle)
        {
            return cached.ShaderDefinition;
        }
    }
    return null;
}
```

### Usage Examples

**Simple Usage:**
```csharp
var pipeline = pipelineManager.GetOrCreate(b => b
    .WithShader(shader)
    .WithRenderPass(renderPass)
);  // All defaults
```

**Configured Usage:**
```csharp
var pipeline = pipelineManager.GetOrCreate(b => b
    .WithShader(shader)
    .WithRenderPass(renderPass)
    .WithName("HelloQuadPipeline")
    .WithTopology(PrimitiveTopology.TriangleFan)
    .WithCullMode(CullModeFlags.None)
    .EnableDepthTest()
    .EnableDepthWrite()
);
```

**Compare with Current:**
```csharp
// BEFORE (Current)
var descriptor = new PipelineDescriptor
{
    Name = "HelloQuadPipeline",
    VertexShaderPath = "Shaders/vert.spv",
    FragmentShaderPath = "Shaders/frag.spv",
    VertexInputDescription = Geometry.ColorQuad.GetVertexInputDescription(),
    Topology = PrimitiveTopology.TriangleFan,
    RenderPass = swapChain.Passes[(int)Math.Log2(RenderPasses.Main)],
    EnableDepthTest = true,
    EnableDepthWrite = true,
    CullMode = CullModeFlags.None,
};
var pipeline = pipelineManager.GetOrCreatePipeline(descriptor);

// AFTER (With Builder)
var shader = resources.Shaders.GetOrCreate(Shaders.ColoredGeometry);
var pipeline = pipelineManager.GetOrCreate(b => b
    .WithName("HelloQuadPipeline")
    .WithShader(shader)  // VertexInputDescription comes from shader
    .WithRenderPass(swapChain.GetPass(RenderPasses.Main))  // Cleaner access
    .WithTopology(PrimitiveTopology.TriangleFan)
    .WithCullMode(CullModeFlags.None)
    .EnableDepthTest()
    .EnableDepthWrite()
);
```

---

## Summary

### Current State
- ✅ Shader definition interfaces exist
- ✅ Shader resource structure exists
- ✅ PipelineManager has robust caching and hot-reload
- ❌ Shader loading not implemented
- ❌ Pipeline is Silk.NET struct (not wrapper)
- ❌ No builder pattern yet

### Recommended Approach
1. **Phase 1:** Implement ShaderResourceManager.GetOrCreate()
2. **Phase 2:** Add IShaderDefinition storage to CachedPipeline
3. **Phase 3:** Implement PipelineBuilder class
4. **Phase 4:** Add PipelineManager.GetOrCreate(builder) method
5. **Phase 5:** Add validation infrastructure
6. **Phase 6:** Migrate HelloQuad example

### Key Design Choices
- **Pipeline Type:** Keep Silk.NET struct, store metadata in manager cache
- **Builder Output:** Builder creates PipelineDescriptor, manager creates Pipeline
- **Manager API:** Use `GetOrCreate(builder => builder.With...)` pattern
- **Shader Loading:** Migrate to ShaderResourceManager
- **Validation:** Store IShaderDefinition with pipeline, validate in Renderer (debug only)

### Benefits of Builder
- ✅ Cleaner, more readable API
- ✅ Automatic VertexInputDescription from shader
- ✅ Type-safe configuration
- ✅ Fluent, discoverable API
- ✅ Can coexist with PipelineDescriptor during migration
- ✅ Less boilerplate for common cases

### Migration Strategy
- Keep PipelineDescriptor for backward compatibility
- Add builder as alternative API
- Migrate examples incrementally
- Deprecate descriptor approach once stable
