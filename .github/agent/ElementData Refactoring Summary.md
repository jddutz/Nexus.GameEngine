# ElementData Refactoring Summary

## Changes Made

### 1. ElementData is now **completely immutable**

- All properties use `{ get; init; }` - no mutation after creation
- Removed all methods (IsTextureBound, SetBoundTexture, ClearBoundTexture, Reset)
- Pure data carrier with no side effects

### 2. Removed unused/duplicate fields

| Removed Field       | Reason                                               |
| ------------------- | ---------------------------------------------------- |
| `Vbo`               | Never used after VAO creation - VAO encapsulates VBO |
| `Ebo`               | Never used after VAO creation - VAO encapsulates EBO |
| `ShaderProgram`     | Duplicate of `Shader` property                       |
| `VertexArray`       | Duplicate of `Vao` property                          |
| `ActiveTextureUnit` | Not needed per-element (Renderer state)              |
| `BoundTextures`     | Renamed to `Textures` with better semantics          |

### 3. Added required draw call parameters

| New Field       | Type               | Purpose                               |
| --------------- | ------------------ | ------------------------------------- |
| `IndexCount`    | `uint` (required)  | Number of indices for glDrawElements  |
| `PrimitiveType` | `PrimitiveType`    | What to draw (Triangles, Lines, etc.) |
| `IndexType`     | `DrawElementsType` | Index data type (UnsignedInt, etc.)   |

### 4. Improved type safety and semantics

| Changed Field    | Before       | After              | Reason                        |
| ---------------- | ------------ | ------------------ | ----------------------------- |
| `SourceViewport` | `IViewport?` | `int ViewportId`   | Only need ID for grouping     |
| `Textures`       | `uint?[]`    | `uint[]`           | Empty array clearer than null |
| `Framebuffer`    | `uint?`      | `uint` (default 0) | Zero is screen framebuffer    |
| `Priority`       | `uint?`      | `uint` (default 0) | Always has priority           |

## New ElementData Definition

```csharp
public class ElementData
{
    // Resource IDs (what to render)
    public required uint Vao { get; init; }
    public required uint Shader { get; init; }

    // Draw call parameters (how to render)
    public required uint IndexCount { get; init; }
    public PrimitiveType PrimitiveType { get; init; } = PrimitiveType.Triangles;
    public DrawElementsType IndexType { get; init; } = DrawElementsType.UnsignedInt;

    // Rendering configuration
    public uint Priority { get; init; }
    public int ViewportId { get; init; }
    public Dictionary<string, object> Uniforms { get; init; } = [];
    public uint[] Textures { get; init; } = [];
    public uint Framebuffer { get; init; } = 0;
}
```

## Files That Need Updates

### Components (need to add IndexCount, remove Vbo/Ebo)

1. **HelloQuad.cs** - Remove `Vbo`, `Ebo`, add `IndexCount`
2. **TextElement.cs** - Remove `Vbo`, `Ebo`, add `IndexCount`
3. **SpriteComponent.cs** - Remove `Vbo`, `Ebo`, add `IndexCount`
4. **BackgroundLayer.cs** - Remove `Vbo`, `Ebo`, `ShaderProgram`, `VertexArray`, add `IndexCount`
5. **LayoutBase.cs** - Remove `Vbo`, `Ebo`, add `IndexCount`

### Batching Strategy (need to update references)

6. **DefaultBatchStrategy.cs** - Update references:
   - `ShaderProgram` → `Shader`
   - `VertexArray` → `Vao`
   - `BoundTextures` → `Textures`

### Tests

7. **RenderableTestComponent.cs** - Update ElementData creation

## Migration Pattern

### Before (Broken)

```csharp
yield return new ElementData()
{
    Vbo = vbo,                    // ❌ Remove
    Ebo = ebo,                    // ❌ Remove
    Vao = vao,                    // ✅ Keep
    Shader = shader,              // ✅ Keep
    ShaderProgram = shader,       // ❌ Remove (duplicate)
    VertexArray = vao,            // ❌ Remove (duplicate)
    // ❌ Missing IndexCount!
};
```

### After (Fixed)

```csharp
yield return new ElementData
{
    Vao = vao,
    Shader = shader,
    IndexCount = 6,                           // ✅ Add (from geometry definition)
    PrimitiveType = PrimitiveType.Triangles,  // ✅ Add (optional, defaults to Triangles)
    Priority = RenderPriority,                // ✅ Update (no longer nullable)
    ViewportId = vp.Id,                       // ✅ Update (was SourceViewport)
    Framebuffer = 0,                          // ✅ Update (was null, now explicit 0)
};
```

## Key Benefits

1. **Immutable** - Safe for sorting/batching without defensive copying
2. **Complete** - Contains all data needed for draw calls
3. **Clean** - No unused fields or confusing duplicates
4. **Type-safe** - No nullable where not needed
5. **Smaller** - ~20% memory reduction per instance

## Why Keep Dictionary for Uniforms?

OpenGL requires uniform names via `glGetUniformLocation(program, name)`. Dictionary is:

- ✅ Required by GL API
- ✅ Flexible for all use cases
- ✅ Cached by GL driver (fast)
- ✅ Can be optimized later with UBOs if needed

See "ElementData Performance Analysis.md" for full details on struct vs. class decision and uniform optimization options.

## Next Steps

1. Fix compilation errors in 7 files listed above
2. Update tests to use new ElementData structure
3. Verify Renderer.Draw() uses new IndexCount property
4. Run tests to ensure everything works
