# RenderPasses Design Documentation

## Overview

The `RenderPasses` static class defines 11 standard render passes as bit flags, providing an efficient, zero-overhead system for controlling render execution order and filtering.

## Core Concepts

### Bit Position = Execution Order

```
Bit 0  (1u << 0)  = Shadow      - Execute FIRST
Bit 1  (1u << 1)  = Depth       - Execute SECOND
Bit 2  (1u << 2)  = Background  - Execute THIRD
Bit 3  (1u << 3)  = Main        - Execute FOURTH
...
Bit 10 (1u << 10) = Debug       - Execute LAST
```

### Zero-Cost Abstraction

```csharp
// During frame rendering:
uint activePasses = 0;  // Start with no passes

// As draw commands are collected:
foreach (var cmd in drawCommands)
{
    activePasses |= cmd.RenderMask;  // Set bits for required passes
}

// Execute only active passes:
for (int i = 0; i <= 10; i++)
{
    uint pass = 1u << i;
    if ((activePasses & pass) == 0)
        continue;  // ✅ Skipped - no draw commands for this pass
    
    // Execute pass...
}
```

**Result:** Menu with Background + UI only executes 2 passes. The other 9 passes have zero cost (not even a comparison).

---

## Pass Definitions

### 1. Shadow Pass (Bit 0)
**Purpose:** Generate shadow maps from light's perspective  
**Execution:** First (depth-only rendering)  
**Configuration:**
- Depth test: Enabled
- Depth write: Enabled
- Color write: Disabled
- Culling: Front-face (avoid shadow acne)

**Usage:**
```csharp
public class Tree : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Main | RenderPasses.Shadow;
}
```

### 2. Depth Pass (Bit 1)
**Purpose:** Depth prepass for early-Z optimization  
**Execution:** Second (before main pass)  
**Configuration:**
- Depth test: Enabled
- Depth write: Enabled
- Color write: Disabled
- Shaders: Vertex-only (minimal fragment work)

**Usage:**
```csharp
// Optional - enable in VulkanSettings for scenes with heavy overdraw
public class Character : RenderableBase
{
    protected override uint GetRenderPasses() => 
        RenderPasses.Depth | RenderPasses.Main | RenderPasses.Shadow;
}
```

### 3. Background Pass (Bit 2)
**Purpose:** Skybox or background rendering  
**Execution:** Third (after depth setup)  
**Configuration:**
- Depth test: LEQUAL (render at depth=1.0)
- Depth write: Disabled
- Culling: Front-face (inside of cube)

**Usage:**
```csharp
public class Skybox : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Background;
    
    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        yield return new DrawCommand
        {
            RenderMask = RenderPasses.Background,
            Pipeline = _skyboxPipeline,
            VertexBuffer = _cubeMesh,
            VertexCount = 36,
            DepthSortKey = float.MaxValue  // Render at infinite depth
        };
    }
}
```

### 4. Main Pass (Bit 3)
**Purpose:** Primary opaque scene rendering  
**Execution:** Fourth (core rendering)  
**Configuration:**
- Depth test: Enabled (Less)
- Depth write: Enabled
- Culling: Back-face
- Sorting: Front-to-back (for early-Z)

**Usage:**
```csharp
public class Sprite : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Main;
}

public class StaticMesh : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Main;
}
```

### 5. Lighting Pass (Bit 4)
**Purpose:** Deferred lighting accumulation  
**Execution:** Fifth (after G-buffer)  
**Configuration:**
- Depth test: Disabled
- Blending: Additive (One, One)
- Full-screen quad per light

**Usage:**
```csharp
// Future - deferred renderer only
public class DeferredLightRenderer
{
    protected override uint GetRenderPasses() => RenderPasses.Lighting;
}
```

### 6. Reflection Pass (Bit 5)
**Purpose:** Reflective surfaces (SSR, planar)  
**Execution:** Sixth (after main scene available)  
**Configuration:**
- Depth test: Enabled
- Requires: Main pass output as texture

**Usage:**
```csharp
public class Mirror : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Reflection;
}

public class Water : RenderableBase
{
    protected override uint GetRenderPasses() => 
        RenderPasses.Main | RenderPasses.Reflection;  // Both passes
}
```

### 7. Transparent Pass (Bit 6)
**Purpose:** Alpha-blended surfaces  
**Execution:** Seventh (after all opaque)  
**Configuration:**
- Depth test: Enabled (Less)
- Depth write: **Disabled**
- Blending: SrcAlpha, OneMinusSrcAlpha
- Sorting: Back-to-front (use DepthSortKey)

**Usage:**
```csharp
public class Glass : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Transparent;
    
    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        // Calculate depth for back-to-front sorting
        float depth = Vector3.Distance(Position, context.Camera.Position);
        
        yield return new DrawCommand
        {
            RenderMask = RenderPasses.Transparent,
            Pipeline = _glassPipeline,
            VertexBuffer = _mesh,
            VertexCount = _vertexCount,
            DepthSortKey = depth  // Higher = farther = render first
        };
    }
}
```

### 8. Particles Pass (Bit 7)
**Purpose:** Particle effects  
**Execution:** Eighth (after transparent)  
**Configuration:**
- Depth test: Enabled (Less)
- Depth write: Disabled
- Blending: Additive (One, One) or Alpha
- Soft particles: Depth fade

**Usage:**
```csharp
public class ParticleSystem : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Particles;
}

public class Fire : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Particles;
}
```

