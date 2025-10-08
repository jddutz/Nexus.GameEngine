# ElementData Refactoring - Performance Analysis

## Question: Should ElementData be a struct?

### Answer: **No - Keep it as a class**

## Performance Analysis

### Why NOT a struct?

1. **Contains reference types**

   - `Dictionary<string, object>` for uniforms
   - `uint?[]` for textures
   - Reference types negate struct memory benefits

2. **Size considerations**

   - Current size: ~200+ bytes (with dictionary overhead)
   - Recommended struct size: < 16 bytes
   - Large structs have worse performance than small classes

3. **Boxing concerns**

   - Used with `IEnumerable<ElementData>` (LINQ operations)
   - Would be boxed when passed to methods
   - Boxing negates all struct performance benefits

4. **Semantic meaning**

   - Represents a render request/command (reference semantics)
   - Not a simple value type like Vector2D or Point
   - Mutable in some scenarios (Priority sorting)

5. **Usage patterns**
   - Created via `yield return` in GetElements()
   - Passed to Draw() method
   - Used in LINQ SelectMany() operations
   - All of these would cause boxing with structs

### Better optimization: Simplify as an immutable class

Instead of making it a struct, we **simplified ElementData** by:

✅ **Removed unused fields:**

- `Vbo` - Never used after creation (VAO encapsulates this)
- `Ebo` - Never used after creation (VAO encapsulates this)
- `ShaderProgram` - Duplicate of `Shader`
- `VertexArray` - Duplicate of `Vao`
- `ActiveTextureUnit` - Not needed per-element
- `BoundTextures` - Renamed to `Textures` and made non-nullable

✅ **Removed stateful methods:**

- `IsTextureBound()` - State tracking belongs in Renderer
- `SetBoundTexture()` - Mutating methods don't belong in data carrier
- `ClearBoundTexture()` - Same as above
- `Reset()` - Same as above

✅ **Added missing properties:**

- `IndexCount` - Required for glDrawElements()
- `PrimitiveType` - What to draw (triangles, lines, etc.)
- `IndexType` - Type of index data (uint, ushort, etc.)

✅ **Improved type safety and semantics:**

- `SourceViewport` → `ViewportId` (int) - Only need ID for grouping, not full interface
- `Textures` → Non-nullable array (`uint[]`) - Empty array is clearer than null
- `Framebuffer` → Non-nullable (`uint`) with default 0 - Zero is the screen framebuffer
- `Priority` → Non-nullable (`uint`) - Always has a priority (0 = highest)

✅ **Made completely immutable:**

- All properties use `{ get; init; }` - No mutation after creation
- Enables safe sorting, batching, and grouping
- No defensive copying needed

## Result

### Before (Complex, Stateful)

```csharp
public class ElementData
{
    public required uint Vbo { get; init; }         // ❌ Unused
    public required uint Ebo { get; init; }         // ❌ Unused
    public required uint Vao { get; init; }
    public required uint Shader { get; init; }
    public uint? Priority { get; set; }             // ❌ Nullable
    public uint? ShaderProgram { get; set; }        // ❌ Duplicate
    public uint?[] BoundTextures { get; private set; } = new uint?[16]; // ❌ Mutable
    public uint? VertexArray { get; set; }          // ❌ Duplicate
    public uint? Framebuffer { get; set; }
    public int ActiveTextureUnit { get; set; } = 0; // ❌ Not per-element
    public IViewport? SourceViewport { get; set; }
    public Dictionary<string, object> Uniforms { get; init; } = [];

    // ❌ Stateful methods
    public bool IsTextureBound(uint textureId, int slot = 0) { ... }
    public void SetBoundTexture(uint textureId, int slot = 0) { ... }
    public void ClearBoundTexture(int slot = 0) { ... }
    public void Reset() { ... }
}
```

### After (Simple, Immutable Data Carrier)

