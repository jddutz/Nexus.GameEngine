# Implementation Plan: UI Element Texture Support

**Branch**: `1-ui-texture-support` | **Date**: 2025-11-03 | **Spec**: [spec.md](./spec.md)

## Summary

Add texture rendering capabilities to UI elements, enabling developers to display images, icons, backgrounds, and textured components. The system provides flexible, extensible architecture supporting various texturing scenarios while maintaining high performance through resource sharing and batching.

**Key Changes**:
- **Uber-shader approach**: Replace `uniform_color` shader with unified `ui_element` shader supporting both solid colors and textures
- All UI elements use textured quad geometry (position + UV) with 1×1 white dummy texture by default
- Eliminates pipeline switches between colored and textured elements (~500µs saved per frame)
- Improves batching: all elements use same pipeline, can batch together
- Element base class exposes `Texture` property (defaults to dummy, set to real texture for images)
- **No TexturedElement subclass needed** - just set Element.Texture property
- Integrate with existing TextureResourceManager for texture loading and caching
- Implement frame-based integration tests with pixel sampling validation

**Technical Approach**: Phased implementation following documentation-first TDD. Create uber-shader infrastructure first, then modify Element base class to expose Texture property with dummy texture default, finally comprehensive testing with pixel sampling. Performance-optimized approach eliminates pipeline switches. No subclass needed.

**Current Status**: Specification complete, ready for Phase 0 (Research & Design Decisions).

## Technical Context

**Language/Version**: C# 9.0+ (.NET 9.0)  
**Primary Dependencies**: Silk.NET (Vulkan 1.2+), Microsoft.Extensions.DependencyInjection, StbImageSharp (texture loading)  
**Storage**: GPU texture memory (Image/ImageView/Sampler), descriptor pool for descriptor sets  
**Testing**: xUnit (unit tests), TestApp (frame-based integration tests with pixel sampling)  
**Target Platform**: Windows/Linux/macOS (Vulkan 1.2+ required)  
**Project Type**: Single project (GameEngine library with test projects)  
**Performance Goals**: 60 FPS with 500+ textured elements, efficient batching for same-texture elements  
**Constraints**: Vulkan API compliance, descriptor pool limits, texture memory budgets  
**Scale/Scope**: Support 100+ unique textures, 500+ textured elements per frame, texture atlases for optimization

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Documentation-First TDD ✅
- **Status**: PASS
- **Evidence**: spec.md created before implementation, plan.md defines phases with tests
- **Action**: Follow TDD workflow for each phase (tests before code)

### II. Component-Based Architecture ✅
- **Status**: PASS
- **Evidence**: TexturedElement will be IRuntimeComponent subclass of Element, uses templates
- **Action**: Maintain component patterns, integrate with ContentManager lifecycle

### III. Source-Generated Properties ✅
- **Status**: PASS
- **Evidence**: TexturedElement will use [ComponentProperty] for Texture, UVMin, UVMax, TintColor
- **Action**: Leverage animated property system for UV coordinate animation

### IV. Vulkan Resource Management ✅
- **Status**: PASS
- **Evidence**: Uses ITextureResourceManager, IDescriptorManager, IBufferManager for all Vulkan resources
- **Action**: Descriptor sets allocated in OnActivate(), released in OnDeactivate()
- **Texture Pattern**: TextureResourceManager handles Image/ImageView/Sampler lifecycle

### V. Explicit Approval Required ✅
- **Status**: PASS
- **Evidence**: Full design documented in spec.md, research.md will document all decisions
- **Action**: Implement exactly as specified in this plan

## Project Structure

### Documentation (this feature)

```text
specs/feature/1-ui-texture-support/
├── spec.md                      # Feature specification
├── plan.md                      # This file (implementation plan)
├── research.md                  # Design decisions and research (Phase 0)
├── tasks.md                     # Task list (created after planning)
└── checklists/
    └── requirements.md          # Specification quality validation (complete)
```

### Source Code (repository root)

**Single project structure** (GameEngine library + test projects):

