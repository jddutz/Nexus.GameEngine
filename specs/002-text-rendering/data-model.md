# Data Model: Text Rendering with TextElement

**Feature**: Text Rendering with TextElement  
**Phase**: Phase 1 - Design & Contracts  
**Date**: 2025-11-04

## Entity Diagram

```
┌─────────────────────┐
│   TextElement       │
│  (IRuntimeComponent)│
├─────────────────────┤
│ + Text: string      │
│ + Position: Vector3D│
│ + AnchorPoint: Vec2D│
│ + Size: Vector2D    │
│ + Scale: Vector3D   │
├─────────────────────┤
│ - _fontDefinition   │
│ - _fontResource     │
│ - _descriptorSet    │
└──────────┬──────────┘
           │ uses
           ↓
┌─────────────────────┐
│  FontResource       │
├─────────────────────┤
│ + AtlasTexture      │──→ TextureResource (GPU texture)
│ + SharedGeometry    │──→ GeometryResource (380 vertices)
│ + GlyphInfo: Dict   │──→ Dictionary<char, GlyphInfo>
│ + FontMetrics       │──→ (Ascender, Descender, LineHeight)
└──────────┬──────────┘
           │ managed by
           ↓
┌─────────────────────┐
│FontResourceManager  │
│ (IFontResourceManager)
├─────────────────────┤
│ + GetOrCreate()     │
│ + Release()         │
│ - _cache: Dict      │
└─────────────────────┘

┌─────────────────────┐
│  FontDefinition     │
│  (record)           │
├─────────────────────┤
│ + Source: IFontSource│
│ + Size: float       │
│ + CharacterRange    │
└─────────────────────┘

┌─────────────────────┐
│    GlyphInfo        │
│    (record)         │
├─────────────────────┤
│ + CharIndex: int    │  → FirstVertex = CharIndex × 4
│ + UV0: Vector2D     │  → Pre-baked atlas coordinates
│ + UV1: Vector2D     │
│ + Width: int        │  → Pixel dimensions
│ + Height: int       │
│ + BearingX: int     │  → Horizontal offset
│ + BearingY: int     │  → Vertical offset (baseline)
│ + Advance: int      │  → Horizontal spacing
└─────────────────────┘
```

## Entities

### 1. TextElement

**Type**: RuntimeComponent (extends Element)  
**Purpose**: UI component that displays text using font atlases  
**Lifecycle**: OnActivate loads font resource, OnDeactivate releases resources  
**Rendering**: Emits N DrawCommands per frame (one per visible character)

**Fields**:
```csharp
[TemplateProperty]
[ComponentProperty]
private string _text = string.Empty;

[TemplateProperty(Name = "Font")]
private FontDefinition? _fontDefinition = null;

[ComponentProperty]
private FontResource? _fontResource;

private VkDescriptorSet _descriptorSet;
```

**Properties** (auto-generated from attributes):
- `Text` (string): Text content to display
- Inherits from Element: `Position`, `AnchorPoint`, `Size`, `Scale`, `WorldMatrix`

**Key Methods**:
- `OnActivate()`: Load font resource, create descriptor set, calculate Size from text measurement
- `GetDrawCommands()`: Emit per-glyph DrawCommands referencing shared geometry
- `OnDeactivate()`: Release descriptor set and font resource reference
- `CalculateGlyphWorldMatrix(char c, int index)`: Calculate per-glyph positioning

**Validation Rules**:
- Text can be empty string or null (treated as empty, no rendering)
- Font definition defaults to embedded Roboto Regular 16pt if not specified
- Size is automatically calculated from text dimensions (no manual override in MVP)

**State Transitions**:
1. **Inactive** → **Activating**: OnActivate() loads font, creates descriptor set
2. **Activating** → **Active**: Font loaded, descriptor set allocated, ready to render
3. **Active** → **Deactivating**: OnDeactivate() releases resources
4. **Deactivating** → **Inactive**: Resources released, component cleaned up

---

### 2. FontResource

**Type**: Class  
**Purpose**: GPU-resident font data (atlas texture + shared geometry + metrics)  
**Lifecycle**: Created by FontResourceManager, cached and reused, reference counted  
**Memory**: ~262KB atlas texture (512×512 R8) + 6KB shared geometry + ~10KB metrics