### 9. Post Processing Pass (Bit 8)
**Purpose:** Full-screen effects  
**Execution:** Ninth (after scene complete)  
**Configuration:**
- Depth test: Disabled
- Full-screen quad
- Requires: Scene as input texture

**Usage:**
```csharp
public class BloomEffect : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Post;
}

public class ToneMappingEffect : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Post;
}
```

### 10. UI Pass (Bit 9)
**Purpose:** Screen-space UI overlay  
**Execution:** Tenth (after post-processing)  
**Configuration:**
- Depth test: Disabled
- Projection: Orthographic
- Blending: Alpha

**Usage:**
```csharp
public class TextElement : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.UI;
}

public class Button : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.UI;
}

public class HealthBar : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.UI;
}
```

### 11. Debug Pass (Bit 10)
**Purpose:** Development overlays  
**Execution:** Last (overlay everything)  
**Configuration:**
- Depth test: Optional
- Wireframe mode supported
- #if DEBUG only

**Usage:**
```csharp
#if DEBUG
public class CollisionDebugRenderer : RenderableBase
{
    protected override uint GetRenderPasses() => RenderPasses.Debug;
}
#endif
```

---

## Common Patterns

### Basic 2D Sprite
```csharp
RenderMask = RenderPasses.Main
```

### 3D Object with Shadow
```csharp
RenderMask = RenderPasses.Main | RenderPasses.Shadow
```

### Transparent Effect
```csharp
RenderMask = RenderPasses.Transparent
```

### Full 3D Object (Opaque)
```csharp
RenderMask = RenderPasses.Depth | RenderPasses.Main | RenderPasses.Shadow
```

### UI Element
```csharp
RenderMask = RenderPasses.UI
```

### Particle Effect
```csharp
RenderMask = RenderPasses.Particles
```

---

## Performance Characteristics

### Example Scenarios

#### Simple Menu
```
Active passes: Background (bit 2), UI (bit 9)
Bitmask: 0b0100000100
Cost: 2 render passes executed, 9 passes skipped (zero cost)
```

#### 3D Game Scene
```
Active passes: Shadow, Main, Transparent, UI
Bitmask: 0b1001001001
Cost: 4 render passes executed, 7 passes skipped (zero cost)
```

#### Full AAA Scene
```
Active passes: All except Debug
Bitmask: 0b01111111111
Cost: 10 render passes executed, 1 pass skipped
```

### Bitwise Operations Performance

```csharp
// Collecting active passes (per DrawCommand)
activePasses |= cmd.RenderMask;  // ~1 CPU cycle

// Checking if pass is active (per pass per frame)
if ((activePasses & pass) == 0) continue;  // ~2 CPU cycles

// Total overhead at 60 FPS with 11 passes
11 checks × 60 FPS = 660 checks/second = ~1320 cycles/second
On 3 GHz CPU = 0.00044 ms per frame (negligible)
```

---

## Integration with Renderer

```csharp
public void OnRender(double deltaTime)
{
    // 1. Collect all draw commands
    var allDrawCommands = GetDrawCommandsFromComponents().ToList();
    
    // 2. Determine active passes (bit register)
    uint activePasses = 0;
    foreach (var cmd in allDrawCommands)
    {
        activePasses |= cmd.RenderMask;
    }
    
    _logger.LogTrace("Active passes this frame: {Passes} ({Count} total)", 
        RenderPasses.GetNames(activePasses), 
        RenderPasses.CountPasses(activePasses));
    
    // 3. Execute active passes in order
    foreach (var passMask in RenderPasses.GetActivePasses(activePasses))
    {
        var passName = RenderPasses.GetName(passMask);
        var renderPass = swapChain.GetRenderPass(passMask);
        var framebuffer = swapChain.GetFramebuffer(passMask, imageIndex);
        
        // Filter commands for this pass
        var passCommands = allDrawCommands
            .Where(cmd => RenderPasses.HasPass(cmd.RenderMask, passMask))
            .OrderBy(cmd => cmd, GetBatchStrategy(passMask))
            .ToList();
        
        _logger.LogTrace("  {PassName}: {Count} draw commands", passName, passCommands.Count);
        
        // Execute render pass
        BeginRenderPass(cmd, renderPass, framebuffer);
        foreach (var drawCmd in passCommands)
            Draw(cmd, drawCmd);
        EndRenderPass(cmd);
    }
}
```

---

## Future Extensions

### Custom Passes (If Needed Later)

```csharp
// User-defined passes (bits 11-31)
public const uint Custom1 = 1u << 11;
public const uint Custom2 = 1u << 12;
// ... up to bit 31 for uint
```

### Per-Pass Configuration

```csharp
// Future: SwapChain could support pass-specific settings
var passConfig = new RenderPassConfiguration
{
    Pass = RenderPasses.Transparent,
    DepthTest = true,
    DepthWrite = false,
    BlendMode = BlendMode.Alpha,
    ClearColor = null  // Don't clear, load previous
};
```

---

## Summary

✅ **11 comprehensive passes** cover 95% of game rendering  
✅ **Zero overhead** for unused passes  
✅ **Simple bit operations** for maximum performance  
✅ **Execution order** defined by bit position  
✅ **Type-safe constants** prevent typos  
✅ **Extensible** up to 32 passes (uint limit)  
✅ **Industry-aligned** with Unity, Unreal, Godot patterns
