# Render Pass Analysis - Mainstream Game Engines

## Unity HDRP (High Definition Render Pipeline)

**Render Passes:**
1. **Shadow Cascade** - Directional light shadows (multiple cascades)
2. **Shadow Spot/Point** - Local light shadows
3. **Depth Prepass** - Z-prepass for early-Z optimization
4. **GBuffer** - Deferred rendering (normals, albedo, metallic, roughness)
5. **Lighting** - Deferred lighting computation
6. **Forward Opaque** - Forward-rendered opaque objects
7. **Sky/Atmosphere** - Skybox, atmospheric scattering
8. **Forward Transparent** - Alpha-blended geometry (back-to-front)
9. **Distortion** - Screen-space distortion (heat waves, glass)
10. **Post Processing** - Bloom, tone mapping, color grading
11. **UI/Overlay** - Screen-space UI elements

**Key Insight:** Separates shadow maps, has both deferred and forward paths

---

## Unreal Engine 5

**Render Passes (Nanite/Lumen era):**
1. **Shadow Depth** - Shadow map generation
2. **Base Pass** - Opaque geometry with material evaluation
3. **Velocity** - Motion vectors for TAA/motion blur
4. **Ambient Occlusion** - SSAO/HBAO
5. **Lighting** - Direct + indirect lighting
6. **Reflection** - Screen-space reflections, planar reflections
7. **Translucency** - Transparent surfaces
8. **Distortion** - Refraction effects
9. **Post Process** - Full-screen effects
10. **HUD** - UI overlay

**Key Insight:** Heavy emphasis on lighting and reflection passes

---

## Godot 4

**Render Passes:**
1. **Shadow** - Shadow map rendering
2. **Depth Prepass** - Optional Z-prepass
3. **Opaque** - Forward or deferred opaque geometry
4. **Sky** - Sky and environment
5. **Transparent** - Alpha blending
6. **Post Process** - Glow, DOF, etc.
7. **Canvas** - 2D rendering / UI

**Key Insight:** Simpler, optimized for indie/mid-tier games

---

## CryEngine

**Render Passes:**
1. **Shadow Gen** - Shadow cascades
2. **Z Prepass** - Depth prepass
3. **Opaque** - Forward opaque
4. **Water** - Specialized water rendering
5. **Transparent** - Sorted transparency
6. **Post Effects** - HDR, bloom, AA
7. **UI** - Overlay

**Key Insight:** Water as separate pass (useful for specialized materials)

---

## Source 2 (Valve)

**Render Passes:**
1. **Shadow** - Shadow maps
2. **Depth** - Depth buffer
3. **Opaque** - Solid geometry
4. **Translucent** - Alpha-blended
5. **Overlay** - HUD/UI
6. **Post** - Effects

**Key Insight:** Minimalist, performance-focused

---

## Common Patterns Across Engines

### Core Passes (Every Engine Has These):
1. ✅ **Shadow** - Shadow map generation
2. ✅ **Opaque/Main** - Solid geometry
3. ✅ **Transparent** - Alpha blending
4. ✅ **UI/Overlay** - Screen-space UI
5. ✅ **Post Process** - Full-screen effects

### Advanced Passes (AAA Engines):
6. **Depth Prepass** - Early-Z optimization
7. **GBuffer** - Deferred rendering data
8. **Lighting** - Deferred light accumulation
9. **Reflection** - SSR, planar, cube maps
10. **Sky/Atmosphere** - Background rendering
11. **Particles** - Soft particles, volumetrics
12. **Distortion** - Refraction, heat haze

### Specialized Passes (Domain-Specific):
- **Water** - Complex water simulation
- **Decals** - Surface decals
- **Terrain** - Heightmap-based terrain
- **Foliage** - Vegetation rendering
- **Velocity** - Motion vectors

---

## Recommendations for Nexus.GameEngine

### Tier 1: Essential (Must Have)
- Shadow
- Main (Opaque)
- Transparent
- UI
- Post

### Tier 2: Common (Should Have)
- Background/Sky
- Particles
- Debug

### Tier 3: Advanced (Nice to Have)
- Depth Prepass
- Lighting (for deferred)
- Reflection

### Tier 4: Future/Optional
- GBuffer (deferred rendering)
- Distortion
- Water
- Velocity

---

## Performance Characteristics

| Pass Type | Frequency | Cost | Purpose |
|-----------|-----------|------|---------|
| Shadow | Per light | Medium | Generate shadow maps |
| Depth Prepass | Optional | Low | Early-Z rejection |
| Main/Opaque | Always | High | Primary scene rendering |
| Background/Sky | Often | Low | Far-plane rendering |
| Transparent | Common | Medium | Alpha blending (sorted) |
| Particles | Common | Medium | Additive/alpha particles |
| Reflection | Optional | High | Mirror surfaces |
| Post Process | Always | Medium | Screen-space effects |
| UI | Always | Low | Overlay rendering |
| Debug | Dev only | Low | Wireframes, bounds |

---

## Execution Order (Typical)

1. **Shadow** - Generate shadow maps first
2. **Depth Prepass** - (Optional) Z-buffer only
3. **Background/Sky** - Far plane at depth=1.0
4. **Main** - Opaque geometry front-to-back
5. **Lighting** - (Deferred only) Light accumulation
6. **Reflection** - Screen-space reflections
7. **Transparent** - Back-to-front blending
8. **Particles** - Additive/soft particles
9. **Post Process** - Full-screen effects
10. **UI** - No depth, orthographic
11. **Debug** - Wireframes, gizmos

---

## Bit Assignment Strategy

```csharp
Shadow      = 1 << 0  // 0b00000000001 (Execute first)
Depth       = 1 << 1  // 0b00000000010
Background  = 1 << 2  // 0b00000000100
Main        = 1 << 3  // 0b00000001000 (Most common)
Lighting    = 1 << 4  // 0b00000010000
Reflection  = 1 << 5  // 0b00000100000
Transparent = 1 << 6  // 0b00001000000
Particles   = 1 << 7  // 0b00010000000
Post        = 1 << 8  // 0b00100000000
UI          = 1 << 9  // 0b01000000000 (Execute near end)
Debug       = 1 << 10 // 0b10000000000 (Execute last)
```

**Note:** Bit position = execution order (mostly)

---

## Example Scenarios

### Simple 2D Menu
```
Enabled: Background (bit 2), UI (bit 9)
Bitmask: 0b0100000100 (2 passes)
```

### 3D Game Scene
```
Enabled: Shadow, Main, Transparent, UI
Bitmask: 0b1001001001 (4 passes)
```

### Full AAA Scene
```
Enabled: All except Debug
Bitmask: 0b01111111111 (10 passes)
```

---

## Conclusion

**Recommended Initial Set (11 passes):**
1. Shadow
2. Depth (optional optimization)
3. Background
4. Main
5. Lighting (for future deferred)
6. Reflection (for future SSR)
7. Transparent
8. Particles
9. Post
10. UI
11. Debug

This covers 95% of use cases while leaving room for growth.
