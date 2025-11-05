# Tasks: UI Element Texture Support

**Feature Branch**: `1-ui-texture-support`  
**Input**: Design documents from `specs/feature/1-ui-texture-support/`  
**Prerequisites**: ✅ plan.md, ✅ spec.md, ✅ research.md

**Tests**: This feature includes comprehensive unit and integration tests (TDD approach required per constitution).

**Organization**: Tasks are grouped by implementation phases from plan.md. User stories from spec.md are mapped to phases 2-4.

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **Checkbox**: `- [ ]` for markdown task tracking
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Exact file paths included in all descriptions

---

## Phase 1: VulkanPixelSampler Implementation (Testing Infrastructure)

**Purpose**: Implement pixel sampling functionality required for visual validation of all integration tests

**Priority**: CRITICAL - All integration tests depend on this functionality

### Tests for Phase 1

- [x] T001 [P] Create unit tests in `Tests/VulkanPixelSamplerTests.cs`
- [x] T002 [P] Add test: PixelSampler_IsAvailable_ReturnsTrueWhenEnabled in `Tests/VulkanPixelSamplerTests.cs`
- [x] T003 [P] Add test: PixelSampler_SamplePixel_ReturnsBlackWhenDisabled in `Tests/VulkanPixelSamplerTests.cs`
- [x] T004 [P] Add test: PixelSampler_SamplePixels_HandlesBatchSampling in `Tests/VulkanPixelSamplerTests.cs`
- [x] T005 [P] Add test: PixelSampler_Enable_AllocatesStagingBuffer in `Tests/VulkanPixelSamplerTests.cs`
- [x] T006 [P] Add test: PixelSampler_Disable_ReleasesStagingBuffer in `Tests/VulkanPixelSamplerTests.cs`

### Implementation for Phase 1

- [x] T007 Implement staging buffer allocation in VulkanPixelSampler.Enable() method in `src/GameEngine/Testing/VulkanPixelSampler.cs`
- [x] T008 Implement CopySwapChainImageToStaging() method with proper barriers in `src/GameEngine/Testing/VulkanPixelSampler.cs`
- [x] T009 Implement SamplePixel() method with memory mapping in `src/GameEngine/Testing/VulkanPixelSampler.cs`
- [x] T010 Implement SamplePixels() batch method in `src/GameEngine/Testing/VulkanPixelSampler.cs`
- [x] T011 Add synchronization fence for frame completion in `src/GameEngine/Testing/VulkanPixelSampler.cs`
- [x] T012 Implement format conversion (BGRA ↔ RGBA) based on swap chain format in `src/GameEngine/Testing/VulkanPixelSampler.cs`
- [x] T013 Implement Disable() method to release staging buffer resources in `src/GameEngine/Testing/VulkanPixelSampler.cs`
- [x] T014 Run unit tests and verify all pass: `dotnet test Tests/Tests.csproj --filter VulkanPixelSampler`

**Checkpoint**: Pixel sampling functional and tested - ready for visual validation tests

---

## Phase 2: Uber-Shader Infrastructure

**Purpose**: Create unified GLSL uber-shader for all UI elements (replaces `uniform_color` shader), compile to SPIR-V, and define shader metadata

**User Story Mapping**: Foundation for US1 (Basic Textured UI Element)

### Tests for Phase 2

- [x] T015 [P] Create shader definition tests in `Tests/UIElementShaderTests.cs`
- [x] T016 [P] Add test: ShaderDefinitions_UIElement_Exists in `Tests/UIElementShaderTests.cs`
- [x] T017 [P] Add test: ShaderDefinitions_UIElement_HasCorrectInputDescription in `Tests/UIElementShaderTests.cs`
- [x] T018 [P] Add test: ShaderDefinitions_UIElement_HasTwoDescriptorSets in `Tests/UIElementShaderTests.cs`
- [x] T019 [P] Add test: ShaderDefinitions_UIElement_HasPushConstantRange in `Tests/UIElementShaderTests.cs`
- [x] T020 [P] Add test: ShaderDefinitions_UIElement_PushConstantSize_Is80Bytes in `Tests/UIElementShaderTests.cs`

### Implementation for Phase 2

