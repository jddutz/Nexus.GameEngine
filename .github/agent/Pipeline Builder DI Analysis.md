# Pipeline Builder - Dependency Injection Analysis
*Generated: October 17, 2025*

## The DI Challenge

The core question: **How do components get access to PipelineBuilder in a DI context?**

---

## Option 1: No Injection - Manager Method with Lambda

**Pattern:**
```csharp
public class HelloQuad(
    IPipelineManager pipelineManager,
    ISwapChain swapChain,
    IResourceManager resources)  // ← Existing DI dependencies
{
    void CreatePipeline()
    {
        // Builder created inline, no DI needed
        var pipeline = pipelineManager.GetOrCreate(b => b
            .WithShader(shader)
            .WithRenderPass(renderPass)
            .WithTopology(PrimitiveTopology.TriangleFan)
        );
    }
}
```

**Implementation:**
```csharp
public interface IPipelineManager
{
    Pipeline GetOrCreate(Func<PipelineBuilder, PipelineBuilder> configure);
}

public class PipelineManager : IPipelineManager
{
    public Pipeline GetOrCreate(Func<PipelineBuilder, PipelineBuilder> configure)
    {
        var builder = new PipelineBuilder();  // Manager creates it
        builder = configure(builder);          // Component configures it
        var descriptor = builder.Build();      // Builder creates descriptor
        return GetOrCreatePipeline(descriptor); // Manager creates pipeline
    }
}
```

### ✅ Pros:
- **Zero new DI dependencies** - components already need `IPipelineManager`
- **Builder is transient** - created per-use, no lifecycle concerns
- **Clean component constructors** - no additional parameters
- **Builder is pure data transformation** - no services needed
- **Fluent and readable** - matches design document

### ❌ Cons:
- Builder can't be unit tested independently (minor issue)
- Less flexible if builder needs services later (solvable)

### DI Impact: **NONE** ✅
Components inject the same services they already use. Builder is an implementation detail.

---

## Option 2: Inject PipelineBuilder

**Pattern:**
```csharp
public class HelloQuad(
    IPipelineManager pipelineManager,
    ISwapChain swapChain,
    IResourceManager resources,
    PipelineBuilder pipelineBuilder)  // ← NEW dependency
{
    void CreatePipeline()
    {
        var pipeline = pipelineBuilder
            .WithShader(shader)
            .WithRenderPass(renderPass)
            .Build();  // Returns descriptor
            
        _pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
    }
}
```

**DI Registration:**
```csharp
services.AddTransient<PipelineBuilder>();  // NEW
```

### ❌ Cons:
- **Every component needs PipelineBuilder injection** - adds constructor parameter
- **Stateful builder requires transient scope** - creates new instance each time
- **Two-step process** - build descriptor, then get pipeline
- **More DI complexity** - another service to register and inject

### ✅ Pros:
- Builder can be unit tested independently
- Builder could hold services if needed (e.g., ILoggerFactory)

### DI Impact: **HIGH** ❌
Every component that creates pipelines needs a new constructor parameter.

---

## Option 3: IPipelineManager.GetBuilder()

**Pattern:**
```csharp
public class HelloQuad(
    IPipelineManager pipelineManager,
    ISwapChain swapChain,
    IResourceManager resources)  // ← No new dependencies!
{
    void CreatePipeline()
    {
        var builder = pipelineManager.GetBuilder();  // Get from manager
        
        var pipeline = builder
            .WithShader(shader)
            .WithRenderPass(renderPass)
            .Build();  // Returns Pipeline or descriptor?
    }
}
```

**Implementation:**
```csharp
public interface IPipelineManager
{
    PipelineBuilder GetBuilder();
    Pipeline GetOrCreatePipeline(PipelineDescriptor descriptor);
}

public class PipelineManager : IPipelineManager
{
    public PipelineBuilder GetBuilder()
    {
        return new PipelineBuilder();  // Or: return new PipelineBuilder(this);
    }
}
```

