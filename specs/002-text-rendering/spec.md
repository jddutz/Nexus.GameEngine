# Feature Specification: Text Rendering with TextElement

**Feature Branch**: `002-text-rendering`  
**Created**: 2025-11-03  
**Status**: Draft  
**Input**: User description: "Rendering text to the screen using TextElement. The current version of TextElement needs to be completely replaced. We will need to include FontAtlas resources and resource management as well as extending our previous Element implementation to allow us to render text."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic "Hello World" Text Display (Priority: P1)

Developers should be able to display the text "Hello World" centered on screen using a default font at default size, with the system handling all font atlas generation, glyph rendering, and GPU resource management automatically. The text will use Element's existing positioning system (Position, AnchorPoint, Size, Scale) for placement and transformation.

**Why this priority**: This is the absolute minimum viable feature. If we can render "Hello World" successfully, we've proven the core text rendering pipeline works end-to-end. Everything else is enhancements built on this foundation.

**Independent Test**: Can be fully tested by creating a TextElement with text "Hello World", Position at screen center, AnchorPoint (0, 0) for center alignment, rendering one frame, and verifying via pixel sampling that the expected glyph shapes appear centered on screen.

**Acceptance Scenarios**:

1. **Given** a TextElement with text "Hello World", Position at screen center (960, 540), AnchorPoint (0, 0), **When** the element activates, **Then** the text renders centered on screen with all 10 characters (including space) visible
2. **Given** a rendered frame with "Hello World", **When** pixel sampling at expected character positions, **Then** sampled pixels show non-background colors (white text on dark background)
3. **Given** a TextElement with empty text "", **When** rendering occurs, **Then** no geometry is generated and no draw commands are issued (optimization)
4. **Given** a TextElement with AnchorPoint (-1, -1), **When** rendering occurs, **Then** text starts at Position and extends right/down (top-left alignment)
5. **Given** a TextElement with Scale (2, 2, 1), **When** rendering occurs, **Then** text renders at 2x size (note: will show bitmap scaling artifacts)

---

### User Story 2 - Font Atlas Resource Management (Priority: P1)

The system should automatically generate a font atlas from the embedded default TrueType font, cache it efficiently, and manage GPU texture resources so that multiple TextElements can share the same font atlas without duplicating memory or generation work.

**Why this priority**: Font atlas generation is expensive. Without proper caching and resource sharing, each TextElement would regenerate the atlas, causing unacceptable performance waste. This is critical infrastructure that must work from day one.

**Independent Test**: Can be tested by creating two TextElements with the default font, monitoring resource manager logs to confirm only one atlas is generated, and verifying both elements render correctly using the shared font resource.

**Acceptance Scenarios**:

1. **Given** two TextElements with default FontDefinition, **When** both activate, **Then** FontResourceManager creates only one FontResource (verified via resource manager logs)
2. **Given** a TextElement using a cached font, **When** it activates, **Then** no atlas generation occurs (reuses existing resource)
3. **Given** a FontResource with no active references, **When** all TextElements deactivate, **Then** the font resource is released and GPU memory is freed
4. **Given** the default embedded TrueType font, **When** FontResourceManager loads it, **Then** the font atlas texture is created successfully with ASCII printable characters

---

### Edge Cases

- **Empty Text**: TextElements with empty strings should not generate geometry or issue draw commands
- **Null Text**: TextElements with null text should behave same as empty text (no rendering, no crash)
- **Whitespace-Only Text**: Text with only spaces should generate geometry for proper spacing but render transparently (no visible pixels)
- **Text Exceeding SizeConstraints**: If text width exceeds SizeConstraints, text extends beyond bounds (no clipping or wrapping for MVP)
- **Scale Effects**: Scaling TextElement (via Scale property) will cause bitmap scaling artifacts - acceptable for MVP, SDF rendering addresses this later
- **Different Anchor Points**: All anchor point values (-1 to 1 range) should work correctly for text positioning (inherited from Element)

## Requirements *(mandatory)*

### Functional Requirements

#### Core Text Rendering (MVP)