- [x] T021 [P] Create ui_element.vert shader with Position2DTexCoord input in `src/GameEngine/Shaders/ui_element.vert`
- [x] T022 [P] Create ui_element.frag shader with uber-shader logic (texture * tintColor) in `src/GameEngine/Shaders/ui_element.frag`
- [x] T023 Update compile.bat to include ui_element shaders in `src/GameEngine/Shaders/compile.bat`
- [x] T024 Run compile.bat to generate SPIR-V files: `src/GameEngine/Shaders/compile.bat`
- [x] T025 [P] Copy compiled shaders to embedded resources in `src/GameEngine/EmbeddedResources/Shaders/ui_element.vert.spv`
- [x] T026 [P] Copy compiled shaders to embedded resources in `src/GameEngine/EmbeddedResources/Shaders/ui_element.frag.spv`
- [x] T027 Add UIElement shader definition to `src/GameEngine/Resources/Shaders/ShaderDefinitions.cs`
- [x] T028 Mark uniform_color shader as obsolete in `src/GameEngine/Resources/Shaders/ShaderDefinitions.cs`
- [x] T029 Run unit tests and verify shader metadata: `dotnet test Tests/Tests.csproj --filter UIElementShader`
- [x] T030 Measure performance: sampling 1×1 white texture vs pipeline switch (document in research.md)

**Checkpoint**: Uber-shader compiles, loads, and metadata validated

---

## Phase 3: Geometry, Pipeline, and Dummy Texture

**Purpose**: Define textured quad geometry (used by ALL elements), unified UI pipeline, dummy white texture, and push constants structure

**User Story Mapping**: Foundation for US1 (Basic Textured UI Element)

### Tests for Phase 3

- [x] T031 [P] Create geometry tests in `Tests/TexturedQuadGeometryTests.cs`
- [x] T032 [P] Add test: GeometryDefinitions_TexturedQuad_HasCorrectVertexCount in `Tests/TexturedQuadGeometryTests.cs`
- [x] T033 [P] Add test: GeometryDefinitions_TexturedQuad_HasCorrectIndexCount in `Tests/TexturedQuadGeometryTests.cs`
- [x] T034 [P] Add test: GeometryDefinitions_TexturedQuad_VertexFormatIsPosition2DTexCoord in `Tests/TexturedQuadGeometryTests.cs`
- [x] T035 [P] Add test: GeometryDefinitions_TexturedQuad_UVCoordinatesCover0To1 in `Tests/TexturedQuadGeometryTests.cs`
- [x] T036 [P] Create pipeline tests in `Tests/UIElementPipelineTests.cs`
- [x] T037 [P] Add test: PipelineDefinitions_UIElement_Exists in `Tests/UIElementPipelineTests.cs`
- [x] T038 [P] Add test: PipelineDefinitions_UIElement_UsesUIElementShader in `Tests/UIElementPipelineTests.cs`
- [x] T039 [P] Add test: PipelineDefinitions_UIElement_EnablesAlphaBlending in `Tests/UIElementPipelineTests.cs`
- [x] T040 [P] Add test: PipelineDefinitions_UIElement_HasTwoDescriptorSetLayouts in `Tests/UIElementPipelineTests.cs`
- [x] T041 [P] Create dummy texture tests in `Tests/DummyTextureTests.cs`
- [x] T042 [P] Add test: TextureDefinitions_WhiteDummy_Exists in `Tests/DummyTextureTests.cs`
- [x] T043 [P] Add test: TextureDefinitions_WhiteDummy_Is1x1 in `Tests/DummyTextureTests.cs`
- [x] T044 [P] Add test: TextureDefinitions_WhiteDummy_IsWhiteColor in `Tests/DummyTextureTests.cs`

### Implementation for Phase 3

- [x] T045 Create 1×1 white dummy texture PNG (RGBA=255,255,255,255) in `src/GameEngine/EmbeddedResources/Textures/dummy_white.png`
- [x] T046 Add WhiteDummy texture definition to `src/GameEngine/Resources/Textures/TextureDefinitions.cs`
- [x] T047 Add TexturedQuad geometry definition (4 vertices, 6 indices, Position2DTexCoord format) to `src/GameEngine/Graphics/Geometry/GeometryDefinitions.cs`
- [x] T048 Mark UniformColorQuad as obsolete in `src/GameEngine/Graphics/Geometry/GeometryDefinitions.cs`
- [x] T049 Create UIElementPushConstants struct (mat4 Model + vec4 TintColor = 80 bytes) in `src/GameEngine/Graphics/UIElementPushConstants.cs`
- [x] T050 Update UIElement pipeline definition to use uber-shader in `src/GameEngine/Graphics/Pipelines/PipelineDefinitions.cs`
- [x] T051 Mark UniformColorPushConstants as obsolete (document replacement with UIElementPushConstants)
- [x] T052 Run unit tests for geometry: `dotnet test Tests/Tests.csproj --filter TexturedQuadGeometry`
- [x] T053 Run unit tests for pipeline: `dotnet test Tests/Tests.csproj --filter UIElementPipeline`
- [x] T054 Run unit tests for dummy texture: `dotnet test Tests/Tests.csproj --filter DummyTexture`