```text
src/GameEngine/
├── Shaders/
│   ├── ui_textured.vert         # ⏳ NEW - Textured UI vertex shader
│   ├── ui_textured.frag         # ⏳ NEW - Textured UI fragment shader
│   ├── ui_textured.vert.spv     # ⏳ NEW - Compiled SPIR-V (via compile.bat)
│   ├── ui_textured.frag.spv     # ⏳ NEW - Compiled SPIR-V (via compile.bat)
│   └── compile.bat              # ⏳ MODIFIED - Add ui_textured shaders to build
├── EmbeddedResources/
│   └── Shaders/
│       ├── ui_textured.vert.spv # ⏳ NEW - Embedded compiled shader
│       └── ui_textured.frag.spv # ⏳ NEW - Embedded compiled shader
├── Resources/
│   └── Shaders/
│       └── ShaderDefinitions.cs # ⏳ MODIFIED - Add UITextured shader definition
├── Graphics/
│   ├── Geometry/
│   │   └── GeometryDefinitions.cs # ⏳ MODIFIED - Add TexturedQuad
│   ├── Pipelines/
│   │   └── PipelineDefinitions.cs # ⏳ MODIFIED - Add UITexturedElement
│   └── TexturedElementPushConstants.cs # ⏳ NEW - 80 bytes (mat4 + vec4)
├── GUI/
│   └── Element.cs               # ⏳ MODIFIED - Add Texture property, UV coordinates
└── Testing/
    ├── IPixelSampler.cs         # ✅ EXISTS - Interface for pixel sampling
    ├── VulkanPixelSampler.cs    # ⏳ IMPLEMENT - Currently stub, needs implementation
    └── PixelAssertions.cs       # ✅ EXISTS - Helper assertions for color matching

Tests/
├── TexturedElementTests.cs      # ⏳ NEW - Unit tests for TexturedElement
├── TexturedShaderTests.cs       # ⏳ NEW - Shader definition validation
├── TexturedQuadGeometryTests.cs # ⏳ NEW - Geometry definition validation
└── TexturedPipelineTests.cs     # ⏳ NEW - Pipeline definition validation

TestApp/
├── TestComponents/
│   ├── BasicTextureTest.cs      # ⏳ NEW - Single textured element test
│   ├── MultiTextureTest.cs      # ⏳ NEW - Multiple textures test
│   ├── SharedTextureTest.cs     # ⏳ NEW - Resource sharing test
│   ├── TintColorTest.cs         # ⏳ NEW - Tint color functionality test
│   └── UVCoordinateTest.cs      # ⏳ NEW - UV coordinate control test
└── Resources/
    └── Textures/
        ├── test_texture.png     # ⏳ NEW - Test texture asset (256x256)
        ├── test_atlas.png       # ⏳ NEW - Texture atlas for UV tests (512x512)
        └── test_icon.png        # ⏳ NEW - Small icon texture (64x64)
```

**Structure Decision**: Single project with four main areas affected:
1. **Shader System** (new shaders for textured UI)
2. **Resource System** (geometry, pipeline, shader definitions)
3. **GUI System** (TexturedElement component)
4. **Testing System** (VulkanPixelSampler implementation + integration tests)

Tests follow existing xUnit (unit) and TestApp (integration) patterns.

## Complexity Tracking

> **No violations** - All constitution checks pass. TexturedElement extends existing Element architecture. Texture support follows established patterns (ImageTextureBackground, TextElement already use textures). No complexity justification required.

---

## Implementation Phases

### Phase 0: Research & Design Decisions ⏳ PENDING

**Status**: Ready to begin research phase

**Artifacts to Create**:
- ⏳ research.md - Design decisions and rationale
- ⏳ Resolve any technical unknowns from Technical Context

**Research Topics**:
1. ✅ **Shader Architecture**: Confirmed uber-shader approach eliminates pipeline switches (~500µs saved)
2. ✅ **Descriptor Set Architecture**: Set=0 for camera UBO, set=1 for texture sampler (all elements)
3. ✅ **Push Constant Layout**: 80 bytes (64 matrix + 16 color) confirmed compatible
4. ✅ **Geometry Vertex Format**: Position2DTexCoord required for all elements (including solid colors)
5. ✅ **Dummy Texture Strategy**: 1×1 white texture for solid colors (negligible cost vs pipeline switch)
6. **Pixel Sampling Implementation**: Research Vulkan image readback patterns for VulkanPixelSampler
7. **Texture Asset Format**: Confirm PNG/embedded resource workflow via StbImageSharp
8. **Performance Validation**: Measure pipeline switch cost vs dummy texture sampling cost

**No code changes in Phase 0** - documentation-first approach required.

**Completion Criteria**: All research questions answered, design decisions documented in research.md

---

### Phase 1: VulkanPixelSampler Implementation ⏳ COMPLETED

**Goal**: Implement pixel sampling functionality required for visual validation of textured rendering.

**Priority**: CRITICAL - All integration tests depend on this functionality

**Tests to Write (Red Phase)**:
```csharp
// Tests/VulkanPixelSamplerTests.cs
- PixelSampler_IsAvailable_ReturnsTrueWhenEnabled()
- PixelSampler_SamplePixel_ReturnsBlackWhenDisabled()
- PixelSampler_SamplePixels_HandlesBatchSampling()
- PixelSampler_Enable_AllocatesStagingBuffer()
- PixelSampler_Disable_ReleasesStagingBuffer()
```

**Implementation Steps**:
1. Create staging buffer (host-visible, device-local) in `Enable()` method
2. Implement `CopySwapChainImageToStaging()` method with proper barriers
3. Implement `SamplePixel()` method with memory mapping
4. Implement `SamplePixels()` batch method
5. Add synchronization (fence) to ensure frame completion before sampling
6. Handle format conversion (BGRA ↔ RGBA) based on swap chain format
7. Implement `Disable()` to release staging buffer resources

**Files Modified**:
- `src/GameEngine/Testing/VulkanPixelSampler.cs`

**Files Created**:
- `Tests/VulkanPixelSamplerTests.cs`

**Verification**: Unit tests pass, manual TestApp test confirms pixel sampling works

---

### Phase 2: Uber-Shader Infrastructure ⏳ COMPLETED