- **FR-001**: TextElement MUST render text strings using font atlases generated from embedded TrueType font
- **FR-002**: TextElement MUST use Element's existing positioning system (Position, AnchorPoint, Size, Scale inherited from Element and Transformable)
- **FR-003**: TextElement MUST calculate text geometry Size based on measured text dimensions (sum of glyph advances for width, font metrics for height)
- **FR-004**: TextElement MUST respect AnchorPoint for text alignment: (-1,-1) = top-left, (0,0) = center, (1,1) = bottom-right
- **FR-005**: TextElement MUST support Scale transformation (inherited from Transformable) even though it causes bitmap artifacts
- **FR-006**: TextElement MUST render text with default white color (1, 1, 1, 1)

#### Font Atlas System (MVP)

- **FR-007**: System MUST generate font atlas texture from default embedded TrueType font (Roboto Regular 16pt)
- **FR-008**: System MUST generate shared font atlas geometry containing pre-positioned quads for all ASCII printable characters (32-126)
- **FR-009**: Font atlas geometry MUST include 95 characters × 4 vertices = 380 vertices with position (-1 to 1) and pre-baked UV coordinates
- **FR-010**: System MUST cache font atlas texture AND geometry, reused across all TextElements using same font
- **FR-011**: FontResource MUST include GPU texture atlas, shared geometry resource, glyph metrics dictionary, and font metrics (line height, ascender, descender)
- **FR-012**: GlyphInfo MUST include character index (for FirstVertex calculation), UV coordinates, bearing offsets, advance width, and pixel dimensions

#### Resource Management (MVP)

- **FR-013**: FontResourceManager MUST manage font atlas lifecycle (texture + geometry creation, caching, reference counting, disposal)
- **FR-014**: FontResourceManager MUST release font resources (texture and geometry) when no TextElements reference them
- **FR-015**: System MUST load embedded TrueType font via EmbeddedTrueTypeFontSource
- **FR-016**: Shared font atlas geometry MUST be cached by GeometryResourceManager and reused by all TextElements with same font

#### Geometry Generation (MVP)

- **FR-017**: FontResourceManager MUST generate shared font atlas geometry containing quads for all ASCII printable characters at font creation time
- **FR-018**: Vertex format MUST include position (Vector2D<float>, normalized -1 to 1) and UV coordinates (Vector2D<float>, pre-baked atlas coords) per vertex
- **FR-019**: Each character quad MUST have 4 vertices at known offset (charIndex × 4) in shared geometry buffer
- **FR-020**: TextElement MUST NOT generate per-element geometry - reuses shared font atlas geometry
- **FR-021**: TextElement MUST calculate per-glyph WorldMatrix for positioning each character on screen using Element's Position, AnchorPoint, Size, Scale plus glyph-specific offsets

#### Rendering Integration (MVP)

- **FR-022**: TextElement MUST emit one DrawCommand per visible glyph (N commands for N characters)
- **FR-023**: Each DrawCommand MUST reference shared font atlas geometry with FirstVertex = charIndex × 4, VertexCount = 4
- **FR-024**: DrawCommand topology MUST be TriangleStrip (4 vertices per quad)
- **FR-025**: TextElement MUST use existing UIElement shader pipeline for rendering (no new shader needed)
- **FR-026**: TextElement MUST bind font atlas texture via descriptor set at set=1, binding=0
- **FR-027**: Push constants MUST include per-glyph WorldMatrix (positioning), tint color (white), and uvRect (0,0,1,1 identity since UVs pre-baked)
- **FR-028**: TextElement MUST participate in UI render pass (RenderPassFlags = UI bit mask)
- **FR-029**: TextElement MUST use default RenderPriority (1000 for UI text layer)
- **FR-030**: Multiple DrawCommands from TextElement MUST batch together naturally (same pipeline, same descriptor set = 1 GPU draw call)

### Key Entities

