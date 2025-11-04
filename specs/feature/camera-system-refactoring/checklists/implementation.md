# Implementation Checklist: Camera System Refactoring

## Completed Work

### Phase 4: Performance Optimization (Partial)

#### ViewProjection UBO System
- [X] Created ViewProjectionUBO struct (64 bytes, Matrix4x4)
- [X] Updated uniform_color.vert to read from UBO
- [X] Updated uniform_color.frag (no changes needed)
- [X] Created UniformColorPushConstants struct (16 bytes)
- [X] Updated Element.cs to use UniformColorPushConstants
- [X] Updated ShaderDefinitions.cs for uniform_color pipeline

#### Camera UBO Implementation
- [X] StaticCamera: Full UBO lifecycle (init, update, cleanup)
- [X] OrthoCamera: Full UBO lifecycle
- [X] PerspectiveCamera: Full UBO lifecycle
- [X] ICamera.GetViewProjectionDescriptorSet() interface method
- [X] RenderContext.ViewProjectionDescriptorSet field
- [X] Renderer.Draw() binds descriptor set

#### Build and Test
- [X] Solution builds without errors
- [X] Shaders compiled to SPIR-V (uniform_color only)
- [X] Unit tests pass (92/93)
- [X] ContentManagerCameraTests updated with mocks

## In Progress

### Task T130: Search for TransformedColorPushConstants usage
- [X] Search codebase: `grep -r "TransformedColorPushConstants" src/`
- [X] Found one usage in Renderer.cs (legacy code path for gradients/textures - not breaking anything)

### Task T133: Fix ColoredRectTest
- [X] Debug why Element is not rendering
  - Default camera now being tracked (1 active camera)
  - Descriptor set is valid (Handle: 58274116272181)
  - Push constants are correct (Color: R=1.000, G=0.000, B=0.000, A=1.000)
  - Draw method optimized to only bind on changes
- [ ] **STILL FAILING**: Test expects red (1,0,0,1) but gets background (0,0,0.546,1)
- [ ] Need to investigate: shader compilation or pipeline issue?

## Next Steps

### Complete Phase 4 (Performance Optimization)
1. **T130**: Search and fix any remaining TransformedColorPushConstants usage
2. **T133**: Fix ColoredRectTest rendering issue
3. **Gradient Shaders**: Apply UBO pattern to linear/radial/biaxial gradients
4. **Texture Shader**: Apply UBO pattern to image_texture shader
5. **Validation**: Verify all 93/93 tests pass

### Shader Updates Needed
- [ ] linear_gradient.vert/frag - Add UBO binding, reduce push constants
- [ ] radial_gradient.vert/frag - Add UBO binding, reduce push constants
- [ ] biaxial_gradient.vert/frag - Add UBO binding, reduce push constants
- [ ] image_texture.vert/frag - Add UBO binding, reduce push constants

### Documentation Updates
- [ ] Update `.docs/Vulkan Architecture.md` with UBO system
- [ ] Update `.docs/Project Structure.md` with camera UBO management
- [ ] Add shader documentation for UBO pattern

## Deferred Work (Phase 1-3)

These phases were skipped in favor of direct Phase 4 implementation:
- Phase 1: Setup & Documentation (mostly complete)
- Phase 2: Viewport simplification (deferred)
- Phase 3: ContentManager camera tracking (deferred)

**Decision**: Continue with Phase 4 completion before revisiting earlier phases