### Issues:
- **What does Build() return?**
  - If `Pipeline`: Builder needs reference to manager → circular dependency
  - If `PipelineDescriptor`: Still need to call manager → two steps
- **Builder stateful** - must create new instance each call
- **Awkward API** - extra method call to get builder

### ✅ Pros:
- No new DI dependencies
- Builder could be created with manager reference

### ❌ Cons:
- Extra step: get builder, then build
- If builder needs manager reference, circular dependency
- Less fluent than Option 1

### DI Impact: **NONE**, but API is awkward ⚠️

---

## Option 4: Builder as Factory with DI

**Pattern:**
```csharp
public class HelloQuad(
    IPipelineManager pipelineManager,
    ISwapChain swapChain,
    IResourceManager resources,
    IPipelineBuilderFactory builderFactory)  // ← NEW dependency
{
    void CreatePipeline()
    {
        var builder = builderFactory.Create();
        var pipeline = builder
            .WithShader(shader)
            .WithRenderPass(renderPass)
            .Build();  // Returns descriptor
            
        _pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
    }
}
```

**Implementation:**
```csharp
public interface IPipelineBuilderFactory
{
    PipelineBuilder Create();
}

public class PipelineBuilderFactory : IPipelineBuilderFactory
{
    public PipelineBuilder Create() => new PipelineBuilder();
}
```

### DI Impact: **MEDIUM** ❌
Adds factory dependency to all components. Over-engineered for simple use case.

---

## Comparison Matrix

| Aspect | Option 1: Lambda | Option 2: Inject Builder | Option 3: GetBuilder() | Option 4: Factory |
|--------|------------------|-------------------------|------------------------|-------------------|
| **New DI Dependencies** | 0 | 1 per component | 0 | 1 per component |
| **Constructor Complexity** | None | +1 parameter | None | +1 parameter |
| **API Fluency** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **Code Lines** | Minimal | Medium | Medium | Most |
| **Testability** | High | Highest | High | Highest |
| **Simplicity** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| **Builder Lifecycle** | Per-call | Transient | Per-call | Per-call |
| **Manager Integration** | Built-in | External | Mixed | External |

---

## Deep Dive: Option 1 (Recommended)

### Why Lambda Pattern is Best for PipelineBuilder

**1. Builder is Pure Configuration**
```csharp
public class PipelineBuilder
{
    // Just data, no services needed
    private ShaderResource? _shader;
    private RenderPass? _renderPass;
    private PrimitiveTopology _topology = PrimitiveTopology.TriangleList;
    // ... configuration state
    
    // Pure transformation - no DI needed
    internal PipelineDescriptor Build()
    {
        ValidateRequiredFields();
        return new PipelineDescriptor { /* map fields */ };
    }
}
```

The builder doesn't need any services! It's just a configuration wrapper that creates a `PipelineDescriptor`.

**2. Manager Already Has All Services**
```csharp
public class PipelineManager : IPipelineManager
{
    private readonly IGraphicsContext _context;
    private readonly IWindowService _windowService;
    private readonly Vk _vk;
    // ... all services needed for pipeline creation
    
    public Pipeline GetOrCreate(Func<PipelineBuilder, PipelineBuilder> configure)
    {
        var builder = new PipelineBuilder();
        builder = configure(builder);
        var descriptor = builder.Build();
        return GetOrCreatePipeline(descriptor);  // Uses manager's services
    }
}
```

Manager already has everything needed to create pipelines. Builder just creates the configuration.

**3. Component Already Injects Manager**
```csharp
// CURRENT (with PipelineDescriptor)
public class HelloQuad(
    IPipelineManager pipelineManager,  // ← Already here
    ISwapChain swapChain,
    IResourceManager resources)
{
    var descriptor = new PipelineDescriptor { /* config */ };
    _pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
}

// WITH BUILDER (Option 1)
public class HelloQuad(
    IPipelineManager pipelineManager,  // ← Same dependencies!
    ISwapChain swapChain,
    IResourceManager resources)
{
    _pipeline = pipelineManager.GetOrCreate(b => b
        .WithShader(shader)
        .WithRenderPass(renderPass)
    );
}
```