**Fields**:
```csharp
public required TextureResource AtlasTexture { get; init; }
public required GeometryResource SharedGeometry { get; init; }
public required IReadOnlyDictionary<char, GlyphInfo> GlyphInfo { get; init; }
public required FontMetrics Metrics { get; init; }
public required string Name { get; init; }
```

**Properties**:
- `AtlasTexture` (TextureResource): GPU texture containing rasterized glyphs (R8_UNORM format)
- `SharedGeometry` (GeometryResource): Vertex buffer with 380 pre-positioned quads (95 chars × 4 vertices)
- `GlyphInfo` (Dictionary<char, GlyphInfo>): Per-character metrics and UV coordinates
- `Metrics` (FontMetrics): Font-level measurements (ascender, descender, line height)
- `Name` (string): Unique identifier for caching (e.g., "RobotoRegular16pt")

**Relationships**:
- Created by `IFontResourceManager.GetOrCreate(FontDefinition)`
- Referenced by multiple `TextElement` instances (shared resource)
- AtlasTexture managed by underlying IBufferManager/IGraphicsContext
- SharedGeometry managed by IGeometryResourceManager

**Validation Rules**:
- AtlasTexture must be valid Vulkan image with R8_UNORM format
- SharedGeometry must contain exactly (characterCount × 4) vertices
- GlyphInfo dictionary must contain entry for every character in font
- All UV coordinates must be in valid [0,1] range

---

### 3. FontDefinition

**Type**: Record (immutable configuration)  
**Purpose**: Specifies font to load (source, size, character range)  
**Usage**: Template-time configuration, passed to FontResourceManager

**Fields**:
```csharp
public record FontDefinition
{
    public required IFontSource Source { get; init; }
    public float Size { get; init; } = 16.0f;
    public CharacterRange CharacterRange { get; init; } = CharacterRange.AsciiPrintable;
}

public static class FontDefinition
{
    public static FontDefinition Default => new() {
        Source = new EmbeddedTrueTypeFontSource("Roboto-Regular.ttf"),
        Size = 16.0f,
        CharacterRange = CharacterRange.AsciiPrintable
    };
}
```

**Properties**:
- `Source` (IFontSource): Font data source (embedded resource, file path, etc.)
- `Size` (float): Font size in points (default 16pt)
- `CharacterRange` (CharacterRange): Which characters to include in atlas (default ASCII printable 32-126)

**Validation Rules**:
- Source must not be null
- Size must be positive (> 0)
- CharacterRange must be valid enumeration value

**Relationships**:
- Used by TextElement to specify desired font
- Passed to FontResourceManager.GetOrCreate() for resource lookup/creation
- Multiple TextElements can share same FontDefinition → same FontResource

---

### 4. GlyphInfo

**Type**: Record (immutable metrics)  
**Purpose**: Per-character metrics and atlas coordinates  
**Storage**: Dictionary<char, GlyphInfo> in FontResource

**Fields**:
```csharp
public record GlyphInfo
{
    public required int CharIndex { get; init; }      // 0-94 for ASCII printable
    public required Vector2D<float> UV0 { get; init; }  // Top-left atlas coordinate
    public required Vector2D<float> UV1 { get; init; }  // Bottom-right atlas coordinate
    public required int Width { get; init; }          // Glyph width in pixels
    public required int Height { get; init; }         // Glyph height in pixels
    public required int BearingX { get; init; }       // Horizontal bearing offset
    public required int BearingY { get; init; }       // Vertical bearing (baseline)
    public required int Advance { get; init; }        // Horizontal advance width
}
```

**Properties**:
- `CharIndex` (int): Character index in atlas (0-94 for ASCII printable), used to calculate FirstVertex = CharIndex × 4
- `UV0` (Vector2D<float>): Top-left UV coordinate in atlas (0-1 range)
- `UV1` (Vector2D<float>): Bottom-right UV coordinate in atlas (0-1 range)
- `Width` (int): Glyph width in pixels
- `Height` (int): Glyph height in pixels
- `BearingX` (int): Horizontal offset from cursor to left edge of glyph
- `BearingY` (int): Vertical offset from baseline to top edge of glyph
- `Advance` (int): Horizontal distance to advance cursor for next character