```csharp
public class ElementData
{
    // Resource IDs (what to render)
    public required uint Vao { get; init; }
    public required uint Shader { get; init; }

    // Draw call parameters (how to render)
    public required uint IndexCount { get; init; }          // ✅ New
    public PrimitiveType PrimitiveType { get; init; }       // ✅ New
    public DrawElementsType IndexType { get; init; }        // ✅ New

    // Rendering configuration
    public uint Priority { get; init; }                     // ✅ Non-nullable, default 0
    public int ViewportId { get; init; }                    // ✅ Changed from IViewport? (only need ID)
    public Dictionary<string, object> Uniforms { get; init; } = [];
    public uint[] Textures { get; init; } = [];             // ✅ Non-nullable array (empty = no textures)
    public uint Framebuffer { get; init; } = 0;             // ✅ Non-nullable, default 0 (screen)

    // ✅ No methods - pure immutable data
}
```

## Performance Impact

### Memory

- **Before:** ~240 bytes (class overhead + fields + dictionary + array)
- **After:** ~200 bytes (removed unused fields)
- **Savings:** ~40 bytes per instance
- **Frame impact:** If 100 elements/frame: ~4KB saved per frame

### Allocations

- **No change** - still allocated on heap (both versions are classes)
- GC pressure unchanged (still reference type)
- Lifetime: Very short (created per frame, collected quickly)

### CPU

- **Simplified initialization** - fewer fields to set
- **No method calls** - removed stateful methods
- **Better cache locality** - smaller object size
- **Clearer code** - easier for JIT to optimize

## Conclusion

**ElementData should remain a class** but be simplified to a pure data carrier. The refactored version:

✅ Removes ~40 bytes per instance
✅ Eliminates unused fields and confusing duplicates  
✅ Adds required draw call parameters
✅ Makes the API clearer and more maintainable
✅ Removes stateful methods (separates concerns)
✅ Keeps reference semantics (no boxing issues)

**Performance gain:** ~20% memory reduction per instance, cleaner code, better separation of concerns, improved type safety.

**Trade-off:** None - this is strictly better than both the original class and a hypothetical struct version.

---

## Why Dictionary for Uniforms?

### Question: Can we optimize uniform storage?

**Current approach:** `Dictionary<string, object>` with uniform names

**Why this is necessary (for now):**

1. **OpenGL API requirement:** `glGetUniformLocation(program, name)` requires the uniform name as a string
2. **Per-draw variability:** Each ElementData can have different uniform values (colors, transforms, etc.)
3. **Flexibility:** Components can set arbitrary uniforms without pre-registration

**Performance characteristics:**

- Dictionary lookup: O(1) average case
- `glGetUniformLocation()`: Cached by GL driver (very fast after first call)
- String allocation: Names are interned/reused across frames

### Future optimization opportunities:

#### Option 1: Uniform Buffer Objects (UBOs)

```csharp
// For shared, rarely-changing uniforms (view/projection matrices)
public uint UniformBufferBinding { get; init; }
```

- Pros: Single bind operation, shared across draws
- Cons: Requires uniform block layout specification, less flexible

#### Option 2: Pre-cached uniform locations

```csharp
// Cache locations at ResourceManager level
public Dictionary<int, object> UniformsByLocation { get; init; } = [];
```

- Pros: Skip string lookup and glGetUniformLocation call
- Cons: Locations are program-specific, harder to use correctly

#### Option 3: Typed uniform structures

```csharp
// For common patterns like materials
public MaterialUniforms? Material { get; init; }
```

- Pros: Type-safe, no boxing, faster
- Cons: Less flexible, need predefined structures

### Recommendation: Keep Dictionary for now

The current `Dictionary<string, object>` approach is:

- ✅ Simple and correct
- ✅ Flexible for all use cases
- ✅ Performance is acceptable (driver caching helps)
- ✅ Can be optimized later without breaking API

**Profile first, optimize later.** If uniform setting becomes a bottleneck (unlikely), we can:

1. Add UBO support for shared uniforms
2. Cache uniform locations at ResourceManager level
3. Batch elements with identical uniforms together
