# Feature Specification: UI Element Texture Support

**Feature Branch**: `1-ui-texture-support`  
**Created**: 2025-11-03  
**Status**: Draft  
**Priority**: P1

## Overview

This feature adds texture rendering capabilities to UI elements in the game engine, enabling developers to display images, icons, backgrounds, and textured UI components. The system will provide a flexible, extensible architecture that supports various texturing scenarios while maintaining high performance through proper resource management and batching.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Textured UI Element (Priority: P1)

Developers creating UI applications should be able to display textured elements (images, icons, buttons with backgrounds) by specifying a texture source and having the system handle all rendering complexity.

**Why this priority**: This is the fundamental requirement. Without basic texture rendering, developers cannot create visually rich UIs. Every modern UI system needs to display images and icons. This is the foundation for all other texture-related features.

**Independent Test**: Can be fully tested by creating a single textured UI element with a test texture asset, positioning it on screen, and verifying via pixel sampling that the correct texture colors appear at expected screen coordinates.

**Acceptance Scenarios**:

1. **Given** a UI element with a texture source specified, **When** the element activates, **Then** the texture loads and renders correctly at the element's position and size
2. **Given** a textured element with specific position and size, **When** rendering occurs, **Then** pixel samples from the rendered area match the expected texture colors within tolerance
3. **Given** a textured element with a tint color, **When** rendering occurs, **Then** the texture colors are multiplied by the tint color as expected
4. **Given** multiple textured elements with the same texture, **When** rendering occurs, **Then** the texture resource is shared (not duplicated) and both elements render correctly

---

### User Story 2 - UV Coordinate Control (Priority: P2)

Developers creating custom UI components should be able to control which portion of a texture is displayed by specifying UV coordinates, enabling texture atlases, sprite sheets, and cropped image rendering.

**Why this priority**: This enables texture atlases (multiple images in one texture) which is critical for performance. It also enables sprite sheets for animations and cropped images for fill modes. This is common in production game engines.

**Independent Test**: Can be tested by creating an element with a texture atlas, specifying UV bounds for a specific region, and verifying via pixel sampling that only the correct sub-region is displayed.

**Acceptance Scenarios**:

1. **Given** an element with UV bounds (0.0, 0.0) to (0.5, 0.5), **When** rendering occurs, **Then** only the top-left quarter of the texture is displayed
2. **Given** an element with UV bounds targeting a specific icon in an atlas, **When** rendering occurs, **Then** only that icon region is visible
3. **Given** an element with animated UV coordinates, **When** UV values change, **Then** the displayed texture region updates accordingly without reloading the texture

---

### User Story 3 - Performance Optimization (Priority: P1)

The rendering system should batch textured elements efficiently, minimizing state changes and draw calls while maintaining correct render order and supporting thousands of textured UI elements per frame.

**Why this priority**: Performance is critical for game engines. Without proper batching, a UI with hundreds of icons/images would cause severe frame rate drops. This directly impacts user experience and is expected in any production engine.

**Independent Test**: Can be measured by profiling draw call counts and render times with 100+ textured elements using the same texture vs different textures. Success = same-texture elements batched into single draw call.

**Acceptance Scenarios**:

1. **Given** 100 UI elements with the same texture, **When** rendering a frame, **Then** elements are batched into minimal draw calls (ideally 1 draw call)
2. **Given** elements with different textures, **When** rendering a frame, **Then** batch strategy groups by texture to minimize pipeline/descriptor set rebinds
3. **Given** textured elements with different Z-indexes, **When** rendering a frame, **Then** elements render in correct depth order while still batching compatible elements

---

### User Story 4 - Extensibility for Various Scenarios (Priority: P2)

The texture system should support different use cases through a flexible architecture: uniform textures (whole element textured), bordered textures (9-slice), gradient textures, and custom shader-based effects.

**Why this priority**: Different UI scenarios require different texturing approaches. A button needs 9-slice borders, text needs font atlases with SDF rendering, and special effects need custom shaders. Extensibility prevents future rewrites.

**Independent Test**: Can be tested by implementing two different texture rendering strategies (uniform texture vs 9-slice) and verifying both work with the same base architecture without modifying core rendering code.

**Acceptance Scenarios**:

1. **Given** a developer creates a custom TexturedElement subclass, **When** overriding shader/geometry methods, **Then** the custom implementation integrates with the existing rendering pipeline
2. **Given** different UI components (Image, Button, Panel), **When** each specifies its texturing strategy, **Then** all strategies coexist without conflicts
3. **Given** a new texturing technique is needed, **When** a developer extends the base classes, **Then** no changes to core rendering or resource management are required