**Zero change** to component constructors!

**4. Encapsulation**
Builder is an implementation detail of how pipelines are configured. Components don't need to know it exists as a separate class.

**5. Comparison to Other Patterns**

Similar successful patterns in .NET:
- **LINQ**: `collection.Where(x => x.Active)` - no builder injection needed
- **Entity Framework**: `dbContext.Users.Where(u => u.Active)` - lambda configuration
- **Minimal APIs**: `app.MapGet("/api", () => "Hello")` - lambda configuration
- **Options Pattern**: `services.Configure<MyOptions>(o => o.Value = 1)` - lambda configuration

All use lambdas to configure, no builder injection required.

---

## Addressing "What If Builder Needs Services?"

### Scenario: Builder needs logging or validation services

**Solution: Optional Manager-Provided Dependencies**
```csharp
public class PipelineBuilder
{
    private ILogger? _logger;  // Optional, provided by manager
    
    internal PipelineBuilder(ILogger? logger = null)
    {
        _logger = logger;
    }
    
    internal PipelineDescriptor Build()
    {
        _logger?.LogDebug("Building pipeline with shader: {Shader}", _shader?.Name);
        ValidateRequiredFields();
        return new PipelineDescriptor { /* ... */ };
    }
}

public class PipelineManager : IPipelineManager
{
    private readonly ILogger _logger;
    
    public Pipeline GetOrCreate(Func<PipelineBuilder, PipelineBuilder> configure)
    {
        var builder = new PipelineBuilder(_logger);  // Manager provides
        builder = configure(builder);
        var descriptor = builder.Build();
        return GetOrCreatePipeline(descriptor);
    }
}
```

**Components still don't need to inject anything!** Manager provides what builder needs.

---

## Real-World Example: Current vs Builder Pattern

### Current Pattern (PipelineDescriptor)
```csharp
public class HelloQuad(
    IPipelineManager pipelineManager,
    ISwapChain swapChain,
    IResourceManager resources)
{
    void CreatePipeline()
    {
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
        
        _pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
    }
}
```

**Constructor parameters:** 3

### Builder Pattern - Option 1 (Lambda)
```csharp
public class HelloQuad(
    IPipelineManager pipelineManager,
    ISwapChain swapChain,
    IResourceManager resources)
{
    void CreatePipeline()
    {
        var shader = resources.Shaders.GetOrCreate(Shaders.ColoredGeometry);
        
        _pipeline = pipelineManager.GetOrCreate(b => b
            .WithName("HelloQuadPipeline")
            .WithShader(shader)  // VertexInputDescription comes from shader!
            .WithRenderPass(swapChain.GetPass(RenderPasses.Main))
            .WithTopology(PrimitiveTopology.TriangleFan)
            .WithCullMode(CullModeFlags.None)
            .EnableDepthTest()
            .EnableDepthWrite()
        );
    }
}
```

**Constructor parameters:** 3 (SAME!)
**Benefits:**
- ✅ More readable
- ✅ No manual VertexInputDescription
- ✅ Cleaner render pass access
- ✅ Fluent API

### Builder Pattern - Option 2 (Inject Builder)
```csharp
public class HelloQuad(
    IPipelineManager pipelineManager,
    ISwapChain swapChain,
    IResourceManager resources,
    PipelineBuilder builder)  // ← NEW parameter
{
    void CreatePipeline()
    {
        var shader = resources.Shaders.GetOrCreate(Shaders.ColoredGeometry);
        
        var descriptor = builder
            .WithName("HelloQuadPipeline")
            .WithShader(shader)
            .WithRenderPass(swapChain.GetPass(RenderPasses.Main))
            .WithTopology(PrimitiveTopology.TriangleFan)
            .WithCullMode(CullModeFlags.None)
            .EnableDepthTest()
            .EnableDepthWrite()
            .Build();  // Returns descriptor
            
        _pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
    }
}
```