- **TextElement**: UI component that displays text; extends Element base class with Text property and rendering logic
- **FontResource**: GPU-resident font data including atlas texture, glyph metrics dictionary, and font metrics; managed by FontResourceManager
- **FontDefinition**: Configuration specifying embedded font source (defaults to Roboto Regular 16pt with ASCII printable characters)
- **GlyphInfo**: Metrics for a single character including UV coordinates in atlas, bearing, advance, and dimensions
- **FontAtlas**: GPU texture containing rasterized glyphs; includes ImageView and Sampler for shader access

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: "Hello World" text renders centered on screen with all characters visible and recognizable (validated via pixel sampling)
- **SC-002**: Pixel samples at expected character positions show non-background colors indicating successful rendering
- **SC-003**: TextElement with AnchorPoint (0, 0) centers text around Position (validated via pixel sampling)
- **SC-004**: TextElement with AnchorPoint (-1, -1) aligns text top-left from Position (validated via pixel sampling)
- **SC-005**: N DrawCommands issued per TextElement where N = character count (e.g., 11 for "Hello World") verified via renderer logs
- **SC-006**: All DrawCommands from all TextElements batch into single GPU draw call (verified via Vulkan profiling)
- **SC-007**: Font atlas texture AND geometry generated once per font, reused across multiple TextElements (verified via resource manager logs)
- **SC-008**: 100 TextElements share single 6KB geometry buffer (not 100 separate buffers) - verified via GeometryResourceManager cache
- **SC-009**: Font atlas generation (texture + geometry) completes within 100ms during application startup
- **SC-010**: Memory usage remains stable when creating/destroying TextElements repeatedly (no memory leaks, no geometry churn)
- **SC-011**: TextElement Size property is automatically set to measured text dimensions (width and height in pixels)
- **SC-012**: Dynamic text changes (if implemented) do not trigger geometry uploads - only different FirstVertex offsets in DrawCommands

## Technical Requirements

### Element Positioning System (Inherited)

TextElement inherits Element's well-optimized positioning system. Understanding this is critical:

**Key Properties**:
- **Position** (Vector3D): Where the element IS in screen space (pixels) - from Transformable
- **AnchorPoint** (Vector2D, -1 to 1): Which point of the element aligns with Position
  - (-1, -1) = top-left corner aligns with Position
  - (0, 0) = center aligns with Position  
  - (1, 1) = bottom-right corner aligns with Position
- **Size** (Vector2D<int>): Pixel dimensions of element (width, height)
- **Scale** (Vector3D): Multiplier applied for effects - from Transformable
- **SizeConstraints** (Rectangle<int>): Maximum space available (set by parent/layout)

**WorldMatrix Calculation** (from Element.UpdateLocalMatrix):
```
centerX = Position.X - (AnchorPoint.X × Size.X × 0.5 × Scale.X)
centerY = Position.Y - (AnchorPoint.Y × Size.Y × 0.5 × Scale.Y)

WorldMatrix = Scale(Size.X/2 × Scale.X, Size.Y/2 × Scale.Y, Scale.Z)
            × Translate(centerX, centerY, Position.Z)
```

**For TextElement**:
- Text content is measured to calculate Size automatically
- Glyphs are positioned in local space (element center at origin)
- WorldMatrix transforms local coords → screen space
- Shader applies camera ViewProjection for final result

**Example - Centered "Hello World"**:
```csharp
new TextElementTemplate {
    Text = "Hello World",
    Position = new(960, 540, 0),  // Screen center
    AnchorPoint = new(0, 0),      // Center alignment
    // Size auto-calculated from text measurement
}
```

### Implementation Constraints

1. **Preserve Existing Architecture**: TextElement currently exists; replacement must maintain compatibility with Element base class and existing rendering pipeline
2. **Shader Compatibility**: Must reuse existing `ImageTexture` shader which expects position, UV coordinates, texture sampler, and tint color
3. **Resource Manager Integration**: Must integrate with existing IResourceManager interface and follow established resource management patterns (GetOrCreate, reference counting, Release)
4. **Template System**: Must support template-based creation via TextElementTemplate with [TemplateProperty] for Text field
5. **Component Property System**: Must use [ComponentProperty] attribute for Text field to enable property updates

### Architecture Components

#### Font Atlas Generation (New System)