**Checkpoint**: ✅ Geometry, pipeline, and dummy texture definitions validated

---

## Phase 4: Modify Element Base Class with Texture Property

**Purpose**: Update Element base class to use uber-shader with Texture property (defaults to dummy white texture). No subclass needed.

**User Story Mapping**: 
- **US1 (P1)**: Basic Textured UI Element - core implementation
- **US2 (P2)**: UV Coordinate Control - property exposure
- **US3 (P1)**: Performance Optimization - unified pipeline for batching

### Tests for Phase 4 [US1]

- [x] T055 [P] [US1] Create Element texture tests in `Tests/ElementTextureTests.cs`
- [x] T056 [P] [US1] Add test: Element_OnActivate_LoadsDummyTextureByDefault in `Tests/ElementTextureTests.cs`
- [x] T057 [P] [US1] Add test: Element_OnActivate_CreatesTextureDescriptorSet in `Tests/ElementTextureTests.cs`
- [x] T058 [P] [US1] Add test: Element_UsesTexturedQuadGeometry in `Tests/ElementTextureTests.cs`
- [x] T059 [P] [US1] Add test: Element_UsesUIElementPipeline in `Tests/ElementTextureTests.cs`
- [x] T060 [P] [US1] Add test: Element_WithCustomTexture_LoadsSpecifiedTexture in `Tests/ElementTextureTests.cs`
- [x] T061 [P] [US1] Add test: Element_WithCustomTexture_UpdatesDescriptorSet in `Tests/ElementTextureTests.cs`
- [x] T062 [P] [US1] Add test: Element_OnDeactivate_ReleasesTexture in `Tests/ElementTextureTests.cs`
- [x] T063 [P] [US1] Add test: Element_TintColor_DefaultsToWhite in `Tests/ElementTextureTests.cs`
- [x] T064 [P] [US1] Add test: Element_UVBounds_DefaultToFullTexture in `Tests/ElementTextureTests.cs`
- [x] T065 [P] [US1] Add test: Element_TextureChange_UpdatesDescriptorSet in `Tests/ElementTextureTests.cs`

### Implementation for Phase 4 [US1] [US2]

- [x] T066 [US1] Add IDescriptorManager parameter to Element constructor in `src/GameEngine/GUI/Element.cs`
- [x] T067 [US1] [US2] Add component properties to Element: _texture (default WhiteDummy), _uvMin, _uvMax, _tintColor in `src/GameEngine/GUI/Element.cs`
- [x] T068 [US1] Add private fields: _textureResource, _textureDescriptorSet in `src/GameEngine/GUI/Element.cs`
- [x] T069 [US1] Update Element.OnActivate() to create TexturedQuad geometry in `src/GameEngine/GUI/Element.cs`
- [x] T070 [US1] Update Element.OnActivate() to load texture from _texture property in `src/GameEngine/GUI/Element.cs`
- [x] T071 [US1] Update Element.OnActivate() to create texture descriptor set layout (set=1, binding=0) in `src/GameEngine/GUI/Element.cs`
- [x] T072 [US1] Update Element.OnActivate() to allocate descriptor set from DescriptorManager in `src/GameEngine/GUI/Element.cs`
- [x] T073 [US1] Update Element.OnActivate() to update descriptor set with loaded texture in `src/GameEngine/GUI/Element.cs`
- [x] T074 [US1] Update Element.OnDeactivate() to release texture resource in `src/GameEngine/GUI/Element.cs`
- [x] T075 [US1] [US3] Update Element.Pipeline property to return UIElement uber-shader pipeline in `src/GameEngine/GUI/Element.cs`
- [x] T076 [US1] Update Element.GetGeometryDefinition() to return TexturedQuad in `src/GameEngine/GUI/Element.cs`
- [x] T077 [US1] Update Element.GetDrawCommands() to create UIElementPushConstants in `src/GameEngine/GUI/Element.cs`
- [x] T078 [US1] Update Element.GetDrawCommands() to include texture descriptor set in DrawCommand in `src/GameEngine/GUI/Element.cs`
- [x] T079 [US1] Add partial void OnTextureChanged(TextureDefinition? oldValue) handler to reload texture and update descriptor set in `src/GameEngine/GUI/Element.cs`
- [x] T080 [US1] [US2] Add Texture, UVMin, UVMax properties to ElementTemplate in `src/GameEngine/GUI/ElementTemplate.cs`
- [x] T081 [US1] Run unit tests for Element: `dotnet test Tests/Tests.csproj --filter ElementTexture`
- [x] T082 [US1] Verify existing Element-based tests still pass: `dotnet test Tests/Tests.csproj --filter Element`