**Goal**: Create unified GLSL uber-shader for all UI elements (replaces `uniform_color` shader), compile to SPIR-V, and define shader metadata.

**Tests to Write (Red Phase)**:
```csharp
// Tests/UIElementShaderTests.cs
- ShaderDefinitions_UIElement_Exists()
- ShaderDefinitions_UIElement_HasCorrectInputDescription() // Position2DTexCoord
- ShaderDefinitions_UIElement_HasTwoDescriptorSets() // Camera + Texture
- ShaderDefinitions_UIElement_HasPushConstantRange()
- ShaderDefinitions_UIElement_PushConstantSize_Is80Bytes()
```

**Implementation Steps**:
1. Create `src/GameEngine/Shaders/ui.vert` (uber-shader):
   - Input: `layout(location = 0) in vec2 inPos; layout(location = 1) in vec2 inTexCoord;`
   - UBO: `layout(set = 0, binding = 0) uniform ViewProjectionUBO { mat4 viewProjection; }`
   - Push constants: `layout(push_constant) uniform PushConstants { mat4 model; vec4 tintColor; }`
   - Output: `layout(location = 0) out vec2 fragTexCoord; layout(location = 1) out vec4 fragTintColor;`
   - Transform: `gl_Position = camera.viewProjection * model * vec4(inPos, 0.0, 1.0);`

2. Create `src/GameEngine/Shaders/ui.frag` (uber-shader):
   - Input: `layout(location = 0) in vec2 fragTexCoord; layout(location = 1) in vec4 fragTintColor;`
   - Texture: `layout(set = 1, binding = 0) uniform sampler2D texSampler;`
   - Output: `layout(location = 0) out vec4 outColor;`
   - **Uber logic**: `outColor = texture(texSampler, fragTexCoord) * fragTintColor;`
   - **Solid colors**: 1×1 white texture × tint color = solid color
   - **Textured**: Real texture × white (or tint) = textured element

3. Update `compile.bat` to compile ui_element shaders
4. Run `compile.bat` to generate .spv files
5. Embed compiled shaders in assembly (copy to EmbeddedResources/Shaders/)
6. Update `ShaderDefinitions.cs`:
   - Add `UIElement` shader definition (replaces both `UniformColor` and separate textured shader)
   - InputDescription: `Position2DTexCoord` (all elements now use UV coordinates)
   - DescriptorSetLayoutBindings: Camera UBO (set=0, binding=0) + Texture sampler (set=1, binding=0)
   - PushConstantRanges: 80 bytes (mat4 model + vec4 tintColor)

**Files Created**:
- `src/GameEngine/Shaders/ui.vert`
- `src/GameEngine/Shaders/ui.frag`
- `src/GameEngine/Shaders/ui.vert.spv` (compiled)
- `src/GameEngine/Shaders/ui.frag.spv` (compiled)
- `src/GameEngine/EmbeddedResources/Shaders/ui.vert.spv`
- `src/GameEngine/EmbeddedResources/Shaders/ui.frag.spv`
- `Tests/UIElementShaderTests.cs`

**Files Modified**:
- `src/GameEngine/Shaders/compile.bat`
- `src/GameEngine/Resources/Shaders/ShaderDefinitions.cs`

**Files Deprecated** (keep for backward compatibility, mark obsolete):
- `src/GameEngine/Shaders/uniform_color.vert` (replaced by ui.vert)
- `src/GameEngine/Shaders/uniform_color.frag` (replaced by ui.frag)

**Verification**: 
- Shaders compile without errors
- Tests pass
- Shader metadata correct
- **Performance test**: Measure sampling 1×1 white texture vs pipeline switch (expect ~500µs improvement)

---

### Phase 3: Geometry, Pipeline, and Dummy Texture ⏳ PENDING

**Goal**: Define textured quad geometry (used by ALL elements), unified UI pipeline, dummy white texture, and push constants structure.

**Tests to Write (Red Phase)**:
```csharp
// Tests/TexturedQuadGeometryTests.cs
- GeometryDefinitions_TexturedQuad_HasCorrectVertexCount()
- GeometryDefinitions_TexturedQuad_HasCorrectIndexCount()
- GeometryDefinitions_TexturedQuad_VertexFormatIsPosition2DTexCoord()
- GeometryDefinitions_TexturedQuad_UVCoordinatesCover0To1()

// Tests/UIElementPipelineTests.cs
- PipelineDefinitions_UIElement_Exists()
- PipelineDefinitions_UIElement_UsesUIElementShader()
- PipelineDefinitions_UIElement_EnablesAlphaBlending()
- PipelineDefinitions_UIElement_HasTwoDescriptorSetLayouts()

// Tests/DummyTextureTests.cs
- TextureDefinitions_WhiteDummy_Exists()
- TextureDefinitions_WhiteDummy_Is1x1()
- TextureDefinitions_WhiteDummy_IsWhiteColor()
```

**Implementation Steps**:
1. Add `TexturedQuad` to `GeometryDefinitions.cs`:
   - 4 vertices: `[(−1,−1,0,0), (1,−1,1,0), (1,1,1,1), (−1,1,0,1)]` (pos+uv)
   - 6 indices: `[0,1,2, 0,2,3]` (two triangles)
   - Vertex format: Position2DTexCoord (vec2 pos, vec2 uv = 16 bytes per vertex)
   - **NOTE**: Replaces UniformColorQuad for ALL UI elements