- **IFontResourceManager**: Interface for font resource lifecycle management (GetOrCreate, Release, caching)
- **FontResourceManager**: Implementation using FreeType library (via StbTrueType or similar) for glyph rasterization
- **FontAtlasBuilder**: Utility for packing rasterized glyphs into optimal texture layout with UV coordinate calculation
- **IFontSource**: Abstraction for font data sources (embedded resources, file paths, URLs)
- **EmbeddedTrueTypeFontSource**: Implementation for loading fonts from embedded assembly resources
- **CharacterRange**: Enumeration/configuration for specifying which characters to include in atlas

#### TextElement Replacement (Core Feature)

- **TextElement**: Completely replace existing implementation with minimal MVP architecture:
  - **Properties**: Text (string only), inherits Position, AnchorPoint, Size, Scale from Element
  - **Lifecycle**: OnActivate loads default font (gets shared FontResource with atlas texture + geometry), creates descriptor set; OnDeactivate releases resources
  - **Rendering**: GetDrawCommands emits N DrawCommands (one per character), each referencing shared geometry at different FirstVertex offset
  - **Geometry**: NO per-element geometry generation - reuses shared font atlas geometry from FontResource
  - **Size Management**: Measures text and sets Size property automatically based on glyph advances and font metrics
  - **Per-Glyph Positioning**: Calculates WorldMatrix per character using Element's transform + glyph-specific offsets

#### Geometry Generation Strategy

**Shared Font Atlas Geometry** (one per font, cached and reused):

```
// Generated once per font at FontResource creation
FontAtlasGeometry = {
    Name: "RobotoRegular16pt_AtlasGeometry",
    Vertices: [
        // Character 'A' (index 0): 4 vertices with position + atlas UVs
        A_v0 { pos: (-1,-1), uv: (0.000, 0.000) },
        A_v1 { pos: (-1, 1), uv: (0.000, 0.063) },
        A_v2 { pos: ( 1,-1), uv: (0.047, 0.000) },
        A_v3 { pos: ( 1, 1), uv: (0.047, 0.063) },
        
        // Character 'B' (index 1): 4 vertices with position + atlas UVs
        B_v0 { pos: (-1,-1), uv: (0.047, 0.000) },
        B_v1 { pos: (-1, 1), uv: (0.047, 0.063) },
        ...
        
        // All 95 ASCII printable characters: 380 vertices total
    ]
}

// TextElement rendering "Hello World"
GetDrawCommands() {
    // 11 DrawCommands, each referencing different section of SAME buffer
    yield DrawCommand { FirstVertex = 'H' * 4, VertexCount = 4, ... }
    yield DrawCommand { FirstVertex = 'e' * 4, VertexCount = 4, ... }
    yield DrawCommand { FirstVertex = 'l' * 4, VertexCount = 4, ... }
    yield DrawCommand { FirstVertex = 'l' * 4, VertexCount = 4, ... }
    yield DrawCommand { FirstVertex = 'o' * 4, VertexCount = 4, ... }
    yield DrawCommand { FirstVertex = ' ' * 4, VertexCount = 4, ... }
    yield DrawCommand { FirstVertex = 'W' * 4, VertexCount = 4, ... }
    // ... etc
}
```

**Benefits**:
- ✅ One 6KB geometry buffer per font (not per TextElement!)
- ✅ 100 TextElements = 6KB upload (not 70KB)
- ✅ All text with same font shares geometry resource
- ✅ No reuploads when text changes (just different FirstVertex offsets)
- ✅ Push constants update glyph position (100× faster than vertex uploads)
- ✅ Batching happens naturally (same pipeline + texture = 1 GPU draw call)

**Performance Characteristics**:
- Static text: 12× less memory, 12× less upload vs per-element geometry
- Dynamic text: No reuploads (push constants only), no GeometryResourceManager churn
- 11 DrawCommands per "Hello World" batch into 1 GPU draw call

**Coordinate Space**:
- Each glyph quad positioned in normalized space (-1 to 1)
- Per-glyph WorldMatrix in push constants positions/scales to screen
- UV coords are pre-baked atlas coordinates (unique per character)
- Shader applies: glyph WorldMatrix → camera ViewProjection

#### Supporting Types (New)

