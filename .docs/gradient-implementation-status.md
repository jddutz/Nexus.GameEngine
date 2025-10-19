# BackgroundLayer Gradient Implementation Summary

## Implementation Status

### ✅ Completed:
1. **GradientStop** and **GradientDefinition** types with validation
2. **LinearGradientPushConstants** and **RadialGradientPushConstants** structs
3. GLSL shader files: linear_gradient.vert/frag, radial_gradient.vert/frag
4. Compiled shaders to SPIR-V (.spv files)
5. **LinearGradientShader** and **RadialGradientShader** definition classes
6. Updated **BackgroundLayerMode** enum

### 🚧 In Progress:
7. **BackgroundLayer** component update

### ⏳ Remaining:
8. Singleton validation for BackgroundLayer
9. Build and test
10. Example usage

## BackgroundLayer Update Requirements

### New Properties Needed:
```csharp
// Configuration (set from template, non-changeable at runtime)
public BackgroundLayerMode Mode { get; private set; }

// UniformColor mode
[ComponentProperty] private Vector4D<float> _uniformColor;

// PerVertexColor mode  
[ComponentProperty] private Vector4D<float>[] _vertexColors;

// LinearGradient mode
private GradientDefinition? _linearGradientDefinition;
[ComponentProperty] private float _linearGradientAngle;

// RadialGradient mode
private GradientDefinition? _radialGradientDefinition;
[ComponentProperty] private Vector2D<float> _radialGradientCenter;
[ComponentProperty] private float _radialGradientRadius;
```

### UBO Management (TODO - requires descriptor set support):
- Create UBO buffer for gradient definition
- Create descriptor set with UBO binding
- Update UBO when gradient definition changes
- Pass descriptor set to DrawCommand

### Pipeline Selection:
```csharp
_pipeline = Mode switch
{
    BackgroundLayerMode.UniformColor => CreatePipeline(new ColoredGeometryShader()),
    BackgroundLayerMode.PerVertexColor => CreatePipeline(new ColoredGeometryShader()),
    BackgroundLayerMode.LinearGradient => CreatePipeline(new LinearGradientShader()),
    BackgroundLayerMode.RadialGradient => CreatePipeline(new RadialGradientShader()),
    _ => throw new NotSupportedException($"Mode {Mode} not implemented")
};
```

### Draw Command Generation:
```csharp
public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
{
    object? pushConstants = Mode switch
    {
        BackgroundLayerMode.UniformColor => VertexColorsPushConstants.Solid(_uniformColor),
        BackgroundLayerMode.PerVertexColor => VertexColorsPushConstants.FromColors(...),
        BackgroundLayerMode.LinearGradient => new LinearGradientPushConstants { Angle = _linearGradientAngle },
        BackgroundLayerMode.RadialGradient => new RadialGradientPushConstants { Center = _radialGradientCenter, Radius = _radialGradientRadius },
        _ => null
    };
    
    yield return new DrawCommand
    {
        RenderMask = RenderMask,
        Pipeline = _pipeline,
        VertexBuffer = _geometry.Buffer,
        VertexCount = _geometry.VertexCount,
        InstanceCount = 1,
        PushConstants = pushConstants,
        // TODO: Add DescriptorSet for gradient modes
    };
}
```

## Known Issues/TODOs:

1. **Descriptor Set Support**: The engine doesn't yet have a descriptor pool/set management system
   - Need to add `IDescriptorManager` interface
   - Create descriptor pool for UBO bindings
   - Allocate descriptor sets per BackgroundLayer
   - Update descriptor sets to point to UBO buffers

2. **UBO Buffer Management**: Need Vulkan buffer creation for UBOs
   - Create buffer with `VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT`
   - Allocate device memory with `VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT`
   - Map/unmap for updates

3. **IShaderDefinition Extension**: Need to add descriptor set layout info
   - Add `DescriptorSetLayoutBinding[]?` property to interface
   - Update shader definitions to specify UBO binding

4. **Renderer Update**: Need to handle descriptor set binding in draw calls
   - Check if DrawCommand has DescriptorSet
   - Call `vkCmdBindDescriptorSets` before draw

## Temporary Solution:
For now, implement UniformColor and PerVertexColor modes (these work with existing infrastructure).
Mark LinearGradient and RadialGradient as "TODO" until descriptor set support is added.