**Relationships**:
- Stored in FontResource.GlyphInfo dictionary (key = character)
- Used by TextElement.CalculateGlyphWorldMatrix() for positioning
- UV coordinates match regions in FontResource.AtlasTexture

**Validation Rules**:
- CharIndex must be non-negative and unique per character
- UV coordinates must be in [0,1] range
- Width, Height, Advance must be non-negative
- BearingX, BearingY can be negative (for glyphs extending below baseline)

**Usage Example**:
```csharp
// Get glyph info for 'A'
var glyphA = fontResource.GlyphInfo['A'];

// Calculate FirstVertex offset in shared geometry
int firstVertex = glyphA.CharIndex * 4;  // e.g., 33 * 4 = 132

// Draw glyph
yield return new DrawCommand {
    GeometryResource = fontResource.SharedGeometry,
    FirstVertex = firstVertex,
    VertexCount = 4,
    ...
};
```

---

### 5. FontMetrics

**Type**: Record (font-level measurements)  
**Purpose**: Font-wide metrics for text layout  
**Storage**: FontResource.Metrics property

**Fields**:
```csharp
public record FontMetrics
{
    public required int Ascender { get; init; }   // Distance from baseline to top
    public required int Descender { get; init; }  // Distance from baseline to bottom (negative)
    public required int LineHeight { get; init; } // Total line height (ascender - descender + gap)
    public required float Scale { get; init; }    // Scale factor applied during rasterization
}
```

**Properties**:
- `Ascender` (int): Distance from baseline to highest point (positive)
- `Descender` (int): Distance from baseline to lowest point (negative)
- `LineHeight` (int): Recommended vertical spacing between lines
- `Scale` (float): Scale factor used during glyph rasterization

**Relationships**:
- Stored in FontResource
- Used by TextElement to calculate Size.Y (text height)
- Used for future multi-line text layout (out of scope for MVP)

**Validation Rules**:
- Ascender must be positive
- Descender must be negative
- LineHeight must be positive
- Scale must be positive

---

### 6. IFontSource (Interface)

**Type**: Interface  
**Purpose**: Abstraction for loading font data from different sources  
**Implementations**: EmbeddedTrueTypeFontSource (MVP), future: FileFontSource, UriFontSource

**Contract**:
```csharp
public interface IFontSource
{
    byte[] LoadFontData();
    string GetUniqueName();
}
```

**Methods**:
- `LoadFontData()`: Returns TrueType font data as byte array
- `GetUniqueName()`: Returns unique identifier for caching (e.g., "Roboto-Regular.ttf")

**Implementations**:

**EmbeddedTrueTypeFontSource**:
```csharp
public class EmbeddedTrueTypeFontSource : IFontSource
{
    private readonly string _resourceName;
    
    public EmbeddedTrueTypeFontSource(string resourceName) {
        _resourceName = resourceName;
    }
    
    public byte[] LoadFontData() {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = $"GameEngine.EmbeddedResources.Fonts.{_resourceName}";
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null) throw new FileNotFoundException($"Embedded font '{_resourceName}' not found");
        
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
    
    public string GetUniqueName() => _resourceName;
}
```

**Validation Rules**:
- LoadFontData() must return valid TrueType font data
- GetUniqueName() must return consistent identifier for same font

---

## Relationships

### TextElement → FontResource
- **Cardinality**: Many-to-One (many TextElements share one FontResource)
- **Lifetime**: TextElement acquires FontResource in OnActivate(), releases in OnDeactivate()
- **Caching**: FontResourceManager ensures single FontResource per unique FontDefinition

### FontResource → TextureResource
- **Cardinality**: One-to-One (each FontResource has one atlas texture)
- **Lifetime**: TextureResource created during FontResource initialization, disposed with FontResource
- **Ownership**: FontResource owns TextureResource lifecycle