- **FontDefinition**: Record with default configuration (embedded Roboto Regular 16pt, ASCII printable range)
- **FontResource**: Class containing:
  - Atlas texture (TextureResource): GPU texture with all rasterized glyphs
  - Atlas geometry (GeometryResource): Shared vertex buffer with 380 pre-positioned quads
  - Glyph dictionary (Dictionary<char, GlyphInfo>): Per-character metrics
  - Font metrics (Ascender, Descender, LineHeight): Font-level measurements
- **GlyphInfo**: Record containing per-character metrics:
  - CharIndex (int): Offset in shared geometry (FirstVertex = charIndex × 4)
  - UV coords (Vector2D, pre-baked atlas coordinates)
  - Bearing offsets (BearingX, BearingY)
  - Advance width and pixel dimensions
- **UIElementPushConstants**: Struct for push constants (96 bytes):
  - model (Matrix4x4): Per-glyph WorldMatrix (64 bytes)
  - tintColor (Vector4): White (1,1,1,1) for text (16 bytes)
  - uvRect (Vector4): Identity (0,0,1,1) since UVs pre-baked (16 bytes)

### Assumptions

1. **Font Library**: Will use FreeType-compatible library (StbTrueType.NET or similar) for TrueType parsing and rasterization
2. **Character Range**: ASCII printable characters (32-126) only - 95 characters total
3. **Font**: Single default font (Roboto Regular 16pt) embedded in assembly
4. **Atlas Size**: 512x512 texture is sufficient for ASCII printable at 16pt
5. **Atlas Packing**: Simple row-based packing algorithm for texture
6. **Texture Format**: R8_UNORM (single-channel grayscale) format
7. **Shared Geometry**: One geometry buffer per font (6KB for 95 chars × 4 vertices × 16 bytes)
8. **Geometry Caching**: All TextElements with same font share the single geometry resource
9. **Per-Glyph DrawCommands**: N characters = N DrawCommands, all batch into 1 GPU draw call
10. **Spacing**: Simple advance-based spacing without kerning
11. **Text Flow**: Left-to-right, single-line only
12. **Positioning**: Per-glyph WorldMatrix calculates position using Element transform + character offset
13. **Element Integration**: TextElement fully leverages Element's Position, AnchorPoint, Size, Scale, and WorldMatrix
14. **Size Calculation**: TextElement sets its Size property based on measured text dimensions (width from advances, height from font metrics)
15. **SizeConstraints**: Text may exceed SizeConstraints bounds (no clipping/wrapping in MVP)
16. **UIElement Shader**: Reuses existing shader, no new shader compilation needed

### Design Decisions

1. **Complete Replacement**: Replace existing TextElement entirely with minimal MVP implementation
2. **Font Resource Manager**: Create IFontResourceManager following established resource management patterns
3. **Reuse UIElement Shader**: Leverage existing shader (not ImageTexture) - supports ViewProjection UBO, model matrix, textures, and tint color
4. **Shared Font Geometry**: ONE geometry buffer per font containing all character quads (not per-element geometry)
5. **Per-Glyph DrawCommands**: Emit N DrawCommands (one per character) referencing shared geometry at different offsets
6. **Geometry Sharing Optimization**: 100 TextElements share 6KB geometry (not 70KB of unique buffers)
7. **Push Constants for Positioning**: Per-glyph WorldMatrix in push constants (faster than vertex uploads for dynamic text)
8. **Single Default Font**: Hard-code Roboto Regular 16pt with ASCII printable range - no configuration needed for MVP
9. **No Styling Properties**: Text property only - no color or font selection for MVP (uses white tint color, inherits Scale from Element)
10. **Leverage Element Properties**: Use existing Position, AnchorPoint, Size, Scale, WorldMatrix from Element base class - no reimplementation
11. **Size Auto-Calculation**: TextElement calculates and sets its Size property based on measured text dimensions
12. **Anchor Point Alignment**: Fully support Element's anchor point system (-1 to 1 range) for text alignment
13. **No Text Updates**: MVP doesn't require runtime text changes - can be added later (but architecture supports it efficiently)
14. **Descriptor Set Per Element**: Each TextElement allocates its own descriptor set for font atlas texture
15. **No SDF**: Standard rasterized rendering only
16. **Pre-Baked UVs**: Atlas UV coordinates stored in shared geometry (not calculated per-draw)

