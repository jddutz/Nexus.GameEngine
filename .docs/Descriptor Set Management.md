# Descriptor Set Management Guide

## Overview

Descriptor sets are Vulkan's mechanism for binding resources (uniform buffers, textures, samplers) to shaders. The engine provides `IDescriptorManager` to handle the complexity of descriptor pool management, layout creation, and set allocation.

## Architecture

### IDescriptorManager

Manages three key Vulkan objects:

1. **Descriptor Pools**: Pre-allocated pools of descriptor sets
2. **Descriptor Set Layouts**: "Blueprints" defining what resources can be bound
3. **Descriptor Sets**: Actual resource bindings used in draw commands

### IBufferManager

Provides buffer creation including:
- **Vertex Buffers**: Geometry data (positions, colors, UVs)
- **Uniform Buffers (UBOs)**: Shader parameters updated per-frame or per-object

## Usage Pattern

### Step 1: Define Descriptor Layout in Shader

```csharp
public class MyShader : IShaderDefinition
{
    public DescriptorSetLayoutBinding[]? DescriptorSetLayoutBindings =>
    [
        new DescriptorSetLayoutBinding
        {
            Binding = 0,                              // matches "layout(binding = 0)" in GLSL
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit,  // accessible in fragment shader
            PImmutableSamplers = null
        }
    ];
}
```

### Step 2: Create Descriptor Set Layout

The pipeline builder automatically creates descriptor set layouts from shader definitions:

```csharp
// In component OnActivate()
_pipeline = pipelineManager.GetBuilder()
    .WithShader(new MyShader())  // Descriptor layout created automatically
    .WithRenderPasses(RenderPasses.Main)
    .Build("MyPipeline");
```

Alternatively, create layouts explicitly for manual descriptor set management:

```csharp
var shader = new MyShader();
var layout = descriptorManager.CreateDescriptorSetLayout(
    shader.DescriptorSetLayoutBindings!);
```

### Step 3: Create Uniform Buffer

```csharp
// Serialize your data structure
var data = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref myStruct, 1));

// Create buffer with appropriate size
var (buffer, memory) = bufferManager.CreateUniformBuffer((ulong)data.Length);

// Upload initial data
bufferManager.UpdateUniformBuffer(memory, data);
```

### Step 4: Allocate and Update Descriptor Set

```csharp
// Allocate descriptor set from pool
var descriptorSet = descriptorManager.AllocateDescriptorSet(layout);

// Bind the uniform buffer to the descriptor set
descriptorManager.UpdateDescriptorSet(
    descriptorSet, 
    buffer, 
    (ulong)data.Length,
    binding: 0);  // Must match shader binding point
```

### Step 5: Include in Draw Command

```csharp
public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
{
    yield return new DrawCommand
    {
        RenderMask = RenderMask,
        Pipeline = _pipeline,
        VertexBuffer = _geometry.Buffer,
        VertexCount = _geometry.VertexCount,
        InstanceCount = 1,
        DescriptorSet = _descriptorSet,  // Renderer binds automatically
        PushConstants = pushData
    };
}
```

## Complete Example: Gradient Background

```csharp
public partial class GradientBackground : RenderableBase, IRenderable
{
    private readonly IBufferManager _bufferManager;
    private readonly IDescriptorManager _descriptorManager;
    
    private Buffer _uboBuffer;
    private DeviceMemory _uboMemory;
    private DescriptorSet _descriptorSet;
    private GeometryResource? _geometry;
    private PipelineHandle _pipeline;
    
    public GradientBackground(
        IPipelineManager pipelineManager,
        IResourceManager resources,
        IBufferManager bufferManager,
        IDescriptorManager descriptorManager)
    {
        _bufferManager = bufferManager;
        _descriptorManager = descriptorManager;
    }
    
    protected override void OnActivate()
    {
        // 1. Create pipeline with gradient shader
        var shader = new LinearGradientShader();
        _pipeline = pipelineManager.GetBuilder()
            .WithShader(shader)
            .WithRenderPasses(RenderPasses.Main)
            .WithTopology(PrimitiveTopology.TriangleStrip)
            .Build("GradientPipeline");
        
        // 2. Create descriptor set layout
        var layout = _descriptorManager.CreateDescriptorSetLayout(
            shader.DescriptorSetLayoutBindings!);
        
        // 3. Serialize gradient definition
        var gradientDef = new GradientDefinition
        {
            StopCount = 3,
            Stops = new[]
            {
                new GradientStop { Position = 0.0f, Color = new Vector4D<float>(1, 0, 0, 1) },
                new GradientStop { Position = 0.5f, Color = new Vector4D<float>(0, 1, 0, 1) },
                new GradientStop { Position = 1.0f, Color = new Vector4D<float>(0, 0, 1, 1) }
            }
        };
        var data = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref gradientDef, 1));
        
        // 4. Create and populate uniform buffer
        (_uboBuffer, _uboMemory) = _bufferManager.CreateUniformBuffer((ulong)data.Length);
        _bufferManager.UpdateUniformBuffer(_uboMemory, data);
        
        // 5. Allocate and update descriptor set
        _descriptorSet = _descriptorManager.AllocateDescriptorSet(layout);
        _descriptorManager.UpdateDescriptorSet(_descriptorSet, _uboBuffer, (ulong)data.Length);
        
        // 6. Create geometry
        _geometry = resources.Geometry.GetOrCreate(new UniformColorQuad());
    }
    
    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        var pushConstants = new LinearGradientPushConstants { Angle = _currentAngle };
        
        yield return new DrawCommand
        {
            RenderMask = RenderMask,
            Pipeline = _pipeline,
            VertexBuffer = _geometry!.Buffer,
            VertexCount = _geometry.VertexCount,
            InstanceCount = 1,
            DescriptorSet = _descriptorSet,
            PushConstants = pushConstants
        };
    }
    
    protected override void OnDeactivate()
    {
        // Cleanup
        resources.Geometry.Release(new UniformColorQuad());
        _bufferManager.DestroyBuffer(_uboBuffer, _uboMemory);
        // Descriptor sets freed automatically when pool is reset
    }
}
```