---

## Functional Requirements *(mandatory)*

### Requirement 1: Shader Infrastructure

**Requirement**: Create a dedicated shader for textured UI elements that accepts geometry with position and UV coordinates, applies the camera's view-projection matrix and element's model matrix, samples a texture via descriptor set, and applies tint color from push constants.

**Acceptance Criteria**:
- Shader vertex format includes position (vec2) and UV coordinates (vec2)
- Shader uses UBO at set=0, binding=0 for camera ViewProjection matrix (inherited from camera system)
- Shader uses descriptor set at set=1, binding=0 for combined image sampler (texture)
- Shader uses push constants for model matrix (64 bytes) and tint color (16 bytes) totaling 80 bytes
- Shader compiles without errors using VulkanSDK's glslc compiler
- Compiled SPIR-V modules load successfully into Vulkan shader modules

**Test Strategy**: Unit tests verify shader definition metadata; integration tests verify shader renders textured quad with correct transformation and color multiplication.

---

### Requirement 2: Geometry Resource for Textured Quads

**Requirement**: Create a geometry definition for textured quads with vertex format containing position and UV coordinates, suitable for texture mapping.

**Acceptance Criteria**:
- Geometry definition specifies 4 vertices forming a quad (2 triangles, 6 indices)
- Each vertex contains position (vec2) in normalized space (-1 to 1) and UV coordinates (vec2) in texture space (0 to 1)
- Geometry resource can be created via IGeometryResourceManager.GetOrCreate()
- Geometry resource is cached and reused across multiple textured elements
- Vertex layout matches shader input requirements

**Test Strategy**: Unit tests verify geometry definition structure; integration tests verify geometry renders correctly with texture shader.

---

### Requirement 3: Texture Descriptor Set Management

**Requirement**: Extend Element class or create TexturedElement subclass to manage texture descriptor sets, binding texture resources to shaders for rendering.

**Acceptance Criteria**:
- Component creates descriptor set layout matching shader's texture binding requirements
- Component allocates descriptor set from DescriptorManager during activation
- Component updates descriptor set with texture's ImageView and Sampler
- Component passes descriptor set to DrawCommand for rendering
- Component releases descriptor set resources during deactivation
- Multiple elements can share the same texture resource but have independent descriptor sets

**Test Strategy**: Integration tests verify descriptor sets bind textures correctly; memory profiling confirms descriptor sets are released on deactivation.

---

### Requirement 4: Texture Resource Loading

**Requirement**: Integrate with existing TextureResourceManager to load, cache, and manage texture resources throughout their lifecycle.

**Acceptance Criteria**:
- Elements specify texture via TextureDefinition (embedded resource path or file path)
- TextureResourceManager loads texture data, creates Vulkan Image/ImageView/Sampler
- Texture resources are reference-counted and cached (multiple elements share same texture)
- Texture resources are released when no longer referenced
- Texture loading errors are handled gracefully with fallback or clear error messages

**Test Strategy**: Integration tests verify textures load from embedded resources; unit tests verify reference counting with multiple elements sharing textures.

---

### Requirement 5: Push Constants for Model and Tint

**Requirement**: Create or extend push constant structures to include both model matrix and tint color for per-element transformation and coloring.

**Acceptance Criteria**:
- Push constant structure contains mat4 model (64 bytes) + vec4 tintColor (16 bytes) = 80 bytes total
- Push constants are populated in GetDrawCommands() with element's WorldMatrix and TintColor
- Push constant size and layout match shader expectations exactly
- Structure uses StructLayout(LayoutKind.Sequential) for correct memory layout

**Test Strategy**: Unit tests verify struct size and layout; integration tests verify values reach shader correctly (via visual rendering and pixel sampling).

---

### Requirement 6: Pipeline Definition for Textured Elements

**Requirement**: Create a pipeline definition for textured UI elements that configures blending, depth testing, viewport, scissor, and topology appropriately for textured rendering.

**Acceptance Criteria**:
- Pipeline definition uses the textured UI shader (vertex + fragment)
- Pipeline enables alpha blending for transparency support
- Pipeline configures topology as TriangleList for indexed quad rendering
- Pipeline includes descriptor set layout for camera UBO (set=0) and texture sampler (set=1)
- Pipeline includes push constant range for model matrix and tint color (80 bytes)
- Pipeline is cached by PipelineManager and reused across frames