## Out of Scope

The following features are explicitly **not** included in this MVP specification:

1. **Text Styling**: Color, alignment, font selection - use defaults only
2. **Multi-Line Text**: Newline characters, line wrapping, vertical spacing
3. **Dynamic Text Updates**: Runtime text changes (can set once at creation only)
4. **Multiple Fonts**: Single embedded default font only
5. **Font Sizes**: Single default size (16pt) only
6. **Text Alignment**: Left-aligned starting at element position only
7. **Text Clipping/Wrapping**: Text extends beyond bounds if too long
8. **Unicode Characters**: ASCII printable only (no emoji, accented letters)
9. **Kerning**: Simple advance-based spacing only
10. **Signed Distance Field (SDF) Rendering**: Standard rasterization only
11. **Rich Text Markup**: Plain text strings only
12. **Text Selection/Editing**: Display only, no interaction
13. **Text Measurement**: No API for calculating text dimensions
14. **Font Fallback**: Single font, no fallback mechanism
15. **Custom Fonts**: Embedded default font only

## Dependencies

1. **Element Base Class**: TextElement extends Element and depends on its properties (Position, Size, Bounds, RenderPriority, WorldMatrix) and lifecycle methods
2. **ImageTexture Shader**: Reuses existing image_texture.vert/frag shaders with position, UV, texture sampler, and tint color support
3. **IResourceManager**: Requires IResourceManager.Fonts property to access FontResourceManager
4. **IDescriptorManager**: Depends on descriptor set allocation and texture binding (UpdateDescriptorSet overload for image/sampler)
5. **IGeometryResourceManager**: Depends on dynamic geometry creation via GetOrCreate with VertexArrayGeometrySource
6. **FreeType Library**: Requires TrueType parsing and rasterization library (StbTrueType.NET or similar NuGet package)
7. **Embedded Font Resource**: Requires Roboto-Regular.ttf embedded in assembly as default font
8. **Vulkan Context**: Depends on IGraphicsContext for GPU texture creation (Image, ImageView, Sampler)
9. **Command Pool**: Depends on ICommandPoolManager for uploading font atlas texture data to GPU

## Risks and Mitigations

### Risk 1: FreeType Library Integration Complexity
**Description**: Integrating native FreeType library or C# wrapper may have platform-specific issues (Windows, Linux, macOS) or memory management challenges.

**Likelihood**: Medium (native libraries often have platform quirks)

