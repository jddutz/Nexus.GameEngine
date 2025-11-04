# Performance Checklist: Camera System Refactoring

## Performance Goals

### Bandwidth Reduction (Primary Goal)
- [X] ViewProjection matrix in UBO (uniform_color shader)
- [X] Push constants reduced: 80 bytes → 16 bytes (uniform_color)
- [ ] ViewProjection matrix in UBO (gradient shaders)
- [ ] Push constants reduced: 80 bytes → 16 bytes (gradients)
- [ ] ViewProjection matrix in UBO (texture shader)
- [ ] Push constants reduced: 80 bytes → 16 bytes (texture)

**Target**: 99% reduction in matrix binding bandwidth (matrix bound once per viewport instead of per draw command)

### Memory Efficiency
- [X] UBO buffer created per camera (64 bytes per camera)
- [X] Descriptor set allocated per camera
- [ ] No memory leaks in UBO lifecycle
- [ ] Proper cleanup on camera disposal

### Frame Time
- [ ] Frame times remain under 16ms (60 FPS target)
- [ ] No performance regression in typical UI scenes
- [ ] Multi-viewport rendering scales linearly with viewport count

## Performance Measurements

### Before Optimization
- Push constant calls: N draw commands × 80 bytes each
- Matrix binding: Per-command overhead

### After Optimization (uniform_color)
- [X] Push constant calls: N draw commands × 16 bytes each
- [X] Matrix binding: Once per viewport (64 bytes)
- [X] 92/93 tests pass with new approach

### After Optimization (all shaders)
- [ ] All shaders use UBO pattern
- [ ] Consistent 16-byte push constants
- [ ] Single matrix binding per viewport per shader

## Performance Validation

### Profiling
- [ ] Log matrix binding calls in Renderer
- [ ] Verify one binding per viewport per frame
- [ ] Measure frame time with 1000+ draw commands

### Stress Testing
- [ ] Test with multiple viewports (2-4 cameras)
- [ ] Test with high draw command count (1000+)
- [ ] Test with rapid window resizing

## Optimization Notes
- UBO approach chosen over push constants for matrix to reduce per-command overhead
- 64-byte alignment requirement satisfied (Matrix4x4 is naturally 64 bytes)
- Descriptor set binding cost amortized across all draw commands in viewport