**Test Strategy**: Integration tests verify pipeline renders textured elements with correct blending and depth; frame debugging confirms pipeline state is correct.

---

### Requirement 7: Batch Rendering Strategy

**Requirement**: Enhance or verify that DefaultBatchStrategy groups textured elements by texture resource to minimize descriptor set rebinds and draw calls.

**Acceptance Criteria**:
- Elements with same texture and pipeline are grouped into adjacent draw commands
- Batch strategy considers texture handle/descriptor set when grouping commands
- Elements with different Z-indexes maintain correct render order despite batching
- Draw call count is minimized for scenes with many elements sharing textures

**Test Strategy**: Performance profiling measures draw call reduction; integration tests with 100+ elements verify batching occurs and render order is correct.

---

### Requirement 8: Frame-Based Integration Testing

**Requirement**: Create frame-based integration tests that render textured UI elements and validate output using pixel sampling.

**Acceptance Criteria**:
- Test creates textured element with known texture and position
- Test waits for frame render to complete (post-render middleware)
- Test samples pixels at expected texture locations
- Test validates pixel colors match expected texture colors within tolerance
- Test reports pass/fail with descriptive error messages
- Tests cover: single texture, multiple textures, shared textures, tint colors, UV coordinates

**Test Strategy**: TestApp integration tests execute in CI/CD pipeline; pixel sampling validates visual correctness.

---

## Technical Requirements

### Architecture Changes

1. **Shader Addition**:
   - Create `ui_textured.vert` and `ui_textured.frag` shader files in `src/GameEngine/Shaders/`
   - Vertex shader: position + UV inputs, camera UBO (set=0), model+color push constants
   - Fragment shader: texture sampler (set=1), tint color from push constants
   - Update `ShaderDefinitions.cs` with new shader definition including descriptor set bindings

2. **Geometry Definition**:
   - Create `GeometryDefinitions.TexturedQuad` entry
   - Vertex format: Position2DTexCoord (vec2 position, vec2 texCoord)
   - 4 vertices, 6 indices for two triangles forming a quad

3. **Pipeline Definition**:
   - Create `PipelineDefinitions.UITexturedElement` entry
   - Configure for alpha blending, UI render pass, textured shader
   - Include descriptor set layouts for camera (set=0) and texture (set=1)

4. **Element Enhancement Options** (choose one):
   
   **Option A: Extend Element Class**
   - Add `TextureDefinition?` property to Element
   - Add texture descriptor set management to Element.OnActivate/OnDeactivate
   - Conditionally use textured pipeline when texture is specified
   
   **Option B: Create TexturedElement Subclass**
   - Create `TexturedElement : Element` subclass
   - Override Pipeline property to return textured pipeline
   - Override GetGeometryDefinition() to return TexturedQuad
   - Add texture descriptor set management in lifecycle methods
   - Provide factory method or template support for declarative creation

5. **Push Constants**:
   - Create or verify `TexturedElementPushConstants` struct (or reuse existing if compatible)
   - 80 bytes: mat4 Model (64 bytes) + vec4 TintColor (16 bytes)

### Assumptions

1. **Texture Format**: Textures will primarily use RGBA8 format (R8G8B8A8_SRGB) which is standard for UI graphics. Other formats can be added later if needed.

2. **Texture Filtering**: Linear filtering with clamp-to-edge addressing mode is sufficient for UI elements. Anisotropic filtering and mipmap support can be added later for 3D scenarios.

3. **UV Coordinate System**: Standard OpenGL/Vulkan UV space (0,0 = top-left, 1,1 = bottom-right) will be used. Elements default to full texture coverage (UV 0-1 on both axes).

4. **Descriptor Set Architecture**: Using set=0 for camera (global), set=1 for per-element resources (texture) follows best practices and allows future expansion (e.g., set=2 for per-material data).

5. **Batching Compatibility**: Current DefaultBatchStrategy already compares PushConstants and DescriptorSet, so texture-based batching will work with minimal or no changes.

6. **Performance Target**: System should support 500+ textured UI elements at 60 FPS on mid-range hardware (assumption for success criteria metrics).

7. **Existing Camera System**: The recently refactored camera system with UBO-based ViewProjection matrices (set=0, binding=0) is working correctly and will be leveraged as-is.

8. **Existing Descriptor Manager**: IDescriptorManager provides all necessary methods for texture descriptor set creation and updates (UpdateDescriptorSet overload for image/sampler binding exists).

### Design Decisions