2. Create dummy white texture:
   - Create `src/GameEngine/EmbeddedResources/Textures/dummy_white.png` (1×1, RGBA=255,255,255,255)
   - Add to `TextureDefinitions.cs`: `WhiteDummy` texture definition
   - This texture shared by all solid color elements (eliminates pipeline switches)

3. Create `UIElementPushConstants.cs` (replaces UniformColorPushConstants):
   - `Matrix4X4<float> Model` (64 bytes)
   - `Vector4D<float> TintColor` (16 bytes)
   - `FromModelAndColor()` static factory method
   - `[StructLayout(LayoutKind.Sequential)]` attribute

4. Update `UIElement` in `PipelineDefinitions.cs`:
   - Shader: `ShaderDefinitions.UIElement` (uber-shader)
   - Topology: TriangleList
   - Blending: Alpha blending enabled (src_alpha, one_minus_src_alpha)
   - RenderPass: UI pass
   - DescriptorSetLayouts: Camera UBO layout (set=0) + Texture sampler layout (set=1)
   - PushConstantRange: 80 bytes for UIElementPushConstants

**Files Created**:
- `src/GameEngine/EmbeddedResources/Textures/dummy_white.png`
- `src/GameEngine/Graphics/UIElementPushConstants.cs`
- `Tests/TexturedQuadGeometryTests.cs`
- `Tests/UIElementPipelineTests.cs`
- `Tests/DummyTextureTests.cs`

**Files Modified**:
- `src/GameEngine/Graphics/Geometry/GeometryDefinitions.cs`
- `src/GameEngine/Graphics/Pipelines/PipelineDefinitions.cs`
- `src/GameEngine/Resources/Textures/TextureDefinitions.cs` (add WhiteDummy)

**Files Deprecated**:
- `GeometryDefinitions.UniformColorQuad` (replaced by TexturedQuad)
- `UniformColorPushConstants.cs` (replaced by UIElementPushConstants)

**Verification**: Tests pass, geometry/pipeline/texture definitions valid, dummy texture loads correctly

---

### Phase 4: Modify Element Base Class with Texture Property ⏳ PENDING

**Goal**: Update Element base class to use uber-shader with Texture property (defaults to dummy white texture). No subclass needed.

**Tests to Write (Red Phase)**:
```csharp
// Tests/ElementTextureTests.cs
- Element_OnActivate_LoadsDummyTextureByDefault()
- Element_OnActivate_CreatesTextureDescriptorSet()
- Element_UsesTexturedQuadGeometry()
- Element_UsesUIElementPipeline()
- Element_WithCustomTexture_LoadsSpecifiedTexture()
- Element_WithCustomTexture_UpdatesDescriptorSet()
- Element_OnDeactivate_ReleasesTexture()
- Element_TintColor_DefaultsToWhite()
- Element_UVBounds_DefaultToFullTexture()
- Element_TextureChange_UpdatesDescriptorSet()
```

**Implementation Steps**:

1. **Modify Element base class** (`src/GameEngine/GUI/Element.cs`):
   
   a. **Add constructor parameter**: `IDescriptorManager descriptorManager`
   
   b. **Add component properties**:
      - `[ComponentProperty] TextureDefinition? _texture = TextureDefinitions.WhiteDummy`
      - `[ComponentProperty] Vector2D<float> _uvMin = new(0, 0)`
      - `[ComponentProperty] Vector2D<float> _uvMax = new(1, 1)`
      - Keep existing: `[ComponentProperty] Vector4D<float> _tintColor = Colors.White`
   
   c. **Add private fields**: 
      - `TextureResource? _textureResource`
      - `DescriptorSet? _textureDescriptorSet`
   
   d. **Update `OnActivate()`**:
      - Create geometry using `TexturedQuad` (position + UV)
      - Load texture from `_texture` property: `_textureResource = ResourceManager.Textures.GetOrCreate(_texture)`
      - Create descriptor set layout for texture sampler (set=1, binding=0)
      - Allocate descriptor set from DescriptorManager
      - Update descriptor set with loaded texture
   
   e. **Update `OnDeactivate()`**:
      - Release texture resource: `ResourceManager.Textures.Release(_texture)`
      - Descriptor set automatically released by pool reset
   
   f. **Update `Pipeline` property**:
      - Return `PipelineManager.GetOrCreate(PipelineDefinitions.UIElement)` (uber-shader pipeline)
   
   g. **Update `GetGeometryDefinition()`**:
      - Return `GeometryDefinitions.TexturedQuad` (all elements use position + UV)
   
   h. **Update `GetDrawCommands()`**:
      - Create `UIElementPushConstants` (replaces UniformColorPushConstants)
      - Include texture descriptor set in DrawCommand
   
   i. **Add property change handler**:
      - `partial void OnTextureChanged(TextureDefinition? oldValue)` - reload texture and update descriptor set