**Impact**: High (blocks entire feature if font loading doesn't work)

**Mitigation**:
- Use well-maintained pure C# library like StbTrueType.NET (avoiding native dependencies)
- Test on Windows initially (primary development platform); defer cross-platform testing
- Create abstraction layer (IFontSource) to allow swapping font loading libraries if needed
- Have fallback to loading pre-generated font atlases as textures if rasterization fails

### Risk 2: Atlas Size Limitations
**Description**: Large character ranges or font sizes may exceed maximum texture size (typically 4096x4096), causing atlas generation to fail.

**Likelihood**: Low (standard use cases fit in 1024x1024)

**Impact**: Medium (developers can't use large fonts or extended character sets)

**Mitigation**:
- Document recommended character ranges and font sizes for optimal atlas sizes
- Calculate required atlas size before generation and warn if approaching limits
- Future enhancement: Support multiple atlas textures if single atlas insufficient
- Default CharacterRange (ASCII printable) fits comfortably in 512x512 at typical sizes

### Risk 3: Text Rendering Clarity
**Description**: Rendered text may appear blurry or pixelated depending on texture filtering and screen resolution.

**Likelihood**: Medium (common issue with bitmap fonts)

**Impact**: Low (MVP acceptance not dependent on perfect visual quality)

**Mitigation**:
- Use point sampling (nearest neighbor) filtering for crisp pixel-perfect text at native resolution
- Render at 1:1 pixel ratio (no scaling) for MVP
- Document known limitation; SDF rendering can address this in future
- Focus on functional correctness first, visual quality second

### Risk 4: Memory Leaks from Descriptor Sets
**Description**: Each TextElement allocates descriptor set; improper cleanup could leak GPU memory over time.

**Likelihood**: Low (architecture follows established patterns)

**Impact**: High (memory leak crashes application eventually)

**Mitigation**:
- Follow existing resource management patterns from TexturedElement example
- Implement rigorous integration tests creating/destroying TextElements repeatedly
- Use memory profiling tools to detect leaks during development
- Ensure OnDeactivate releases all GPU resources (descriptor sets, geometry)

### Risk 5: Shader Compatibility Issues
**Description**: ImageTexture shader may have assumptions or limitations that don't work well for text rendering (blending, filtering, coordinate precision).

**Likelihood**: Low (shader designed for textures with UV mapping)

**Impact**: Medium (visual artifacts in text rendering)

**Mitigation**:
- Review ImageTexture shader implementation before starting work
- Test early with simple text rendering to validate shader compatibility
- Be prepared to create text-specific shader if ImageTexture doesn't work well
- Ensure proper texture filtering (point sampling for crisp text at native resolution)

### Risk 6: Glyph Positioning Precision
**Description**: Calculating proper glyph positions using font metrics may have rounding or alignment issues causing visual artifacts.

**Likelihood**: Medium (floating-point precision issues are common)

**Impact**: Low (minor visual issues in MVP, can refine later)

**Mitigation**:
- Use integer positioning where possible for pixel-perfect alignment
- Test with "Hello World" specifically to catch obvious positioning errors
- Reference font rendering best practices from established libraries
- Document known limitations; can refine in future iterations

## Implementation Notes

### Shader Reuse Strategy

TextElement uses the existing **UIElement shader** (not ImageTexture shader):

```glsl
// ui.vert
layout(push_constant) uniform PushConstants {
    mat4 model;      // Per-glyph WorldMatrix
    vec4 tintColor;  // White (1,1,1,1) for text
    vec4 uvRect;     // Identity (0,0,1,1) - UVs pre-baked in geometry
};
```

**Why UIElement shader works perfectly**:
- ✅ Supports ViewProjection UBO (set=0, binding=0) from camera
- ✅ Supports per-draw model matrix in push constants
- ✅ Supports texture sampling (set=1, binding=0) for font atlas
- ✅ Supports tint color for text rendering
- ✅ uvRect transform allows identity (0,0,1,1) since UVs pre-baked

**No new shader needed!**

### Per-Glyph WorldMatrix Calculation

```csharp
Matrix4x4 CalculateGlyphWorldMatrix(char c, int glyphIndex) {
    // Element's base transform (Position, AnchorPoint, Size, Scale)
    var baseTransform = this.WorldMatrix;
    
    // Glyph offset within text (horizontal advance)
    var glyphOffset = CalculateGlyphOffset(glyphIndex);
    
    // Glyph-specific translation
    var glyphTranslation = Matrix4x4.CreateTranslation(glyphOffset, 0, 0);
    
    return glyphTranslation * baseTransform;
}
```

## Next Steps

After this specification is approved:

1. **Planning Phase** (`/speckit.plan`):
   - Break down requirements into concrete implementation tasks
   - Identify dependencies between tasks
   - Estimate effort for each task
   - Create development checklist
2. **MVP Implementation Order** (recommended):
   1. Research and select font library (StbTrueType.NET)
   2. Implement FontDefinition, GlyphInfo, FontResource data structures
   3. Implement IFontResourceManager interface and FontResourceManager
   4. Implement font atlas texture generation (rasterization, packing, UV calculation)
   5. Implement font atlas geometry generation (380 quads with pre-baked UVs)
   6. Implement GPU uploads for font atlas texture and geometry
   7. Replace TextElement implementation with shared geometry approach
   8. Implement per-glyph DrawCommand emission with FirstVertex offsets
   9. Implement per-glyph WorldMatrix calculation for positioning
   10. Create integration test: render "Hello World" and sample pixels
   11. Verify resource sharing: multiple TextElements, one atlas texture + geometry
   12. Document usage and known limitations

---

**End of Specification**