**Constructor parameters:** 4 (ONE MORE for every component!)
**Issues:**
- ❌ Extra DI dependency
- ❌ Two-step process (build descriptor, get pipeline)
- ❌ More verbose

---

## Testability Analysis

### Option 1: Lambda Pattern Testing

**Component Test:**
```csharp
[Fact]
public void HelloQuad_CreatesPipeline_WithCorrectConfiguration()
{
    // Arrange
    var mockPipelineManager = new Mock<IPipelineManager>();
    var mockSwapChain = Mock.Of<ISwapChain>();
    var mockResources = Mock.Of<IResourceManager>();
    
    PipelineDescriptor? capturedDescriptor = null;
    mockPipelineManager
        .Setup(m => m.GetOrCreate(It.IsAny<Func<PipelineBuilder, PipelineBuilder>>()))
        .Callback<Func<PipelineBuilder, PipelineBuilder>>(configure =>
        {
            var builder = new PipelineBuilder();
            builder = configure(builder);
            capturedDescriptor = builder.Build();
        })
        .Returns(new Pipeline());
    
    var component = new HelloQuad(
        mockPipelineManager.Object,
        mockSwapChain,
        mockResources);
    
    // Act
    component.Activate();
    
    // Assert
    Assert.NotNull(capturedDescriptor);
    Assert.Equal("HelloQuadPipeline", capturedDescriptor.Name);
    Assert.Equal(PrimitiveTopology.TriangleFan, capturedDescriptor.Topology);
}
```

**Builder Unit Test:**
```csharp
[Fact]
public void PipelineBuilder_Build_ThrowsIfShaderMissing()
{
    // Arrange
    var builder = new PipelineBuilder()
        .WithRenderPass(mockRenderPass);
    
    // Act & Assert
    Assert.Throws<InvalidOperationException>(() => builder.Build());
}
```

Both are testable!

### Option 2: Inject Builder Testing

**Component Test:**
```csharp
[Fact]
public void HelloQuad_CreatesPipeline_WithCorrectConfiguration()
{
    // Arrange
    var mockPipelineManager = new Mock<IPipelineManager>();
    var mockSwapChain = Mock.Of<ISwapChain>();
    var mockResources = Mock.Of<IResourceManager>();
    var builder = new PipelineBuilder();  // ← Need to create builder
    
    var component = new HelloQuad(
        mockPipelineManager.Object,
        mockSwapChain,
        mockResources,
        builder);  // ← Extra parameter
    
    // Act
    component.Activate();
    
    // Assert
    mockPipelineManager.Verify(m => 
        m.GetOrCreatePipeline(It.Is<PipelineDescriptor>(d => 
            d.Topology == PrimitiveTopology.TriangleFan)));
}
```

More complex test setup with extra parameter.

---

## Performance Considerations

### Option 1: Lambda
```csharp
_pipeline = pipelineManager.GetOrCreate(b => b
    .WithShader(shader)
    .WithRenderPass(renderPass)
);
```

**Allocation:**
- 1 lambda allocation (captured in delegate)
- 1 PipelineBuilder instance
- Total: ~96 bytes (64-byte builder + 32-byte delegate)

**Frequency:** Once per pipeline (cached after first creation)

**Impact:** Negligible - pipelines are created during initialization, not per-frame

### Option 2: Inject Builder
```csharp
var descriptor = builder
    .WithShader(shader)
    .Build();
_pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
```

**Allocation:**
- 1 PipelineBuilder instance (from DI container)
- DI container overhead for transient resolution
- Total: ~100-150 bytes (includes DI tracking)

**Frequency:** Once per pipeline

**Impact:** Negligible, but slightly higher due to DI overhead