### FontResource → GeometryResource
- **Cardinality**: One-to-One (each FontResource has one shared geometry buffer)
- **Lifetime**: GeometryResource created during FontResource initialization, cached by GeometryResourceManager
- **Ownership**: Shared ownership via GeometryResourceManager (reference counted)

### FontResource → GlyphInfo
- **Cardinality**: One-to-Many (one FontResource contains many GlyphInfo records)
- **Lifetime**: GlyphInfo dictionary populated during FontResource initialization, immutable thereafter
- **Storage**: Stored as Dictionary<char, GlyphInfo> in FontResource

---

## Data Flow

### Font Atlas Generation (One-Time per Font)
```
FontDefinition
    ↓
IFontSource.LoadFontData()
    ↓
StbTrueType.CreateFont()
    ↓
Rasterize Glyphs (95 characters)
    ↓
Pack into Atlas Texture (row-based packing)
    ↓
Upload to GPU (R8_UNORM texture)
    ↓
Generate Shared Geometry (380 vertices with pre-baked UVs)
    ↓
Upload to GPU (vertex buffer)
    ↓
FontResource {
    AtlasTexture,
    SharedGeometry,
    GlyphInfo dictionary,
    FontMetrics
}
    ↓
Cache in FontResourceManager
```

### Text Rendering (Per Frame, Per TextElement)
```
TextElement.GetDrawCommands()
    ↓
For each character in text:
    ↓
    Look up GlyphInfo in dictionary
    ↓
    Calculate per-glyph WorldMatrix
    ↓
    Emit DrawCommand {
        GeometryResource = fontResource.SharedGeometry,
        FirstVertex = glyphInfo.CharIndex × 4,
        VertexCount = 4,
        PushConstants = { glyphWorldMatrix, tintColor, uvRect },
        DescriptorSet = { fontResource.AtlasTexture }
    }
    ↓
Renderer batches DrawCommands (same pipeline + texture)
    ↓
GPU renders N glyphs in 1 draw call
```

---

## Memory Footprint

### Per FontResource (Shared)
- **Atlas Texture**: 512×512×1 byte (R8) = 262,144 bytes (~256 KB)
- **Shared Geometry**: 95 chars × 4 vertices × 16 bytes = 6,080 bytes (~6 KB)
- **GlyphInfo Dictionary**: 95 entries × ~100 bytes = 9,500 bytes (~10 KB)
- **FontMetrics**: ~50 bytes
- **Total**: ~272 KB per unique font (shared across all TextElements)

### Per TextElement Instance
- **_fontResource reference**: 8 bytes (pointer)
- **_descriptorSet**: 8 bytes (VkDescriptorSet handle)
- **_text**: 8 bytes (string reference) + string heap allocation
- **Inherited fields** (Position, Size, Scale, etc.): ~100 bytes
- **Total**: ~124 bytes + text string allocation

### 100 TextElements with Same Font
- **FontResource**: 272 KB (shared, one-time)
- **TextElement instances**: 100 × ~124 bytes = 12.4 KB
- **Text strings**: Variable (depends on text content)
- **Total**: ~285 KB (vs 27 MB without shared geometry!)

---

## Validation & Invariants

### FontResource Invariants
- AtlasTexture dimensions must be power of 2 (512×512, 1024×1024, etc.)
- SharedGeometry vertex count = characterCount × 4
- GlyphInfo dictionary size = characterCount
- All UV coordinates in [0,1] range
- All GlyphInfo.CharIndex values unique and sequential

### TextElement Invariants
- Text property never null (empty string by default)
- FontResource is null when inactive, non-null when active
- DescriptorSet allocated in OnActivate, released in OnDeactivate
- Size property automatically calculated from text measurement (read-only in MVP)

### FontResourceManager Invariants
- Cache key = FontDefinition (Source.GetUniqueName() + Size + CharacterRange)
- Reference counting ensures resources released when unused
- No duplicate FontResources for same FontDefinition

---

## Next Steps

1. Create API contracts in `/contracts/` directory
2. Generate `quickstart.md` with usage examples
3. Update agent context with StbTrueTypeSharp technology
4. Re-evaluate Constitution Check
5. Proceed to Phase 2: Task Breakdown (via `/speckit.tasks` command)

**Phase 1: Data Model Complete** ✅