1. **Shader Approach**: Create dedicated textured UI shader rather than modifying existing uniform_color shader. This keeps shaders simple and allows independent optimization.

2. **Element Design**: Use subclass approach (TexturedElement) rather than extending base Element. This maintains separation of concerns and allows both textured and non-textured elements to coexist cleanly.

3. **Descriptor Set Management**: Each textured element gets its own descriptor set instance (allocated from pool) rather than sharing descriptor sets. This simplifies lifecycle management and supports per-element texture changes.

4. **Resource Sharing**: Texture resources (Image, ImageView, Sampler) are shared via TextureResourceManager's caching, but descriptor sets are per-element. This balances memory efficiency with flexibility.

5. **UV Coordinate Control**: Expose UV coordinates as element properties for flexibility, but default to (0,0)-(1,1) for full texture coverage. This supports both simple cases and advanced scenarios (atlases, 9-slice).

6. **Push Constant Layout**: Keep model matrix in push constants (not UBO) to maintain compatibility with existing rendering architecture. Camera matrix already uses UBO (set=0).

7. **Testing Strategy**: Use frame-based integration tests with pixel sampling rather than only unit tests. Visual correctness is critical for rendering features.

## Success Criteria *(mandatory)*

1. Textured UI element renders correctly with texture colors visible at expected screen positions
2. Pixel sampling validates that rendered colors match source texture colors within 1% tolerance
3. Multiple elements sharing the same texture result in single texture resource load (verified via resource manager logs)
4. Shader compiles without errors and loads successfully into Vulkan
5. 100+ textured elements with the same texture render at 60 FPS with minimal draw calls (batching effective)
6. Elements with different textures render correctly with proper layering (Z-index respected)
7. Tint color functionality works (white tint = original colors, red tint = red-shifted texture)
8. UV coordinate control works (specifying (0,0)-(0.5,0.5) shows top-left quarter of texture)
9. All integration tests pass in TestApp with pixel sampling validation
10. No memory leaks detected after creating/destroying textured elements (descriptor sets released)
11. Build completes without errors or warnings
12. Documentation updated with examples of creating textured elements

## Performance Impact

**Before (No Texture Support)**:
- UI elements render with uniform colors only
- Limited visual richness
- No support for images, icons, or textured backgrounds

**After (With Texture Support)**:
- Textured UI elements with minimal overhead
- Efficient batching reduces draw calls for same-texture elements
- Descriptor set overhead: ~1 descriptor set per textured element (negligible memory: ~64 bytes each)
- Texture memory: depends on assets (typical: 1-4 MB for UI textures)
- Batching effectiveness: 100 elements with same texture → 1 draw call (vs 100 without batching)

**Expected Performance Characteristics**:
- Draw call reduction: Up to 99% when multiple elements share textures
- Frame time impact: <0.5ms for 500 textured elements on mid-range GPU
- Memory overhead: ~64 bytes per element (descriptor set) + shared texture memory
- Load time impact: ~10-50ms per unique texture on first load (cached thereafter)

## Out of Scope

The following features are explicitly **not** included in this specification and may be addressed in future work:

1. **9-Slice (Bordered) Texturing**: Rendering textures with resizable borders while preserving corner detail (common for UI panels/buttons). This requires different geometry and shader logic.

2. **Texture Atlases with Automatic Packing**: Tools or systems to automatically pack multiple small images into texture atlases. This feature assumes textures are pre-prepared; developers manage atlas layouts manually.

3. **Animated Textures**: Frame-based texture animation or sprite sheet playback. While UV coordinate control enables this, automatic animation management is out of scope.

4. **Mipmap Generation**: Automatic mipmap generation for textured UI elements. UI textures typically don't need mipmaps (rendered 1:1 pixel ratio). Can be added later if needed.

5. **Compressed Texture Formats**: Support for GPU-compressed formats (DXT, BC7, ASTC). Initial implementation uses uncompressed RGBA8 for simplicity.

6. **Advanced Texture Effects**: Signed distance field (SDF) rendering, normal mapping, parallax mapping. These require specialized shaders and are better suited for dedicated feature work.

7. **Dynamic Texture Updates**: Updating texture content at runtime (e.g., rendering to texture). This requires render-to-texture infrastructure beyond this scope.

8. **Multi-Texture Elements**: Single element rendering with multiple textures blended together. Initial implementation supports one texture per element.

9. **Texture Streaming**: Loading high-resolution textures progressively or on-demand. Initial implementation loads full textures immediately.

