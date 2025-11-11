# Research: Text Rendering with TextElement

**Feature**: Text Rendering with TextElement  
**Phase**: Phase 0 - Outline & Research  
**Date**: 2025-11-04

## Research Tasks

Based on Technical Context unknowns and technology choices, the following research tasks were identified:

1. **FreeType Library Selection**: Evaluate C# FreeType wrappers for TrueType font parsing and glyph rasterization
2. **Font Atlas Packing**: Research optimal atlas texture packing algorithms for glyph layout
3. **Vulkan Texture Formats**: Determine best texture format for font atlas (R8_UNORM vs RGBA8)
4. **Shared Geometry Best Practices**: Research patterns for shared vertex buffer usage with multiple draw commands
5. **Per-Glyph WorldMatrix Calculation**: Research coordinate space transformations for glyph positioning
6. **UIElement Shader Compatibility**: Verify existing shader supports text rendering requirements

## Research Findings

### 1. FreeType Library Selection

**Decision**: Use **StbTrueTypeSharp** (pure C# port of stb_truetype.h)

**Rationale**:
- **Pure C#**: No native library dependencies, simplifies deployment (no platform-specific binaries)
- **Proven**: stb_truetype is widely used, battle-tested library (used in Unity, Unreal, etc.)
- **Simple API**: Straightforward loading, rasterization, and metrics extraction
- **MIT Licensed**: Compatible with project licensing
- **Active Maintenance**: NuGet package actively maintained with .NET Standard 2.0 support
- **Embedded Resource Support**: Can load directly from byte arrays (perfect for embedded fonts)

**Alternatives Considered**:
- **SharpFont** (FreeType wrapper): Requires native FreeType.dll deployment, platform-specific builds, more complex
- **SixLabors.Fonts**: More heavyweight, designed for complex text layout (overkill for MVP)
- **Typography library**: Excellent but larger scope than needed for simple glyph rasterization

**Implementation Pattern**:
```csharp
using StbTrueTypeSharp;

// Load font from embedded resource
var fontBytes = EmbeddedResources.GetFont("Roboto-Regular.ttf");
var fontInfo = StbTrueType.CreateFont(fontBytes, 0);

// Rasterize glyph at specified size
var scale = StbTrueType.ScaleForPixelHeight(fontInfo, 16.0f);
var bitmap = StbTrueType.GetCodepointBitmap(fontInfo, scale, scale, 'A', 
    out int width, out int height, out int xOffset, out int yOffset);

// Get glyph metrics
StbTrueType.GetCodepointHMetrics(fontInfo, 'A', out int advanceWidth, out int leftSideBearing);
```

**NuGet Package**: `StbTrueTypeSharp` (latest stable version)

---

### 2. Font Atlas Packing

**Decision**: Use **row-based packing** with fixed character order (ASCII 32-126)

**Rationale**:
- **Simplicity**: MVP doesn't require optimal packing, row-based is trivial to implement
- **Predictability**: Fixed character order makes debugging easier (known atlas layout)
- **Sufficient Efficiency**: ASCII printable at 16pt fits comfortably in 512x512 with row packing
- **Fast Generation**: No complex bin-packing algorithm overhead
- **Easy UV Calculation**: Linear calculation based on character index

**Alternatives Considered**:
- **Shelf Packing**: More efficient but added complexity, unnecessary for 95 characters
- **RectangleBinPack**: Optimal space usage but overkill for MVP's small character set
- **Dynamic Sizing**: Could grow atlas as needed, but fixed size is simpler and sufficient

**Implementation Strategy**:
```csharp
// Simple row-based packing
int atlasWidth = 512, atlasHeight = 512;
int padding = 2;  // Pixels between glyphs to prevent bleeding
int currentX = padding, currentY = padding, rowHeight = 0;

foreach (char c in asciiPrintableRange) {
    var glyphBitmap = RasterizeGlyph(c, fontSize);
    
    // Check if glyph fits in current row
    if (currentX + glyphBitmap.Width + padding > atlasWidth) {
        // Move to next row
        currentX = padding;
        currentY += rowHeight + padding;
        rowHeight = 0;
    }
    
    // Copy glyph to atlas at (currentX, currentY)
    CopyToAtlas(glyphBitmap, currentX, currentY);
    
    // Calculate UV coordinates (0-1 range)
    float u0 = (float)currentX / atlasWidth;
    float v0 = (float)currentY / atlasHeight;
    float u1 = (float)(currentX + glyphBitmap.Width) / atlasWidth;
    float v1 = (float)(currentY + glyphBitmap.Height) / atlasHeight;
    
    // Store glyph info
    glyphInfoDict[c] = new GlyphInfo {
        CharIndex = charIndex++,
        UV0 = new Vector2D<float>(u0, v0),
        UV1 = new Vector2D<float>(u1, v1),
        Width = glyphBitmap.Width,
        Height = glyphBitmap.Height,
        BearingX = glyphBitmap.BearingX,
        BearingY = glyphBitmap.BearingY,
        Advance = glyphBitmap.Advance
    };
    
    // Advance position
    currentX += glyphBitmap.Width + padding;
    rowHeight = Math.Max(rowHeight, glyphBitmap.Height);
}
```

**Expected Atlas Size**: ~256x128 for ASCII printable at 16pt (well within 512x512 limit)

---

### 3. Vulkan Texture Format

**Decision**: Use **R8_UNORM** (single-channel 8-bit grayscale)

**Rationale**:
- **Memory Efficiency**: 1 byte per pixel vs 4 bytes for RGBA8 (75% smaller)
- **Bandwidth Efficiency**: Less GPU memory bandwidth for texture sampling
- **Sufficient Precision**: 8-bit grayscale adequate for anti-aliased text
- **Shader Simplicity**: Sample single channel, multiply by tint color for final color
- **Standard Format**: Widely supported across all Vulkan implementations

**Alternatives Considered**:
- **RGBA8_UNORM**: 4× larger, wastes RGB channels (alpha channel duplicated to all channels anyway)
- **A8_UNORM**: Alpha-only format, but R8 is more universally supported and equally efficient
- **BC4_UNORM**: Compressed format, but adds compression overhead and complexity for minimal benefit at 512x512

**Shader Sampling Pattern**:
```glsl
// ui.frag
layout(set = 1, binding = 0) uniform sampler2D fontAtlas;

void main() {
    // Sample single channel (R = grayscale alpha)
    float alpha = texture(fontAtlas, fragTexCoord).r;
    
    // Apply tint color (white for default text)
    vec4 textColor = tintColor * alpha;
    
    outColor = textColor;
}
```

**Format Specification**:
- **Vulkan Format**: `VK_FORMAT_R8_UNORM`
- **Usage Flags**: `VK_IMAGE_USAGE_TRANSFER_DST_BIT | VK_IMAGE_USAGE_SAMPLED_BIT`
- **Tiling**: `VK_IMAGE_TILING_OPTIMAL`
- **Memory Properties**: `VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT`

---

### 4. Shared Geometry Best Practices

**Decision**: Create **single large vertex buffer** with pre-positioned quads for all characters

**Rationale**:
- **Zero Redundancy**: One copy of geometry shared by all TextElements
- **No Dynamic Uploads**: Text changes only affect DrawCommand FirstVertex offset (push constants update position)
- **Optimal Batching**: All DrawCommands reference same geometry buffer → automatic batching
- **Cache Friendly**: GeometryResourceManager caches by definition, reused across elements
- **Vulkan Best Practice**: Prefer fewer large buffers over many small buffers

**Implementation Pattern**:
```csharp
// FontResourceManager.GenerateSharedGeometry()
var vertices = new List<Vertex>();

foreach (var kvp in glyphInfoDict.OrderBy(x => x.Value.CharIndex)) {
    char c = kvp.Key;
    GlyphInfo glyph = kvp.Value;
    
    // Pre-positioned quad in normalized space (-1 to 1)
    // Shader will transform via per-glyph WorldMatrix
    float left = -1.0f;
    float right = 1.0f;
    float top = 1.0f;
    float bottom = -1.0f;
    
    // Pre-baked UV coordinates from atlas
    float u0 = glyph.UV0.X, v0 = glyph.UV0.Y;
    float u1 = glyph.UV1.X, v1 = glyph.UV1.Y;
    
    // 4 vertices per quad (TriangleStrip topology)
    vertices.Add(new Vertex { Position = new(left, bottom), UV = new(u0, v1) });  // Bottom-left
    vertices.Add(new Vertex { Position = new(left, top), UV = new(u0, v0) });     // Top-left
    vertices.Add(new Vertex { Position = new(right, bottom), UV = new(u1, v1) }); // Bottom-right
    vertices.Add(new Vertex { Position = new(right, top), UV = new(u1, v0) });    // Top-right
}

// Create shared geometry resource
var geometryDef = new GeometryDefinition {
    Name = $"{fontName}_AtlasGeometry",
    Source = new VertexArrayGeometrySource(vertices.ToArray(), null),  // No indices
    Topology = PrimitiveTopology.TriangleStrip
};

var sharedGeometry = geometryResourceManager.GetOrCreate(geometryDef);
return sharedGeometry;
```

**DrawCommand Emission** (TextElement.GetDrawCommands):
```csharp
foreach (char c in text) {
    if (!glyphInfoDict.TryGetValue(c, out var glyph)) continue;
    
    // Calculate per-glyph WorldMatrix (Element transform + glyph offset)
    var glyphWorldMatrix = CalculateGlyphWorldMatrix(c, glyphIndex++);
    
    yield return new DrawCommand {
        GeometryResource = fontResource.SharedGeometry,  // SAME buffer for all glyphs
        FirstVertex = glyph.CharIndex * 4,  // Offset into shared buffer
        VertexCount = 4,
        Topology = PrimitiveTopology.TriangleStrip,
        PushConstants = new UIElementPushConstants {
            model = glyphWorldMatrix,
            tintColor = new Vector4D<float>(1, 1, 1, 1),
            uvRect = new Vector4D<float>(0, 0, 1, 1)  // Identity (UVs pre-baked)
        },
        DescriptorSet = descriptorSet,  // Font atlas texture
        RenderPassFlags = RenderPassFlags.UI,
        RenderPriority = 1000
    };
}
```

**Performance Benefits**:
- **Memory**: 6KB buffer shared by 100 TextElements (not 70KB of unique buffers)
- **Upload**: One-time 6KB upload vs 70KB per-element uploads
- **Batching**: N DrawCommands batch into 1 GPU draw call (same pipeline, same texture)

---

### 5. Per-Glyph WorldMatrix Calculation

**Decision**: Combine **Element base transform** with **per-glyph offset**

**Rationale**:
- **Leverages Element System**: TextElement inherits Position, AnchorPoint, Size, Scale from Element
- **Per-Glyph Positioning**: Each character needs unique position within text string
- **Maintains Transformability**: Scale, rotation (future) work correctly per-glyph
- **Efficient**: Matrix multiplication in push constants (fast CPU-side, no GPU overhead)

**Coordinate Space Flow**:
1. **Local Glyph Space**: Normalized quad (-1 to 1) in shared geometry
2. **Glyph Transform**: Scale to actual glyph size, translate by bearing + advance offset
3. **Element Transform**: Apply Element's Position, AnchorPoint, Size, Scale
4. **World Space**: Final screen pixel coordinates
5. **Camera Transform**: Shader applies ViewProjection UBO for clip space

**Implementation**:
```csharp
private Matrix4x4 CalculateGlyphWorldMatrix(char c, int glyphIndex) {
    var glyph = fontResource.GlyphInfo[c];
    
    // Calculate cumulative text advance up to this glyph
    float xOffset = 0;
    for (int i = 0; i < glyphIndex; i++) {
        char prevChar = text[i];
        if (fontResource.GlyphInfo.TryGetValue(prevChar, out var prevGlyph)) {
            xOffset += prevGlyph.Advance;
        }
    }
    
    // Glyph-specific transform
    // Scale normalized quad (-1,1) to actual pixel dimensions
    float scaleX = glyph.Width / 2.0f;   // Half-extents
    float scaleY = glyph.Height / 2.0f;
    
    // Position glyph at bearing offset + cumulative advance
    float translateX = xOffset + glyph.BearingX + scaleX;
    float translateY = glyph.BearingY - scaleY;  // Bearing is top-left, center is offset
    
    var glyphLocal = Matrix4x4.CreateScale(scaleX, scaleY, 1.0f) *
                     Matrix4x4.CreateTranslation(translateX, translateY, 0);
    
    // Combine with Element's WorldMatrix (handles Position, AnchorPoint, Size, Scale)
    return glyphLocal * this.WorldMatrix;
}
```

**Element WorldMatrix** (inherited from Element.UpdateLocalMatrix):
```csharp
// Element calculates this automatically
float centerX = Position.X - (AnchorPoint.X × Size.X × 0.5f × Scale.X);
float centerY = Position.Y - (AnchorPoint.Y × Size.Y × 0.5f × Scale.Y);

WorldMatrix = Matrix4x4.CreateScale(Size.X/2 × Scale.X, Size.Y/2 × Scale.Y, Scale.Z) *
              Matrix4x4.CreateTranslation(centerX, centerY, Position.Z);
```

**Example - "Hello World" Centered**:
- TextElement Position = (960, 540) (screen center)
- AnchorPoint = (0, 0) (center alignment)
- Size = (measured text width, font line height) = (110, 20) pixels
- Scale = (1, 1, 1)
- Element WorldMatrix positions text center at (960, 540)
- Per-glyph matrices offset each character relative to text center

---

### 6. UIElement Shader Compatibility

**Decision**: **Reuse existing `ui.vert` and `ui.frag` shaders** without modification

**Rationale**:
- **Perfect Compatibility**: Shader already supports all required features:
  - ViewProjection UBO (set=0, binding=0) from camera
  - Per-draw model matrix in push constants
  - Texture sampling (set=1, binding=0)
  - Tint color in push constants
  - uvRect transform in push constants (can use identity)
- **Zero Shader Work**: No new shader development, compilation, or testing needed
- **Proven System**: Shader already tested and working for textured UI elements
- **Batch Compatible**: Shader pipeline hash includes same descriptor set layout → automatic batching

**Shader Verification** (from existing codebase):
```glsl
// ui.vert
#version 450

layout(set = 0, binding = 0) uniform ViewProjection {
    mat4 viewProjection;
};

layout(push_constant) uniform PushConstants {
    mat4 model;
    vec4 tintColor;
    vec4 uvRect;
} pc;

layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec2 inTexCoord;

layout(location = 0) out vec2 fragTexCoord;
layout(location = 1) out vec4 fragTintColor;

void main() {
    // Apply uvRect transform to UV coordinates (identity for text)
    fragTexCoord = inTexCoord * pc.uvRect.zw + pc.uvRect.xy;
    fragTintColor = pc.tintColor;
    
    // Transform vertex: local → world → clip
    gl_Position = viewProjection * pc.model * vec4(inPosition, 0.0, 1.0);
}

// ui.frag
#version 450

layout(set = 1, binding = 0) uniform sampler2D texSampler;

layout(location = 0) in vec2 fragTexCoord;
layout(location = 1) in vec4 fragTintColor;

layout(location = 0) out vec4 outColor;

void main() {
    // Sample texture and apply tint
    vec4 texColor = texture(texSampler, fragTexCoord);
    outColor = texColor * fragTintColor;
}
```

**Text Rendering Usage**:
- **ViewProjection UBO**: Automatically bound by camera (set=0, binding=0) ✅
- **model**: Per-glyph WorldMatrix in push constants ✅
- **tintColor**: White (1,1,1,1) for default text ✅
- **uvRect**: Identity (0,0,1,1) since UVs pre-baked in geometry ✅
- **texSampler**: Font atlas texture bound at set=1, binding=0 ✅

**No shader changes required** - existing UIElement shader is perfect for text rendering!

---

## Technology Choices Summary

| Component | Choice | Rationale |
|-----------|--------|-----------|
| **Font Library** | StbTrueTypeSharp | Pure C#, simple API, proven, MIT licensed |
| **Atlas Packing** | Row-based packing | Simple, sufficient for ASCII printable at 16pt |
| **Texture Format** | R8_UNORM | 75% smaller than RGBA8, widely supported |
| **Geometry Strategy** | Shared buffer, per-glyph FirstVertex | Zero redundancy, optimal batching |
| **Positioning** | Per-glyph WorldMatrix | Leverages Element system, maintains transformability |
| **Shader** | Reuse UIElement shader | Perfect compatibility, zero new shader work |

---

## Next Steps

Proceed to **Phase 1: Design & Contracts**:
1. Generate `data-model.md` defining FontResource, GlyphInfo, FontDefinition entities
2. Create API contracts in `/contracts/` (IFontResourceManager interface, FontAtlasStructure schema)
3. Generate `quickstart.md` with "Hello World" TextElement usage example
4. Update agent context with new technology (StbTrueTypeSharp)
5. Re-evaluate Constitution Check post-design

**Phase 0 Complete** ✅
