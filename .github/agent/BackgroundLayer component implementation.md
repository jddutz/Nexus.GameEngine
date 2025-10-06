# BackgroundLayer Component Implementation Analysis

## Current State

The `BackgroundLayer` component exists but currently returns an empty enumerable in its `OnRender` method. The component is designed to render a solid color background but isn't actually drawing anything.

## Architecture Overview

The engine uses a **component-based rendering system** where:

1. Components implement `IRenderable` interface
2. `OnRender()` returns `IEnumerable<RenderData>` - declarative rendering requirements
3. The `Renderer` collects these render states and applies them in batches
4. No direct OpenGL calls in components - all through `RenderData` declarations

## Required Components for Simple Full-Screen Quad

To render a simple full-screen quad with solid color, we need:

### 1. Geometry Resource (Full-Screen Quad)

- **Vertex Array Object (VAO)** containing:
  - **Vertex Buffer Object (VBO)** with quad vertices
  - **Element Buffer Object (EBO)** with quad indices (optional for simple quad)
  - **Vertex attributes** setup (position + texture coordinates)

**Quad geometry** (NDC coordinates):

```
Vertices: [-1,-1,0, 1,-1,0, 1,1,0, -1,1,0]  // Positions
TexCoords: [0,0, 1,0, 1,1, 0,1]              // UV coordinates
Indices: [0,1,2, 2,3,0]                       // Triangle indices
```

### 2. Shader Resource (Background Solid)

- **Vertex Shader**: Transforms quad vertices, passes through texture coordinates
- **Fragment Shader**: Outputs solid color from uniform
- **Compiled Shader Program**: Linked vertex + fragment shaders

**Key uniforms needed**:

- `uBackgroundColor` (vec4): RGBA color value
- `uFade` (float): Optional opacity control

### 3. Render State Declaration

In `OnRender()`, the BackgroundLayer needs to return a `RenderData` with:

- `ShaderProgram`: ID of compiled background shader
- `VertexArray`: ID of full-screen quad VAO
- `Priority`: Low value (background renders first)

## Resource Management System

The engine uses **declarative resource definitions** with attributes:

### Expected Structure (needs to be created):

```csharp
public static class Geometry
{
    [SharedResource("FullScreenQuad", ResourceType.Geometry)]
    public static readonly GeometryDefinition FullScreenQuad = new()
    {
        Vertices = [...],
        Indices = [...],
        Attributes = [...]
    };
}

public static class Shaders
{
    [SharedResource("BackgroundSolid", ResourceType.Shader)]
    public static readonly ShaderDefinition BackgroundSolid = new()
    {
        VertexSource = "...",
        FragmentSource = "..."
    };
}
```

### Resource Access Pattern:

```csharp
public IEnumerable<RenderData> OnRender(RenderContext context)
{
    var quadVAO = resourceManager.GetOrCreateResource<uint>(Geometry.FullScreenQuad);
    var shader = resourceManager.GetOrCreateResource<uint>(Shaders.BackgroundSolid);

    yield return new RenderData
    {
        ShaderProgram = shader,
        VertexArray = quadVAO,
        Priority = 0  // Background priority
    };
}
```

## Implementation Steps Required

1. **Create Resource Definitions**:

   - `Geometry.cs` with `FullScreenQuad` definition
   - `Shaders.cs` with `BackgroundSolid` definition

2. **Implement Resource Definition Classes**:

   - `GeometryDefinition` with VAO creation logic
   - `ShaderDefinition` with shader compilation logic

3. **Complete Resource Manager**:

   - Current `IResourceManager` is mostly empty
   - Need attribute discovery and resource creation

4. **Update BackgroundLayer.OnRender()**:

   - Remove empty return
   - Add resource access and RenderData creation
   - Handle uniform updates (background color)

5. **Shader Uniform Management**:
   - Need way to pass `BackgroundColor` property to shader
   - Either through render state or separate uniform binding system

## Key Files Identified

### Existing Infrastructure:

- `RenderData.cs` - Declarative rendering requirements ✓
- `IRenderable.cs` - Rendering interface ✓
- `Renderer.cs` - Batched rendering system ✓
- `BackgroundLayer.cs` - Component shell ✓

### Missing/Incomplete:

- `GeometryDefinition` class
- `ShaderDefinition` class
- Resource definitions (`Geometry.cs`, `Shaders.cs`)
- Complete `ResourceManager` implementation
- Uniform binding system

## Rendering Pipeline Flow

1. **Component Update**: `BackgroundLayer` properties updated via deferred updates
2. **Render Collection**: `Viewport.OnRender()` calls `BackgroundLayer.OnRender()`
3. **Resource Resolution**: Component requests VAO + Shader from ResourceManager
4. **State Declaration**: Component returns `RenderData` with rendering requirements
5. **Batch Processing**: `Renderer` groups compatible render states
6. **GL State Application**: Renderer applies VAO, shader, uniforms
7. **Draw Call**: Renderer issues draw call for quad geometry

## Next Steps

The component needs significant infrastructure to work properly. We should implement in this order:

1. Resource definition system
2. Basic geometry and shader resources
3. Resource manager completion
4. BackgroundLayer render state implementation
5. Uniform binding for dynamic colors