2. **Update ElementTemplate**:
   - Add `Texture` property (optional, defaults to dummy)
   - Keep existing properties: Position, Size, AnchorPoint, TintColor, ZIndex
   - Add UV properties: `UVMin`, `UVMax`

**Usage Examples**:
```csharp
// Solid color element (default dummy texture)
var redBox = new ElementTemplate
{
    Position = new(100, 100, 0),
    Size = new(200, 100),
    TintColor = Colors.Red  // White texture * red = red
};

// Textured element (specify texture)
var imageBox = new ElementTemplate
{
    Position = new(300, 100, 0),
    Size = new(200, 100),
    Texture = TextureDefinitions.MyImage
};

// Textured with tint
var tintedImage = new ElementTemplate
{
    Position = new(500, 100, 0),
    Size = new(200, 100),
    Texture = TextureDefinitions.MyImage,
    TintColor = new Vector4D<float>(1, 0.5f, 0.5f, 1) // Reddish tint
};

// UV coordinate control (texture atlas)
var iconFromAtlas = new ElementTemplate
{
    Position = new(700, 100, 0),
    Size = new(64, 64),
    Texture = TextureDefinitions.IconAtlas,
    UVMin = new(0.0f, 0.0f),
    UVMax = new(0.25f, 0.25f)  // Top-left quarter
};
```

**Files Modified**:
- `src/GameEngine/GUI/Element.cs` (BREAKING CHANGE: requires IDescriptorManager, uses uber-shader, adds Texture property)
- `src/GameEngine/GUI/ElementTemplate.cs` (add Texture, UVMin, UVMax properties)

**Files Created**:
- `Tests/ElementTextureTests.cs`

**Files NOT Created**:
- ~~`TexturedElement.cs`~~ - Not needed, Element handles both cases
- ~~`TexturedElementTemplate.cs`~~ - Not needed, ElementTemplate handles both cases

**Migration Impact**:
- **Breaking**: All Element subclasses now use textured quad geometry and uber-shader
- **Breaking**: Element constructor requires IDescriptorManager
- **Benefit**: Simpler API - just set `Texture` property instead of creating subclass
- **Benefit**: ColoredRectTest and other tests automatically benefit from uber-shader
- **Performance**: Eliminates pipeline switches between colored and textured elements

**Verification**: 
- Unit tests pass
- Existing Element-based tests still pass (ColoredRectTest, etc.)
- Can set Texture property to switch between dummy and real textures
- Animated property system works for texture changes

---

### Phase 5: Test Assets and Integration Tests ⏳ PENDING

**Goal**: Create test texture assets and frame-based integration tests with pixel sampling. All tests use Element base class with Texture property.

**Implementation Steps**:

1. **Create Test Assets**:
   - Create `TestApp/Resources/Textures/test_texture.png`: 256x256, solid red (#FF0000)
   - Create `TestApp/Resources/Textures/test_atlas.png`: 512x512, 4 quadrants (red, green, blue, yellow)
   - Create `TestApp/Resources/Textures/test_icon.png`: 64x64, white square with black border
   - Update TestResources.cs with texture definitions

2. **Colored Element Test** (`TestApp/TestComponents/ColoredElementTest.cs`):
   - Create Element with red tint color (default dummy texture)
   - Position: (100, 100), Size: (200, 200)
   - Expected: Red square at specified location
   - Validation: Sample center pixel, expect red color (dummy texture * red tint)

3. **Basic Texture Test** (`TestApp/TestComponents/BasicTextureTest.cs`):
   - Create Element with Texture=test_texture.png (real texture)
   - Position: (300, 100), Size: (200, 200)
   - Expected: Red textured square
   - Validation: Sample center pixel, expect red color

4. **Multi-Texture Test** (`TestApp/TestComponents/MultiTextureTest.cs`):
   - Create 3 Elements with different Texture properties
   - Position side-by-side
   - Expected: All three textures visible
   - Validation: Sample center of each element, expect different colors

5. **Shared Texture Test** (`TestApp/TestComponents/SharedTextureTest.cs`):
   - Create 10 Elements with same Texture property
   - Arrange in grid layout
   - Expected: All elements render correctly, texture resource shared (via logs)
   - Validation: Verify resource manager logs show single texture load

6. **Mixed UI Test** (`TestApp/TestComponents/MixedUITest.cs`):
   - Create 5 Elements: 3 with solid colors (dummy texture), 2 with real textures
   - Arrange in grid
   - Expected: All render correctly without pipeline switches
   - Validation: Sample pixels, verify all elements render correctly

7. **Tint Color Test** (`TestApp/TestComponents/TintColorTest.cs`):
   - Create Element with Texture=white_texture.png, TintColor=Red
   - Expected: Red-tinted texture
   - Validation: Sample pixel, expect red color (white * red = red)

8. **UV Coordinate Test** (`TestApp/TestComponents/UVCoordinateTest.cs`):
   - Create Element with Texture=test_atlas.png
   - Set UVMin=(0,0), UVMax=(0.5, 0.5) to show top-left quadrant (red)
   - Expected: Only red quadrant visible
   - Validation: Sample pixel, expect red (not other colors)

9. **Dynamic Texture Change Test** (`TestApp/TestComponents/DynamicTextureChangeTest.cs`):
   - Create Element with Texture=test_texture.png (red)
   - After 60 frames, change Texture to test_icon.png (white with border)
   - Expected: Visual change from red square to icon
   - Validation: Sample pixel before and after, verify color changes

**Files Created**:
- `TestApp/Resources/Textures/test_texture.png`
- `TestApp/Resources/Textures/test_atlas.png`
- `TestApp/Resources/Textures/test_icon.png`
- `TestApp/Resources/Textures/white_texture.png`
- `TestApp/TestComponents/UITexture/ColoredElementTest.cs`
- `TestApp/TestComponents/UITexture/BasicTextureTest.cs`
- `TestApp/TestComponents/UITexture/MultiTextureTest.cs`
- `TestApp/TestComponents/UITexture/SharedTextureTest.cs`
- `TestApp/TestComponents/UITexture/MixedUITest.cs`
- `TestApp/TestComponents/UITexture/TintColorTest.cs`
- `TestApp/TestComponents/UITexture/UVCoordinateTest.cs`
- `TestApp/TestComponents/UITexture/DynamicTextureChangeTest.cs`

**Files Modified**:
- `TestApp/TestResources.cs` (add texture definitions)

**Key Testing Notes**:
- All tests use Element base class with Texture property (no TexturedElement subclass)
- Colored elements test dummy texture path: `Texture=null` or default WhiteDummy
- Textured elements test real texture path: `Texture=TextureDefinitions.MyTexture`
- Mixed UI test validates no pipeline switches between colored and textured elements
- Dynamic change test validates animated property system handles texture changes

**Verification**: All integration tests pass with pixel sampling validation

---

### Phase 6: Performance Profiling and Optimization ⏳ PENDING

**Goal**: Verify batching effectiveness and performance targets.

**Tests to Write (Red Phase)**:
```csharp
// Tests/TexturedBatchingTests.cs
- Batching_SameTexture_MinimizesDrawCalls()
- Batching_DifferentTextures_GroupsByTexture()
- Batching_RespectZIndex_MaintainsRenderOrder()
```

**Implementation Steps**:
1. Create stress test with 100+ textured elements
2. Profile draw call count (expect batching for same-texture elements)
3. Measure frame time (expect <16ms for 500 elements)
4. Verify descriptor pool doesn't exhaust (monitor DescriptorManager logs)
5. Add logging to DefaultBatchStrategy to confirm texture-based grouping
6. Optimize if performance targets not met

**Files Created**:
- `TestApp/TestComponents/TexturedStressTest.cs`
- `Tests/TexturedBatchingTests.cs`

**Verification**: 
- 100 elements with same texture → 1 draw call
- 500 elements total → 60 FPS maintained
- Descriptor pool has sufficient capacity

---

### Phase 7: Shader Consolidation and Cleanup ⏳ PENDING

**Goal**: Remove obsolete shaders now that uber-shader replaces multiple specialized shaders. Consolidate all UI rendering to use `ui_element` uber-shader.

**Rationale**:
- **UniformColorQuad**: Replaced by uber-shader with dummy texture (eliminates pipeline switches)
- **ImageTexture**: Replaced by uber-shader (same functionality, unified pipeline)
- **Per-vertex color shader**: Can be replaced by uber-shader with per-vertex UVs + gradient texture
- **Gradient shaders**: Can be replaced by uber-shader with gradient textures (future enhancement)

**Analysis of Existing Shaders**:

| Shader | Current Usage | Replacement Strategy | Keep? |
|--------|--------------|---------------------|-------|
| `uniform_color` | Element base class | `ui_element` uber-shader + dummy texture | ❌ Remove |
| `image_texture` | ImageTextureBackground, TextElement | `ui_element` uber-shader | ❌ Remove |
| `per_vertex_color` | ColoredGeometry | `ui_element` uber-shader + vertex color texture | ❌ Remove |
| `linear_gradient` | LinearGradientBackground | Keep for now (specialized UBO usage) | ✅ Keep |
| `radial_gradient` | RadialGradientBackground | Keep for now (specialized UBO usage) | ✅ Keep |
| `biaxial_gradient` | BiaxialGradientBackground | Keep for now (specialized UBO usage) | ✅ Keep |
| `shader.vert/frag` | Legacy ColoredGeometry | Unused? Verify then remove | ⚠️ Verify |

**Implementation Steps**:

1. **Verify gradient shader usage**:
   - Search codebase for references to LinearGradient, RadialGradient, BiaxialGradient shaders
   - Confirm these are actively used by background components
   - Decision: Keep gradient shaders for now (specialized UBO structure different from uber-shader)
   - Future: Could consolidate to uber-shader + procedural textures, but out of scope

2. **Remove uniform_color shader**:
   - Delete `src/GameEngine/Shaders/uniform_color.vert`
   - Delete `src/GameEngine/Shaders/uniform_color.frag`
   - Delete `src/GameEngine/Shaders/uniform_color.vert.spv`
   - Delete `src/GameEngine/Shaders/uniform_color.frag.spv`
   - Delete `src/GameEngine/EmbeddedResources/Shaders/uniform_color.vert.spv`
   - Delete `src/GameEngine/EmbeddedResources/Shaders/uniform_color.frag.spv`
   - Remove `ShaderDefinitions.UniformColorQuad` from ShaderDefinitions.cs
   - Remove `UniformColorPushConstants.cs` (replaced by UIElementPushConstants)

3. **Remove image_texture shader**:
   - Delete `src/GameEngine/Shaders/image_texture.vert`
   - Delete `src/GameEngine/Shaders/image_texture.frag`
   - Delete `src/GameEngine/Shaders/image_texture.vert.spv`
   - Delete `src/GameEngine/Shaders/image_texture.frag.spv`
   - Delete `src/GameEngine/EmbeddedResources/Shaders/image_texture.vert.spv`
   - Delete `src/GameEngine/EmbeddedResources/Shaders/image_texture.frag.spv`
   - Update `ShaderDefinitions.ImageTexture` to use `ui_element` shader (or remove if unused)
   - Update ImageTextureBackground and TextElement to use uber-shader pipeline

4. **Verify and remove per_vertex_color shader**:
   - Search for usage of `ShaderDefinitions.ColoredGeometry`
   - If unused: Delete shader files and remove from ShaderDefinitions.cs
   - If used: Migrate to uber-shader with appropriate texture/color setup
   - Delete `src/GameEngine/Shaders/per_vertex_color.vert/frag` (and .spv files)

5. **Verify and remove shader.vert/frag**:
   - Search for usage (likely legacy/unused)
   - If unused: Delete shader files
   - Delete `src/GameEngine/Shaders/shader.vert/frag` (and .spv files)

6. **Update compile.bat**:
   - Remove references to deleted shaders
   - Keep only: `ui_element`, gradient shaders

7. **Update all affected components**:
   - Element: Already using uber-shader (Phase 4)
   - ImageTextureBackground: Update to use UIElement pipeline
   - TextElement: Update to use UIElement pipeline
   - Verify no other components reference removed shaders

**Files Deleted**:
- `src/GameEngine/Shaders/uniform_color.vert`
- `src/GameEngine/Shaders/uniform_color.frag`
- `src/GameEngine/Shaders/image_texture.vert`
- `src/GameEngine/Shaders/image_texture.frag`
- `src/GameEngine/Shaders/per_vertex_color.vert` (if unused)
- `src/GameEngine/Shaders/per_vertex_color.frag` (if unused)
- `src/GameEngine/Shaders/shader.vert` (if unused)
- `src/GameEngine/Shaders/shader.frag` (if unused)
- All corresponding .spv files (compiled and embedded)
- `src/GameEngine/Graphics/UniformColorPushConstants.cs`
- `src/GameEngine/Graphics/ImageTexturePushConstants.cs` (consolidated into UIElementPushConstants)

**Files Modified**:
- `src/GameEngine/Shaders/compile.bat` (remove deleted shaders)
- `src/GameEngine/Resources/Shaders/ShaderDefinitions.cs` (remove obsolete definitions)
- `src/GameEngine/GUI/BackgroundLayers/ImageTextureBackground.cs` (use uber-shader)
- `src/GameEngine/GUI/TextElement.cs` (use uber-shader)

**Verification**: 
- Build succeeds with no errors
- All tests pass (especially ImageTextureBackground and TextElement tests)
- Grep for removed shader names returns no results
- Pipeline count reduced (confirm via logs)

---

### Phase 8: Documentation and Examples ⏳ PENDING

**Goal**: Update documentation with texture support examples and usage patterns.

**Implementation Steps**:
1. Update `.docs/Project Structure.md`:
   - Add TexturedElement to GUI components section
   - Document uber-shader consolidation
   - List remaining shaders (ui_element + gradients)
   - Add test assets location

2. Update `README.md`:
   - Add example of creating textured UI elements
   - Show texture atlas usage pattern
   - Document tint color and UV coordinate control
   - Explain uber-shader performance benefits

3. Create tutorial example in TestApp:
   - Simple image display component
   - Button with textured background
   - Icon with transparency

4. Update `src/GameEngine/Testing/README.md`:
   - Add pixel sampling examples for textured elements
   - Document color tolerance for anti-aliased edges

5. Create shader architecture documentation:
   - Explain uber-shader approach
   - Document when to use ui_element vs gradient shaders
   - Performance implications of shader consolidation

**Files Modified**:
- `.docs/Project Structure.md`
- `.docs/Vulkan Architecture.md` (add shader consolidation section)
- `README.md`
- `src/GameEngine/Testing/README.md`

**Files Created**:
- `TestApp/Examples/TexturedImageExample.cs`
- `TestApp/Examples/TexturedButtonExample.cs`
- `.docs/Shader Architecture.md` (optional, comprehensive shader guide)

**Verification**: Documentation accurate, examples compile and run

---

## Testing Strategy

### Unit Tests (Tests Project)
- Mock dependencies (IDescriptorManager, IResourceManager, etc.)
- Test individual method behaviors (descriptor set lifecycle, geometry definitions)
- Fast execution (<1ms per test)
- Run after each phase: `dotnet test Tests/Tests.csproj`

### Integration Tests (TestApp Project)
- Frame-based rendering tests with pixel sampling
- Visual validation of texture rendering correctness
- Run after Phases 4, 5, and 6 (texture rendering implemented)
- Command: `dotnet run --project TestApp/TestApp.csproj --configuration Debug`

### Test Filtering for Debugging
- Use `--filter` argument for focused testing during debugging
- Example: `dotnet run --project TestApp/TestApp.csproj -- --filter=BasicTexture`
- Helps isolate failing tests without running full suite

### Acceptance Criteria
- [ ] All unit tests pass (Tests project)
- [ ] All integration tests pass (TestApp)
- [ ] BasicTextureTest renders red square correctly (pixel sampling validates)
- [ ] MultiTextureTest shows all textures without conflicts
- [ ] SharedTextureTest confirms resource sharing (logs show single texture load)
- [ ] TintColorTest shows correct color multiplication
- [ ] UVCoordinateTest shows correct texture region
- [ ] 100+ same-texture elements render at 60 FPS
- [ ] Build produces no errors or warnings
- [ ] Documentation updated with examples

---

## Rollback Plan

If critical issues discovered:

**After Phase 1**: Can rollback - pixel sampling is testing-only feature  
**After Phase 2**: Can rollback - shaders not yet used by any components  
**After Phase 3**: Can rollback - definitions exist but not referenced  
**After Phase 4**: **Point of No Return** - TexturedElement exists, tests may depend on it  
**After Phase 5**: Must fix forward - integration tests committed

**Mitigation**: Test incrementally after each phase. Verify shaders compile before proceeding to Phase 4.

---

## Performance Validation

**Metrics to Measure**:
1. Draw call count with same-texture elements (expect: 1 draw call per texture)
2. Frame time with 500 textured elements (expect: <16ms for 60 FPS)
3. Texture memory usage (monitor via Vulkan validation layers)
4. Descriptor pool usage (ensure sufficient capacity)
5. Texture load time (expect: <50ms per texture on first load)

**How to Measure**:
- Add logging in Renderer to count draw calls per frame
- Use Vulkan timestamp queries for GPU timing
- Profile with RenderDoc or Nsight Graphics for detailed analysis
- Monitor descriptor pool allocation counts in DescriptorManager logs

**Performance Targets**:
- 100 elements, same texture: 1 draw call
- 500 elements, mixed textures: <10 draw calls (with good batching)
- Frame time: <10ms for 500 elements (leave headroom for other systems)
- Texture load: <50ms per 1024x1024 texture
- Memory: <100MB for 50 UI textures (typical)

---

## Documentation Updates Required

After implementation complete, update:
- [x] `.docs/Project Structure.md` - Add TexturedElement, shader files, test assets
- [x] `README.md` - Add texture usage examples
- [x] `src/GameEngine/Testing/README.md` - Add pixel sampling examples for textures
- [ ] Create `.docs/Texture System.md` - Comprehensive guide to texture system (optional)

---

## Success Criteria Summary

✅ All 8 phases complete  
✅ VulkanPixelSampler implemented and functional  
✅ Uber-shader (`ui_element`) replaces multiple specialized shaders  
✅ Element base class uses uber-shader with dummy texture  
✅ TexturedElement component functional with real textures  
✅ All unit tests pass (Tests project)  
✅ All integration tests pass (TestApp) with pixel sampling validation  
✅ Performance targets met (60 FPS with 500 elements)  
✅ Pipeline switches eliminated (~500µs saved per frame)  
✅ Batching effective (draw call reduction confirmed)  
✅ Descriptor pool capacity sufficient  
✅ Obsolete shaders removed (uniform_color, image_texture, etc.)  
✅ ImageTextureBackground and TextElement migrated to uber-shader  
✅ Documentation updated with uber-shader architecture and examples  
✅ Build succeeds with no errors/warnings  

---

## Phase Dependencies

```
Phase 0 (Research)
    ↓
Phase 1 (Pixel Sampling) ← CRITICAL for testing
    ↓
Phase 2 (Uber-Shader) ← Foundation
    ↓
Phase 3 (Geometry + Pipeline + Dummy Texture) ← Definitions
    ↓
Phase 4 (Element Modification + TexturedElement) ← Core implementation
    ↓
Phase 5 (Integration Tests) ← Validation
    ↓
Phase 6 (Performance) ← Optimization
    ↓
Phase 7 (Shader Cleanup) ← Remove obsolete shaders
    ↓
Phase 8 (Documentation) ← Finalization
```

**Critical Path**: Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 7

**Parallel Work**: 
- Phase 6 (performance profiling) can overlap with Phase 5 (tests provide profiling scenarios)
- Phase 7 (cleanup) must come after Phase 5 (ensures existing components work with uber-shader)

---

## Next Steps

1. **Create research.md** - Document design decisions for Phase 0
2. **Create tasks.md** - Break down phases into granular tasks
3. **Begin Phase 1** - Implement VulkanPixelSampler (critical dependency)
4. **Follow TDD workflow** - Write tests (Red) → Implement (Green) → Refactor
5. **Test incrementally** - Verify after each phase before proceeding