**Checkpoint**: Element base class uses uber-shader, defaults to dummy texture for solid colors, supports real textures

---

## Phase 5: Test Assets and Integration Tests

**Purpose**: Create test texture assets and frame-based integration tests with pixel sampling. All tests use Element base class with Texture property.

**User Story Mapping**: 
- **US1 (P1)**: Basic Textured UI Element - visual validation
- **US2 (P2)**: UV Coordinate Control - atlas and cropping tests
- **US3 (P1)**: Performance Optimization - batching and multi-element tests

### Test Assets for Phase 5

- [x] T083 [P] Create test_texture.png (256×256, solid red #FF0000) in `TestApp/Resources/Textures/test_texture.png`
- [x] T084 [P] Create test_atlas.png (512×512, 4 quadrants: red, green, blue, yellow) in `TestApp/Resources/Textures/test_atlas.png`
- [x] T085 [P] Create test_icon.png (64×64, white square with black border) in `TestApp/Resources/Textures/test_icon.png`
- [x] T086 [P] Create white_texture.png (256×256, solid white #FFFFFF) in `TestApp/Resources/Textures/white_texture.png`
- [x] T087 Add texture definitions for test assets to `TestApp/TestResources.cs`

### Integration Tests for Phase 5 [US1] [US2] [US3]

- [x] T088 [P] [US1] Create ColoredElementTest (Element with red tint, dummy texture) in `TestApp/TestComponents/UITexture/ColoredElementTest.cs`
- [x] T089 [P] [US1] Create BasicTextureTest (Element with test_texture.png) in `TestApp/TestComponents/UITexture/BasicTextureTest.cs`
- [x] T090 [P] [US1] [US3] Create MultiTextureTest (3 Elements with different textures) in `TestApp/TestComponents/UITexture/MultiTextureTest.cs`
- [x] T091 [P] [US1] [US3] Create SharedTextureTest (10 Elements with same texture, verify resource sharing) in `TestApp/TestComponents/UITexture/SharedTextureTest.cs`
- [x] T092 [P] [US1] [US3] Create MixedUITest (5 Elements: 3 solid colors, 2 textured, verify no pipeline switches) in `TestApp/TestComponents/UITexture/MixedUITest.cs`
- [x] T093 [P] [US1] Create TintColorTest (Element with white texture, red tint) in `TestApp/TestComponents/UITexture/TintColorTest.cs`
- [x] T094 [P] [US2] Create UVCoordinateTest (Element with test_atlas.png, UVMin/Max for red quadrant) in `TestApp/TestComponents/UITexture/UVCoordinateTest.cs`
- [x] T095 [P] [US1] Create DynamicTextureChangeTest (Element changes texture after 60 frames) in `TestApp/TestComponents/UITexture/DynamicTextureChangeTest.cs`
- [x] T096 Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=ColoredElement`
- [x] T097 Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=BasicTexture`
- [x] T098 Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=MultiTexture`
- [x] T099 Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=SharedTexture`
- [x] T100 Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=MixedUI`
- [x] T101 Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=TintColor`
- [x] T102 Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=UVCoordinate`
- [x] T103 Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=DynamicTextureChange`

**Checkpoint**: All integration tests pass with pixel sampling validation

---

## Phase 6: Performance Profiling and Optimization

**Purpose**: Verify batching effectiveness and performance targets

**User Story Mapping**: **US3 (P1)**: Performance Optimization - validate batching and draw call reduction

### Tests for Phase 6 [US3]

- [x] T104 [P] [US3] Create batching tests in `Tests/TexturedBatchingTests.cs`
- [x] T105 [P] [US3] Add test: Batching_SameTexture_MinimizesDrawCalls in `Tests/TexturedBatchingTests.cs`
- [x] T106 [P] [US3] Add test: Batching_DifferentTextures_GroupsByTexture in `Tests/TexturedBatchingTests.cs`
- [x] T107 [P] [US3] Add test: Batching_RespectZIndex_MaintainsRenderOrder in `Tests/TexturedBatchingTests.cs`

### Implementation for Phase 6 [US3]

- [x] T108 [US3] Create TexturedStressTest (100+ textured elements) in `TestApp/TestComponents/TexturedStressTest.cs`
- [ ] T109 [US3] Add logging to DefaultBatchStrategy to confirm texture-based grouping in `src/GameEngine/Graphics/DefaultBatchStrategy.cs`
- [ ] T110 [US3] Run stress test and profile draw call count: `dotnet run --project TestApp/TestApp.csproj -- --filter=StressTest`
- [ ] T111 [US3] Measure frame time with 500 elements (expect <16ms): document results in research.md
- [ ] T112 [US3] Verify descriptor pool capacity (monitor DescriptorManager logs): document results in research.md
- [ ] T113 [US3] Optimize if performance targets not met (only if needed)
- [x] T114 [US3] Run batching tests: `dotnet test Tests/Tests.csproj --filter TexturedBatching`

**Checkpoint**: Performance targets met (100 same-texture elements → 1 draw call, 500 elements → 60 FPS)

---

## Phase 7: Shader Consolidation and Cleanup

**Purpose**: Remove obsolete shaders now that uber-shader replaces multiple specialized shaders. Consolidate all UI rendering to use `ui_element` uber-shader.

**User Story Mapping**: **US4 (P2)**: Extensibility - simplify shader architecture for future extensions

### Implementation for Phase 7 [US4]

- [ ] T115 [P] [US4] Verify gradient shader usage (LinearGradient, RadialGradient, BiaxialGradient) via grep search
- [ ] T116 [P] [US4] Document decision to keep gradient shaders in research.md
- [ ] T117 [US4] Delete uniform_color.vert shader file: `src/GameEngine/Shaders/uniform_color.vert`
- [ ] T118 [US4] Delete uniform_color.frag shader file: `src/GameEngine/Shaders/uniform_color.frag`
- [ ] T119 [US4] Delete uniform_color.vert.spv compiled shader: `src/GameEngine/Shaders/uniform_color.vert.spv`
- [ ] T120 [US4] Delete uniform_color.frag.spv compiled shader: `src/GameEngine/Shaders/uniform_color.frag.spv`
- [ ] T121 [US4] Delete embedded uniform_color.vert.spv: `src/GameEngine/EmbeddedResources/Shaders/uniform_color.vert.spv`
- [ ] T122 [US4] Delete embedded uniform_color.frag.spv: `src/GameEngine/EmbeddedResources/Shaders/uniform_color.frag.spv`
- [ ] T123 [US4] Delete UniformColorPushConstants.cs: `src/GameEngine/Graphics/UniformColorPushConstants.cs`
- [ ] T124 [US4] Search for usage of image_texture shader via grep
- [ ] T125 [US4] Delete image_texture.vert shader file: `src/GameEngine/Shaders/image_texture.vert`
- [ ] T126 [US4] Delete image_texture.frag shader file: `src/GameEngine/Shaders/image_texture.frag`
- [ ] T127 [US4] Delete image_texture.vert.spv compiled shader: `src/GameEngine/Shaders/image_texture.vert.spv`
- [ ] T128 [US4] Delete image_texture.frag.spv compiled shader: `src/GameEngine/Shaders/image_texture.frag.spv`
- [ ] T129 [US4] Delete embedded image_texture.vert.spv: `src/GameEngine/EmbeddedResources/Shaders/image_texture.vert.spv`
- [ ] T130 [US4] Delete embedded image_texture.frag.spv: `src/GameEngine/EmbeddedResources/Shaders/image_texture.frag.spv`
- [ ] T131 [US4] Update ImageTextureBackground to use UIElement pipeline in `src/GameEngine/GUI/BackgroundLayers/ImageTextureBackground.cs`
- [ ] T132 [US4] Update TextElement to use UIElement pipeline in `src/GameEngine/GUI/TextElement.cs`
- [ ] T133 [US4] Search for usage of per_vertex_color shader via grep
- [ ] T134 [US4] If per_vertex_color unused: Delete per_vertex_color shader files (vert, frag, spv)
- [ ] T135 [US4] Search for usage of legacy shader.vert/frag via grep
- [ ] T136 [US4] If shader.vert/frag unused: Delete shader files (vert, frag, spv)
- [ ] T137 [US4] Remove references to deleted shaders from compile.bat in `src/GameEngine/Shaders/compile.bat`
- [ ] T138 [US4] Remove ShaderDefinitions.UniformColorQuad from `src/GameEngine/Resources/Shaders/ShaderDefinitions.cs`
- [ ] T139 [US4] Remove obsolete shader definitions from ShaderDefinitions.cs (ImageTexture, ColoredGeometry if unused)
- [ ] T140 [US4] Build solution: `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [ ] T141 [US4] Run all tests to verify no regressions: `dotnet test Tests/Tests.csproj`
- [ ] T142 [US4] Run integration tests for ImageTextureBackground: `dotnet run --project TestApp/TestApp.csproj -- --filter=ImageTexture`
- [ ] T143 [US4] Run integration tests for TextElement: `dotnet run --project TestApp/TestApp.csproj -- --filter=TextElement`
- [ ] T144 [US4] Grep for removed shader names and verify no results: search for "uniform_color" and "image_texture"

**Checkpoint**: Obsolete shaders removed, all tests pass, shader architecture simplified

---

## Phase 8: Documentation and Examples

**Purpose**: Update documentation with texture support examples and usage patterns

**User Story Mapping**: Cross-cutting - documentation for all user stories

### Implementation for Phase 8

- [ ] T145 [P] Add TexturedElement to GUI components section in `.docs/Project Structure.md`
- [ ] T146 [P] Document uber-shader consolidation in `.docs/Project Structure.md`
- [ ] T147 [P] List remaining shaders (ui_element + gradients) in `.docs/Project Structure.md`
- [ ] T148 [P] Add test assets location to `.docs/Project Structure.md`
- [ ] T149 [P] Add shader consolidation section to `.docs/Vulkan Architecture.md`
- [ ] T150 [P] Document uber-shader approach and performance benefits in `.docs/Vulkan Architecture.md`
- [ ] T151 [P] Document when to use ui_element vs gradient shaders in `.docs/Vulkan Architecture.md`
- [ ] T152 [P] Add example of creating textured UI elements to `README.md`
- [ ] T153 [P] Show texture atlas usage pattern in `README.md`
- [ ] T154 [P] Document tint color and UV coordinate control in `README.md`
- [ ] T155 [P] Explain uber-shader performance benefits in `README.md`
- [ ] T156 [P] Add pixel sampling examples for textured elements to `src/GameEngine/Testing/README.md`
- [ ] T157 [P] Document color tolerance for anti-aliased edges in `src/GameEngine/Testing/README.md`
- [ ] T158 [P] Create TexturedImageExample (simple image display) in `TestApp/Examples/TexturedImageExample.cs`
- [ ] T159 [P] Create TexturedButtonExample (button with textured background) in `TestApp/Examples/TexturedButtonExample.cs`
- [ ] T160 Verify documentation accuracy by building solution: `dotnet build Nexus.GameEngine.sln`
- [ ] T161 Verify examples compile and run: `dotnet run --project TestApp/TestApp.csproj`

**Checkpoint**: Documentation complete and accurate, examples functional

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (VulkanPixelSampler) ← CRITICAL for all integration tests
    ↓
Phase 2 (Uber-Shader Infrastructure) ← Foundation
    ↓
Phase 3 (Geometry + Pipeline + Dummy Texture) ← Definitions
    ↓
Phase 4 (Element Modification) ← Core implementation [US1, US2, US3]
    ↓
Phase 5 (Integration Tests) ← Validation [US1, US2, US3]
    ↓
Phase 6 (Performance) ← Optimization [US3]
    ↓
Phase 7 (Shader Cleanup) ← Remove obsolete code [US4]
    ↓
Phase 8 (Documentation) ← Finalization
```

### User Story Mapping to Phases

- **US1 (P1) - Basic Textured UI Element**: Phases 2, 3, 4, 5
- **US2 (P2) - UV Coordinate Control**: Phases 4, 5
- **US3 (P1) - Performance Optimization**: Phases 4, 5, 6
- **US4 (P2) - Extensibility**: Phase 7

### Critical Path

Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 7 (Phase 6 overlaps with Phase 5)

### Parallel Opportunities Within Phases

**Phase 1**: Tasks T001-T006 (all tests) can run in parallel

**Phase 2**: Tasks T015-T020 (all tests) can run in parallel, T021-T022 (shaders) can run in parallel, T025-T026 (copy operations) can run in parallel

**Phase 3**: Tasks T031-T044 (all tests) can run in parallel, T045-T046 (dummy texture) can run in parallel with T047-T048 (geometry) and T049 (push constants)

**Phase 4**: Tasks T055-T065 (all tests) can run in parallel

**Phase 5**: Tasks T083-T087 (all test assets) can run in parallel, Tasks T088-T095 (all test implementations) can run in parallel

**Phase 6**: Tasks T104-T107 (all tests) can run in parallel

**Phase 7**: Tasks T115-T116 (gradient verification) can run in parallel, T117-T123 (uniform_color deletion) can run in parallel, T124-T130 (image_texture deletion) can run in parallel with T131-T132 (component updates)

**Phase 8**: Tasks T145-T161 (all documentation) can run in parallel

---

## Parallel Example: Phase 4 Tests

```bash
# Launch all unit tests for Element texture support in parallel:
Task: "Create Element texture tests in Tests/ElementTextureTests.cs"
Task: "Add test: Element_OnActivate_LoadsDummyTextureByDefault in Tests/ElementTextureTests.cs"
Task: "Add test: Element_OnActivate_CreatesTextureDescriptorSet in Tests/ElementTextureTests.cs"
Task: "Add test: Element_UsesTexturedQuadGeometry in Tests/ElementTextureTests.cs"
Task: "Add test: Element_UsesUIElementPipeline in Tests/ElementTextureTests.cs"
Task: "Add test: Element_WithCustomTexture_LoadsSpecifiedTexture in Tests/ElementTextureTests.cs"
Task: "Add test: Element_WithCustomTexture_UpdatesDescriptorSet in Tests/ElementTextureTests.cs"
Task: "Add test: Element_OnDeactivate_ReleasesTexture in Tests/ElementTextureTests.cs"
Task: "Add test: Element_TintColor_DefaultsToWhite in Tests/ElementTextureTests.cs"
Task: "Add test: Element_UVBounds_DefaultToFullTexture in Tests/ElementTextureTests.cs"
Task: "Add test: Element_TextureChange_UpdatesDescriptorSet in Tests/ElementTextureTests.cs"
```

---

## Implementation Strategy

### TDD Workflow (Required by Constitution)

Each phase follows Red-Green-Refactor:
1. **Red Phase**: Write tests first, confirm they fail
2. **Green Phase**: Implement code to make tests pass
3. **Refactor**: Clean up implementation if needed
4. **Verify**: Run all tests to ensure no regressions

### MVP Scope (Phase 1-5)

1. Complete Phase 1: VulkanPixelSampler (critical dependency)
2. Complete Phase 2: Uber-Shader Infrastructure
3. Complete Phase 3: Geometry + Pipeline + Dummy Texture
4. Complete Phase 4: Element Modification (US1, US2, US3 core)
5. Complete Phase 5: Integration Tests (validation)
6. **STOP and VALIDATE**: Run all tests, verify performance targets
7. Deploy/demo if ready (MVP complete)

### Incremental Delivery

- **Milestone 1**: Phase 1-3 complete → Foundation ready, shaders compile
- **Milestone 2**: Phase 4 complete → Element supports textures, unit tests pass
- **Milestone 3**: Phase 5 complete → Integration tests pass, visual validation complete
- **Milestone 4**: Phase 6 complete → Performance targets met
- **Milestone 5**: Phase 7 complete → Shader architecture simplified
- **Milestone 6**: Phase 8 complete → Documentation and examples ready

### Sequential Workflow

This feature requires sequential phase completion due to dependencies:
- Phase 1 must complete before any integration tests (Phases 5-6)
- Phase 2 must complete before Phase 3 (shaders required for pipeline)
- Phase 3 must complete before Phase 4 (geometry/pipeline required for Element)
- Phase 4 must complete before Phase 5 (Element implementation required for tests)
- Phase 5 should complete before Phase 7 (verify existing components work with uber-shader)

Within each phase, tasks marked [P] can run in parallel.

---

## Build and Test Commands

### Compile Shaders (Phase 2)
```powershell
cd src/GameEngine/Shaders
.\compile.bat
```

### Build Solution (After Each Phase)
```powershell
dotnet build Nexus.GameEngine.sln --configuration Debug
```

### Run Unit Tests (After Phases 1-4, 6)
```powershell
# All tests
dotnet test Tests/Tests.csproj

# Specific test filter
dotnet test Tests/Tests.csproj --filter VulkanPixelSampler
dotnet test Tests/Tests.csproj --filter UIElementShader
dotnet test Tests/Tests.csproj --filter TexturedQuadGeometry
dotnet test Tests/Tests.csproj --filter ElementTexture
```

### Run Integration Tests (Phase 5)
```powershell
# All integration tests
dotnet run --project TestApp/TestApp.csproj --configuration Debug

# Filtered integration tests
dotnet run --project TestApp/TestApp.csproj -- --filter=ColoredElement
dotnet run --project TestApp/TestApp.csproj -- --filter=BasicTexture
dotnet run --project TestApp/TestApp.csproj -- --filter=MultiTexture
dotnet run --project TestApp/TestApp.csproj -- --filter=SharedTexture
dotnet run --project TestApp/TestApp.csproj -- --filter=MixedUI
dotnet run --project TestApp/TestApp.csproj -- --filter=TintColor
dotnet run --project TestApp/TestApp.csproj -- --filter=UVCoordinate
dotnet run --project TestApp/TestApp.csproj -- --filter=DynamicTextureChange
dotnet run --project TestApp/TestApp.csproj -- --filter=StressTest
```

---

## Success Criteria Summary

### Phase Completion Criteria

- [x] **Phase 1**: VulkanPixelSampler implemented, unit tests pass, manual validation successful
- [x] **Phase 2**: Uber-shader compiles without errors, shader metadata tests pass, performance measurement documented
- [ ] **Phase 3**: Geometry/pipeline/dummy texture definitions validated, all unit tests pass
- [ ] **Phase 4**: Element uses uber-shader, defaults to dummy texture, all unit tests pass
- [ ] **Phase 5**: All integration tests pass with pixel sampling validation
- [ ] **Phase 6**: Performance targets met (100 same-texture → 1 draw call, 500 elements → 60 FPS)
- [ ] **Phase 7**: Obsolete shaders removed, ImageTextureBackground and TextElement migrated, all tests pass
- [ ] **Phase 8**: Documentation updated, examples functional

### Overall Feature Success

- [ ] All 8 phases complete
- [ ] VulkanPixelSampler functional
- [ ] Uber-shader replaces multiple specialized shaders
- [ ] Element base class uses uber-shader with dummy texture
- [ ] All unit tests pass (Tests project)
- [ ] All integration tests pass (TestApp) with pixel sampling
- [ ] Performance targets met (60 FPS with 500 elements)
- [ ] Pipeline switches eliminated (~500µs saved per frame)
- [ ] Batching effective (draw call reduction confirmed)
- [ ] Descriptor pool capacity sufficient
- [ ] Obsolete shaders removed
- [ ] ImageTextureBackground and TextElement migrated
- [ ] Documentation updated with uber-shader architecture
- [ ] Build succeeds with no errors/warnings

### User Story Acceptance

- [ ] **US1 (P1) - Basic Textured UI Element**: BasicTextureTest, TintColorTest, DynamicTextureChangeTest pass
- [ ] **US2 (P2) - UV Coordinate Control**: UVCoordinateTest passes (shows correct texture region)
- [ ] **US3 (P1) - Performance Optimization**: StressTest shows batching, 500 elements @ 60 FPS
- [ ] **US4 (P2) - Extensibility**: Shader architecture simplified, ImageTextureBackground/TextElement migrated

---

## Notes

- **[P] tasks**: Different files, no dependencies, can run in parallel
- **[Story] labels**: Map tasks to specific user stories (US1, US2, US3, US4)
- **TDD workflow**: Write tests first (Red), implement (Green), refactor, verify
- **Checkpoints**: Stop after each phase to validate before proceeding
- **Performance monitoring**: Document measurements in research.md
- **Build incrementally**: Compile and test after each task group
- **Filter tests**: Use `--filter` for focused testing during debugging
- **Constitution compliance**: Follow documentation-first TDD, component architecture, source-generated properties

---

## Task Count Summary

- **Phase 1** (VulkanPixelSampler): 14 tasks (6 tests + 8 implementation)
- **Phase 2** (Uber-Shader): 16 tasks (6 tests + 10 implementation)
- **Phase 3** (Geometry/Pipeline/Dummy): 24 tasks (14 tests + 10 implementation)
- **Phase 4** (Element Modification): 27 tasks (11 tests + 16 implementation)
- **Phase 5** (Integration Tests): 21 tasks (5 assets + 16 tests)
- **Phase 6** (Performance): 11 tasks (4 tests + 7 implementation)
- **Phase 7** (Shader Cleanup): 30 tasks (all implementation)
- **Phase 8** (Documentation): 17 tasks (all documentation)

**Total**: 161 tasks

**Parallel Opportunities**: 47 tasks marked [P] can run in parallel with siblings in same phase

**Test Tasks**: 41 unit test tasks, 16 integration test tasks = 57 total test tasks (35% of all tasks)

---

**Generated**: 2025-11-03  
**Feature Branch**: `1-ui-texture-support`  
**Status**: Ready for implementation
