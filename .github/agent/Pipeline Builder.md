# Pipeline Builder Pattern - Design

## Context
Pipelines need flexible configuration for different rendering scenarios. Builder pattern provides fluent, readable API for pipeline creation.

## Design Decision

**Use Builder Pattern for Pipeline Configuration**

**Benefits:**
- ✅ Flexible - only specify options you need
- ✅ Readable - fluent method chaining
- ✅ Extensible - easy to add new configuration options
- ✅ Immutable - pipelines can't change after build
- ✅ Cacheable - PipelineManager can cache by configuration

---

## Pipeline Builder API

```csharp
public class PipelineBuilder
{
    // Required
    public PipelineBuilder WithShader(ShaderResource shader);
    public PipelineBuilder WithRenderPass(RenderPass renderPass);
    
    // Optional with defaults
    public PipelineBuilder WithTopology(PrimitiveTopology topology);
    public PipelineBuilder WithCullMode(CullModeFlags cullMode);
    public PipelineBuilder WithFrontFace(FrontFace frontFace);
    public PipelineBuilder EnableDepthTest(bool enable = true);
    public PipelineBuilder EnableDepthWrite(bool enable = true);
    public PipelineBuilder EnableBlending(bool enable = true);
    public PipelineBuilder WithSubpass(uint subpass);
    
    // Build
    public Pipeline Build();
}
```

---

## Usage Examples

**Simple Pipeline:**
```csharp
var pipeline = new PipelineBuilder()
    .WithShader(shader)
    .WithRenderPass(renderPass)
    .Build();  // Uses all defaults
```

**Configured Pipeline:**
```csharp
var pipeline = new PipelineBuilder()
    .WithShader(shader)
    .WithRenderPass(renderPass)
    .WithTopology(PrimitiveTopology.TriangleFan)
    .WithCullMode(CullModeFlags.None)
    .EnableDepthTest()
    .EnableDepthWrite()
    .Build();
```

**With PipelineManager Caching:**
```csharp
var pipeline = pipelineManager.GetOrCreate(builder => builder
    .WithShader(shader)
    .WithRenderPass(renderPass)
    .WithTopology(PrimitiveTopology.TriangleFan)
);
```

---

## Pipeline Structure

**Immutable Pipeline after Build:**
```csharp
public class Pipeline
{
    internal Handle VulkanPipeline { get; }
    internal IShaderDefinition ShaderDefinition { get; }
    
    // Private constructor - only builder can create
    internal Pipeline(PipelineBuilder builder) 
    {
        ShaderDefinition = builder.ShaderDefinition;
        VulkanPipeline = CreateVulkanPipeline(builder);
    }
    
    // Used by renderer for validation
    internal void ValidateGeometry(GeometryResource geometry)
    {
        ShaderDefinition.ValidateGeometry(geometry);
    }
}
```

---

## Integration with Shader Resources

**Shader provides VertexInputDescription:**
```csharp
// ShaderResource already loaded from ShaderSubManager
var shader = resources.Shaders.GetOrCreate(Shaders.ColoredGeometry);

// Builder uses shader's VertexInputDescription internally
var pipeline = new PipelineBuilder()
    .WithShader(shader)  // Shader has VertexInputDescription
    .WithRenderPass(renderPass)
    .Build();

// Pipeline stores ShaderDefinition for validation
pipeline.ValidateGeometry(geometry);  // Called by renderer
```

---

## Validation Flow

**At Draw Time (Debug Builds):**
```csharp
// In Renderer.Draw(commandBuffer, drawCommand)
#if DEBUG
drawCommand.Pipeline.ValidateGeometry(drawCommand.Geometry);
#endif

context.VulkanApi.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, drawCommand.Pipeline);
context.VulkanApi.CmdBindVertexBuffers(commandBuffer, 0, 1, drawCommand.VertexBuffer, ...);
context.VulkanApi.CmdDraw(...);
```

**Validation in ShaderDefinition:**
```csharp
public void ValidateGeometry(GeometryResource geometry)
{
    var expectedStride = InputDescription.Bindings[0].Stride;
    if (geometry.Stride != expectedStride)
    {
        throw new InvalidOperationException(
            $"Geometry '{geometry.Name}' stride ({geometry.Stride} bytes) " +
            $"doesn't match shader '{Name}' expected stride ({expectedStride} bytes)"
        );
    }
}
```

---

## Migration Path

**Current HelloQuad:**
```csharp
var descriptor = new PipelineDescriptor {
    Name = "HelloQuadPipeline",
    VertexShaderPath = "Shaders/vert.spv",
    FragmentShaderPath = "Shaders/frag.spv",
    VertexInputDescription = GetVertexInputDescription(),
    Topology = PrimitiveTopology.TriangleFan,
    RenderPass = swapChain.Passes[0],
    EnableDepthTest = true,
    EnableDepthWrite = true,
    CullMode = CullModeFlags.None
};
_pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
```

**With Builder (future):**
```csharp
var shader = resources.Shaders.GetOrCreate(Shaders.ColoredGeometry);

_pipeline = pipelineManager.GetOrCreate(builder => builder
    .WithShader(shader)  // Shader has VertexInputDescription
    .WithRenderPass(swapChain.Passes[0])
    .WithTopology(PrimitiveTopology.TriangleFan)
    .WithCullMode(CullModeFlags.None)
    .EnableDepthTest()
    .EnableDepthWrite()
);
```

**Key Differences:**
- ❌ No manual VertexInputDescription - comes from shader
- ❌ No shader path strings - shader resource loaded by ShaderSubManager
- ✅ Fluent, readable API
- ✅ Type-safe configuration

---

## Implementation Notes

**PipelineBuilder State:**
- Validates required fields (shader, renderPass) in Build()
- Throws if required fields missing
- Applies defaults for optional fields

**PipelineManager Integration:**
- Caches by builder configuration hash
- Returns existing pipeline if configuration matches
- Creates new Vulkan pipeline if configuration new

**Thread Safety:**
- Builder is NOT thread-safe (not needed - built once)
- Pipeline is immutable (thread-safe after build)
- PipelineManager cache is thread-safe

---

## Status

**Decision:** ✅ Approved - Use builder pattern for pipelines

**Dependencies:**
- Requires ShaderResource with VertexInputDescription
- Requires ShaderSubManager implementation
- Can coexist with current PipelineDescriptor during migration

**Next Steps:**
1. Keep current PipelineDescriptor approach in HelloQuad for now
2. Focus on Geometry Resource System first
3. Implement PipelineBuilder as Phase 2 simplification