### Winner: Option 1 (marginally better, but both are fine)

---

## Migration Consideration

### Current Code Impact

**With Option 1 (Lambda):**
- **Change**: Replace `new PipelineDescriptor { }` with `GetOrCreate(b => b.WithXxx())`
- **Constructor**: No change
- **DI Registration**: No change
- **Components to update**: ~10-20 (wherever pipelines are created)

**With Option 2 (Inject Builder):**
- **Change**: Add `PipelineBuilder` to every component constructor
- **Constructor**: Change every component that creates pipelines
- **DI Registration**: Add `services.AddTransient<PipelineBuilder>()`
- **Components to update**: ~10-20 + ALL constructors + DI setup

**Winner: Option 1** - less code to change

---

## Recommendation: Option 1 (Lambda Pattern)

### Summary

**Choose Lambda Pattern because:**

1. ✅ **Zero DI Impact** - no new dependencies in any component
2. ✅ **Clean Constructors** - no extra parameters to track
3. ✅ **Manager Encapsulation** - builder is implementation detail
4. ✅ **Fluent API** - best developer experience
5. ✅ **Simplest Migration** - change pipeline creation, not constructors
6. ✅ **Future-Proof** - if builder needs services, manager provides them
7. ✅ **Testable** - can still unit test both component and builder
8. ✅ **Follows .NET Patterns** - similar to LINQ, EF, Options, etc.

### API Design

```csharp
public interface IPipelineManager : IDisposable
{
    // New builder method
    Pipeline GetOrCreate(Func<PipelineBuilder, PipelineBuilder> configure);
    
    // Keep existing for backward compatibility
    Pipeline GetOrCreatePipeline(PipelineDescriptor descriptor);
}

public class PipelineBuilder
{
    // Pure configuration, no services needed in constructor
    public PipelineBuilder WithShader(ShaderResource shader) { ... }
    public PipelineBuilder WithRenderPass(RenderPass renderPass) { ... }
    // ... fluent methods
    
    // Internal - only manager calls this
    internal PipelineDescriptor Build() { ... }
}
```

### Usage

```csharp
// Component constructor - no change!
public class MyComponent(
    IPipelineManager pipelineManager,
    IResourceManager resources)
{
    void CreatePipeline()
    {
        var shader = resources.Shaders.GetOrCreate(MyShader);
        
        // Clean, fluent API
        _pipeline = pipelineManager.GetOrCreate(b => b
            .WithShader(shader)
            .WithRenderPass(renderPass)
            .EnableDepthTest()
        );
    }
}
```

### DI Registration - No Change!

```csharp
services
    .AddSingleton<IPipelineManager, PipelineManager>()
    .AddSingleton<IResourceManager, ResourceManager>()
    // ... existing services
    // No PipelineBuilder registration needed!
```

---

## Answer to Original Questions

### Q: "What needs to be injected so we can build the pipeline?"

**A:** Nothing new! Components already inject `IPipelineManager`, which is all they need.

### Q: "Is it going to be too onerous to keep track of the dependencies?"

**A:** No, because there are no new dependencies to track. The builder is created internally by the manager.

### Q: "Or, do we inject the PipelineBuilder itself?"

**A:** No, that would add a dependency to every component. Better to let the manager create it.

### Q: "Or, IPipelineManager.GetBuilder()?"

**A:** Possible, but less fluent than the lambda approach. Lambda is cleaner and more idiomatic.

---

## Conclusion

**Use the Lambda Pattern (Option 1).** It provides the best developer experience with zero DI impact. The builder is an implementation detail that components don't need to know about directly.

```csharp
// Perfect balance of fluency and simplicity
_pipeline = pipelineManager.GetOrCreate(b => b
    .WithShader(shader)
    .WithRenderPass(renderPass)
    .WithTopology(PrimitiveTopology.TriangleFan)
    .EnableDepthTest()
);
```

No new dependencies. No constructor changes. Clean and readable. ✅
