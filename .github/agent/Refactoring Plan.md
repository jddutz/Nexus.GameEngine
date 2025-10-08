# Refactoring Plan: Separation of Concerns

## Goal

Enable creation of new IRenderable components with minimal complexity in GetElements() implementation by properly separating concerns between Renderer, ResourceManager, and Renderables.

## Phase 1: Enhance ElementData (Foundation)

### Changes to `ElementData.cs`

**Add missing draw call properties:**

```csharp
public class ElementData
{
    // Resource references (KEEP)
    public required uint Vao { get; init; }
    public required uint Shader { get; init; }

    // NEW: Draw call parameters
    public required uint IndexCount { get; init; }
    public PrimitiveType PrimitiveType { get; init; } = PrimitiveType.Triangles;
    public DrawElementsType IndexType { get; init; } = DrawElementsType.UnsignedInt;

    // KEEP: Uniforms and priority
    public Dictionary<string, object> Uniforms { get; init; } = [];
    public uint? Priority { get; set; }

    // KEEP: Viewport reference
    public IViewport? SourceViewport { get; set; }

    // REMOVE or CLARIFY: These fields are for state tracking, not resource IDs
    // public required uint Vbo { get; init; }        // ❌ Remove - not used
    // public required uint Ebo { get; init; }        // ❌ Remove - not used
    // public uint? ShaderProgram { get; set; }       // ❌ Remove - duplicate of Shader
    // public uint?[] BoundTextures { ... }           // Move to RenderState class?
    // public uint? Framebuffer { get; set; }         // Move to RenderState class?
    // public int ActiveTextureUnit { get; set; }     // Move to RenderState class?
}
```

**Impact:**

- ✓ ElementData becomes pure "render request" data
- ✓ Contains everything needed for a single draw call
- ✓ No redundant fields
- ✓ Renderer can use this directly without hardcoded references

---

## Phase 2: Enhance ResourceManager (Enable Resource Reuse)

### Option A: Return Metadata Wrappers (Recommended)

**Add resource result types:**

```csharp
namespace Nexus.GameEngine.Resources;

public record GeometryResource
{
    public required uint VaoId { get; init; }
    public required uint IndexCount { get; init; }
    public PrimitiveType PrimitiveType { get; init; } = PrimitiveType.Triangles;
    public DrawElementsType IndexType { get; init; } = DrawElementsType.UnsignedInt;
}

public record ShaderResource
{
    public required uint ProgramId { get; init; }
    public IReadOnlyList<string> UniformNames { get; init; } = [];
}
```

**Update IResourceManager:**

```csharp
public interface IResourceManager : IDisposable
{
    // NEW: Type-safe resource getters
    GeometryResource GetOrCreateGeometry(GeometryDefinition definition);
    ShaderResource GetOrCreateShader(ShaderDefinition definition);

    // KEEP: Existing validation and lifecycle methods
    ResourceValidationResult ValidateResource(IResourceDefinition definition);
    bool ReleaseResource(string resourceName);
    ResourceManagerStatistics GetStatistics();
    int PurgeUnusedResources();

    // OPTIONAL: Keep generic method for extensibility
    uint GetOrCreateResource(IResourceDefinition definition, IAssetService? assetService = null);
}
```

**Update ResourceManager implementation:**

```csharp
public GeometryResource GetOrCreateGeometry(GeometryDefinition definition)
{
    var vaoId = GetOrCreateResource(definition);

    return new GeometryResource
    {
        VaoId = vaoId,
        IndexCount = (uint)definition.Indices.Length,
        PrimitiveType = PrimitiveType.Triangles, // Could be in GeometryDefinition
        IndexType = DrawElementsType.UnsignedInt
    };
}

public ShaderResource GetOrCreateShader(ShaderDefinition definition)
{
    var programId = GetOrCreateResource(definition);

    return new ShaderResource
    {
        ProgramId = programId,
        UniformNames = definition.Uniforms.Select(u => u.Name).ToList()
    };
}
```

### Option B: Store Metadata in Cache (Alternative)

Keep returning `uint` but cache metadata separately:

```csharp
private readonly ConcurrentDictionary<string, ResourceMetadata> _metadata;

public record ResourceMetadata
{
    public uint IndexCount { get; init; }
    // ... other metadata
}

public ResourceMetadata? GetResourceMetadata(string resourceName)
{
    _metadata.TryGetValue(resourceName, out var metadata);
    return metadata;
}
```

**Recommendation:** Use Option A - cleaner API, type-safe, no extra lookups.

---

## Phase 3: Refactor HelloQuad (Make Declarative)

### Changes to `HelloQuad.cs`

**Add IResourceManager dependency:**

```csharp
public partial class HelloQuad : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager;

    // Injected via ComponentFactory
    public HelloQuad(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    // ... existing component properties ...
}
```

**Simplify GetElements():**

```csharp
public IEnumerable<ElementData> GetElements(GL gl, IViewport vp)
{
    if (!IsVisible)
        yield break;

    // Get or create resources (cached automatically)
    var geometry = _resourceManager.GetOrCreateGeometry(GeometryDefinitions.BasicQuad);
    var shader = _resourceManager.GetOrCreateShader(ShaderDefinitions.BasicQuad);

    // Return declarative render request
    yield return new ElementData
    {
        Vao = geometry.VaoId,
        Shader = shader.ProgramId,
        IndexCount = geometry.IndexCount,
        PrimitiveType = geometry.PrimitiveType,
        IndexType = geometry.IndexType,
        Priority = RenderPriority,
        SourceViewport = vp,
        Uniforms = new Dictionary<string, object>
        {
            ["backgroundColor"] = BackgroundColor
        }
    };
}
```

**Remove GetRenderData():**

```csharp
// ❌ DELETE THIS METHOD - no longer needed
// public unsafe ElementData GetRenderData(GL gl) { ... }
```

**Impact:**

- ✓ ~100 lines of GL code removed from HelloQuad
- ✓ Resources properly cached and managed
- ✓ No resource leaks
- ✓ Easy to test (mock IResourceManager)
- ✓ New renderables can follow same simple pattern

---

## Phase 4: Fix Renderer.Draw() (Use ElementData Properly)

### Changes to `Renderer.cs`

**Update Draw() method:**

```csharp
private unsafe void Draw(ElementData element)
{
    // Bind the geometry and shader
    GL.BindVertexArray(element.Vao);
    GL.UseProgram(element.Shader);

    // Apply uniforms if any
    if (element.Uniforms.Count > 0)
    {
        ApplyUniforms(element.Uniforms);  // Already exists but unused
    }

    // Draw with ElementData parameters (not hardcoded!)
    GL.DrawElements(
        element.PrimitiveType,
        element.IndexCount,
        element.IndexType,
        null);
}
```

**Impact:**

- ✓ No hardcoded geometry references
- ✓ Can render any geometry type
- ✓ Uniforms are applied
- ✓ Extensible to new render types

---

## Phase 5: Update IRenderable (Optional - Remove GL Parameter)

### Changes to `IRenderable.cs`

**Current signature:**

```csharp
IEnumerable<ElementData> GetElements(GL gl, IViewport vp);
```

**Proposed signature:**

```csharp
IEnumerable<ElementData> GetElements(IViewport vp);
```

**Rationale:**

- Components shouldn't need direct GL access for declarative rendering
- IResourceManager handles all GL resource creation
- Enforces separation of concerns at interface level
- Easier to test (no GL context needed in mocks)

**Impact:**

- Requires updating all IRenderable implementations
- Renderer.RenderViewport() passes one less parameter
- Stronger architectural boundaries

**Decision:** Defer to later if needed - not blocking for minimal GetElements() complexity.

---

## Implementation Order

1. **Step 1:** Update ElementData

   - Add IndexCount, PrimitiveType, IndexType
   - Remove Vbo, Ebo, duplicate ShaderProgram fields
   - Run tests, fix compilation errors

2. **Step 2:** Add GeometryResource and ShaderResource types

   - Create new record types
   - No breaking changes yet

3. **Step 3:** Add new methods to IResourceManager

   - Add GetOrCreateGeometry() and GetOrCreateShader()
   - Implement in ResourceManager
   - Keep existing GetOrCreateResource() for backward compatibility