## GLSL Shader Example

```glsl
// Fragment shader
#version 450

// Descriptor set binding - matches C# DescriptorSetLayoutBinding
layout(binding = 0) uniform GradientData {
    int stopCount;
    vec4 colors[8];
    float positions[8];
} gradient;

// Push constants for animation
layout(push_constant) uniform PushConstants {
    float angle;
} pc;

layout(location = 0) in vec2 fragTexCoord;
layout(location = 0) out vec4 outColor;

void main() {
    // Sample gradient based on texture coordinate and angle
    float t = dot(fragTexCoord, vec2(cos(pc.angle), sin(pc.angle)));
    
    // Find gradient stops
    for (int i = 0; i < gradient.stopCount - 1; i++) {
        if (t >= gradient.positions[i] && t <= gradient.positions[i + 1]) {
            float localT = (t - gradient.positions[i]) / 
                          (gradient.positions[i + 1] - gradient.positions[i]);
            outColor = mix(gradient.colors[i], gradient.colors[i + 1], localT);
            return;
        }
    }
}
```

## Best Practices

### Memory Management

1. **Create UBOs once** in `OnActivate()`, update as needed
2. **Destroy buffers** in `OnDeactivate()` 
3. **Descriptor sets** are freed automatically when pools are reset (don't manually free)

### Performance

1. **Cache descriptor set layouts** - DescriptorManager does this automatically
2. **Batch updates** - Update UBO once per frame, not per draw
3. **Minimize descriptor set changes** - Renderer sorts by descriptor set to reduce binding overhead

### Updating UBO Data

```csharp
// Per-frame or on-property-change updates
protected override void OnUpdate(double deltaTime)
{
    if (_needsUpdate)
    {
        var data = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _gradientDef, 1));
        _bufferManager.UpdateUniformBuffer(_uboMemory, data);
        _needsUpdate = false;
    }
}
```

### Multiple Descriptor Sets

For complex shaders with multiple resources:

```csharp
// Shader definition
public DescriptorSetLayoutBinding[]? DescriptorSetLayoutBindings =>
[
    new() { Binding = 0, DescriptorType = DescriptorType.UniformBuffer, ... },
    new() { Binding = 1, DescriptorType = DescriptorType.CombinedImageSampler, ... }
];

// Update multiple bindings
descriptorManager.UpdateDescriptorSet(descriptorSet, uboBuffer, uboSize, binding: 0);
// TODO: Add texture/sampler update methods when texture support is implemented
```

## Troubleshooting

### Descriptor Pool Exhausted

If you see "descriptor pool exhausted" errors:
- DescriptorManager automatically creates new pools
- Check logs for pool creation messages
- Consider increasing `DescriptorsPerPool` in DescriptorManager if needed

### Binding Mismatch

If shader doesn't receive data:
- Verify `binding` parameter matches shader `layout(binding = N)`
- Check descriptor type matches shader declaration (UniformBuffer, Sampler, etc.)
- Ensure StageFlags includes correct shader stages

### Validation Errors

Common validation errors:
- "Descriptor set not bound" - Ensure DescriptorSet is set in DrawCommand
- "Buffer out of bounds" - UBO size must match shader uniform block size
- "Layout incompatible" - Pipeline and descriptor set layouts must match

## See Also

- `src/GameEngine/Graphics/Descriptors/IDescriptorManager.cs` - Full API documentation
- `src/GameEngine/Graphics/Buffers/IBufferManager.cs` - Buffer management API
- `src/GameEngine/Resources/Shaders/Definitions/LinearGradientShader.cs` - Example shader with UBO
- `.docs/Vulkan Architecture.md` - Complete rendering system overview