10. **Cross-Platform Texture Loading**: Support for loading textures from platform-specific sources (iOS asset catalogs, Android resources). Initial implementation uses embedded resources or file paths.

## Dependencies

1. **Existing Camera System**: Relies on camera system providing ViewProjection UBO at set=0, binding=0 (implemented in camera-system-refactoring)
2. **Texture Resource Manager**: Depends on ITextureResourceManager and TextureResourceManager implementation (already exists)
3. **Descriptor Manager**: Depends on IDescriptorManager providing texture descriptor set support (already exists with UpdateDescriptorSet overload for image/sampler)
4. **Pipeline Manager**: Depends on IPipelineManager for pipeline creation and caching (already exists)
5. **Geometry Resource Manager**: Depends on IGeometryResourceManager for quad geometry (already exists)
6. **VulkanSDK**: Requires glslc shader compiler to compile new shaders (already required for build)
7. **Pixel Sampling Service**: Integration tests depend on IPixelSampler for visual validation (stub exists, needs implementation)

## Risks and Mitigations

### Risk 1: Descriptor Set Pool Exhaustion
**Description**: Creating one descriptor set per textured element could exhaust descriptor pools if many elements exist.

**Likelihood**: Medium (100s of elements possible in complex UIs)

**Impact**: High (rendering fails with Vulkan errors)

**Mitigation**:
- Configure DescriptorManager with sufficient pool sizes (monitor usage in tests)
- Implement descriptor set reuse/recycling if needed
- Document recommended limits (e.g., max 1000 textured elements per scene)

### Risk 2: Pixel Sampling Stub Not Implemented
**Description**: Integration tests depend on IPixelSampler which is currently a stub (returns dummy data).

**Likelihood**: High (known issue documented in Testing/README.md)

**Impact**: Medium (tests can't validate visual correctness automatically)

**Mitigation**:
- Implement VulkanPixelSampler before starting integration tests (separate task)
- Fallback: Manual visual inspection of test output during development
- Document visual test cases for manual verification if sampling not ready

### Risk 3: Shader Compilation Errors
**Description**: New shaders may have syntax errors or descriptor set binding mismatches.

**Likelihood**: Medium (complex shader with multiple descriptor sets)

**Impact**: Medium (blocks rendering, but detectable early)

**Mitigation**:
- Compile shaders incrementally during development
- Test with simple single-color texture first
- Use Vulkan validation layers to catch binding mismatches
- Reference existing working shaders (image_texture) as templates

### Risk 4: Performance Regression with Many Elements
**Description**: Adding texture support might slow down rendering if batching is ineffective.

**Likelihood**: Low (batching architecture already exists)

**Impact**: Medium (frame rate drops below 60 FPS)

**Mitigation**:
- Profile early with 100+ element stress tests
- Verify DefaultBatchStrategy groups by texture correctly
- Optimize descriptor set binding if needed (bind once per batch)
- Document performance best practices for developers

### Risk 5: UV Coordinate Confusion
**Description**: Developers might confuse screen space coordinates with UV texture space coordinates.

**Likelihood**: Medium (common mistake in graphics programming)

**Impact**: Low (results in visual bugs, not crashes)

**Mitigation**:
- Document UV coordinate system clearly (0,0=top-left, 1,1=bottom-right)
- Provide clear examples in tests and documentation
- Add validation or assertions for UV range (warn if outside 0-1)
- Use descriptive property names (UVMin, UVMax vs just "coordinates")

## Next Steps

After this specification is approved:

1. **Planning Phase** (`/speckit.plan`):
   - Break down requirements into concrete tasks
   - Identify dependencies between tasks
   - Estimate effort for each task
   - Create development checklist

2. **Implementation Order** (recommended):
   1. Implement VulkanPixelSampler (enables visual validation)
   2. Create shader files (ui_textured.vert, ui_textured.frag)
   3. Add shader definition to ShaderDefinitions.cs
   4. Create TexturedQuad geometry definition
   5. Create TexturedElement subclass with descriptor set management
   6. Create pipeline definition for textured elements
   7. Create push constants structure (or verify existing)
   8. Write first integration test (single textured element)
   9. Iterate on implementation until test passes
   10. Add more integration tests (multiple textures, tint, UV control)
   11. Performance profiling and optimization
   12. Documentation and examples

3. **Review and Clarification** (`/speckit.clarify`):
   - Resolve any [NEEDS CLARIFICATION] markers (none currently)
   - Gather stakeholder feedback
   - Refine requirements based on technical feasibility

---

**End of Specification**