4. **Step 4:** Update Renderer.Draw()

   - Use element.IndexCount instead of hardcoded value
   - Add ApplyUniforms() call
   - Test with existing HelloQuad (should still work)

5. **Step 5:** Refactor HelloQuad

   - Inject IResourceManager
   - Simplify GetElements()
   - Remove GetRenderData()
   - Update tests

6. **Step 6:** Cleanup
   - Remove unused Renderer methods if batching isn't implemented
   - Document the new pattern for future renderables
   - Create example/template renderable

---

## Testing Strategy

### Unit Tests to Add/Update

1. **ResourceManager Tests:**

   - Test GetOrCreateGeometry() returns correct metadata
   - Test GetOrCreateShader() returns correct metadata
   - Test caching works across multiple calls

2. **HelloQuad Tests:**

   - Test GetElements() with mocked IResourceManager
   - Verify correct resource definitions are requested
   - Verify correct ElementData is returned
   - Test uniform values match component properties

3. **Renderer Tests:**
   - Test Draw() with various ElementData configurations
   - Verify GL calls use ElementData properties
   - Test uniform application

### Integration Tests

1. **TestApp:**
   - Verify HelloQuad still renders correctly
   - Check resource caching (shouldn't create resources every frame)
   - Monitor for GL errors
   - Verify uniforms affect rendering (color changes)

---

## Example: New Renderable After Refactoring

```csharp
public partial class TexturedQuad : RuntimeComponent, IRenderable
{
    private readonly IResourceManager _resourceManager;

    [ComponentProperty]
    private string _texturePath = "default.png";

    public TexturedQuad(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public IEnumerable<ElementData> GetElements(GL gl, IViewport vp)
    {
        // Simple, declarative, no GL code!
        var geometry = _resourceManager.GetOrCreateGeometry(GeometryDefinitions.TexturedQuad);
        var shader = _resourceManager.GetOrCreateShader(ShaderDefinitions.TexturedShader);
        var texture = _resourceManager.GetOrCreateTexture(TextureDefinitions.FromFile(_texturePath));

        yield return new ElementData
        {
            Vao = geometry.VaoId,
            Shader = shader.ProgramId,
            IndexCount = geometry.IndexCount,
            PrimitiveType = geometry.PrimitiveType,
            Priority = RenderPriority,
            Uniforms = new()
            {
                ["diffuseTexture"] = (int)texture.TextureUnit,
                ["modelMatrix"] = Transform.Matrix
            }
        };
    }

    public uint RenderPriority => 100;
    public Box3D<float> BoundingBox => ComputeBoundingBox();
    public uint RenderPassFlags => 1;
    public bool IsVisible => _isVisible;
    public void SetVisible(bool visible) => IsVisible = visible;
}
```

**Lines of code:** ~35 (vs ~150 in current HelloQuad)
**Complexity:** Low - just resource lookups and data declaration
**Testability:** High - mock IResourceManager
**Maintainability:** High - no GL knowledge required

---

## Breaking Changes

### For Component Authors

- Must inject IResourceManager
- Must use GetOrCreateGeometry/Shader methods
- ElementData requires new properties

### For Renderer Users

- None - render API unchanged

### For Resource Definitions

- None - existing definitions work as-is

---

## Future Enhancements (Out of Scope)

1. **Batching Implementation**

   - Use existing Update\* methods in Renderer
   - Sort ElementData by state
   - Minimize state changes

2. **Resource Handle System**

   - Replace raw uint with typed handles
   - Automatic disposal tracking
   - RAII-style resource management

3. **Material System**

   - Group shader + uniforms + textures
   - Reusable material definitions
   - Material instancing

4. **Render Graph**
   - Multi-pass rendering
   - Automatic dependency resolution
   - Framebuffer management

---

## Success Criteria

✅ HelloQuad.GetElements() is < 20 lines
✅ No direct GL resource creation in components
✅ ResourceManager is used for all resources
✅ Renderer.Draw() uses ElementData properties
✅ All existing tests pass
✅ New renderables can follow same simple pattern
✅ No resource leaks
✅ Resource caching works
