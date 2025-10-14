# Deep Dive: OpenGL Uniform Management

## Current Problem

**DrawCommand.Uniforms is `Dictionary<string, object>`**

Issues:

1. ❌ Dictionary itself is mutable (can add/remove items)
2. ❌ `object` causes boxing/unboxing every frame
3. ❌ String lookups every frame
4. ❌ `glGetUniformLocation()` call every frame per uniform
5. ❌ Heavy performance cost for something used by every element

## How OpenGL Uniforms Actually Work

### Method 1: Named Uniforms (Current Approach)

```glsl
// In shader
uniform vec4 color;
uniform mat4 modelMatrix;
```

```csharp
// In C#
int location = GL.GetUniformLocation(program, "color");  // String lookup
GL.Uniform4(location, r, g, b, a);                       // Set value
```

**Location is:**

- An integer assigned by GL during shader compilation
- Stable for the lifetime of the shader program
- Can be queried once and cached
- Does NOT require string lookup every frame if cached

**Key insight:** We CAN cache the location!

### Method 2: Explicit Uniform Locations (GLSL 4.3+)

```glsl
// In shader - specify location explicitly
layout(location = 0) uniform vec4 color;
layout(location = 1) uniform mat4 modelMatrix;
```

```csharp
// In C# - use location directly, no lookup needed!
GL.Uniform4(0, r, g, b, a);              // Location 0 = color
GL.UniformMatrix4(1, false, matrix);     // Location 1 = modelMatrix
```

**Benefits:**

- ✅ No string lookup at all
- ✅ No glGetUniformLocation call
- ✅ Locations known at compile time
- ✅ Fast direct access

**Drawback:**

- Requires GLSL 4.3+ (OpenGL 4.3 / 2012)
- Need to coordinate locations between C# and GLSL

### Method 3: Uniform Buffer Objects (UBOs)

```glsl
// In shader
layout(std140, binding = 0) uniform MaterialBlock
{
    vec4 color;
    vec4 ambient;
    vec4 diffuse;
    float shininess;
};
```

```csharp
// In C# - upload entire block at once
struct MaterialData
{
    Vector4 color;
    Vector4 ambient;
    Vector4 diffuse;
    float shininess;
}

uint ubo = GL.GenBuffer();
GL.BindBuffer(BufferTarget.UniformBuffer, ubo);
GL.BufferData(BufferTarget.UniformBuffer, sizeof(MaterialData), ref data, BufferUsage.DynamicDraw);
GL.BindBufferBase(BufferTarget.UniformBuffer, 0, ubo);  // Bind to binding point 0
```

**Benefits:**

- ✅ Upload many uniforms with one bind
- ✅ Shared across shaders
- ✅ No per-uniform calls
- ✅ Better for large uniform sets
- ✅ Type-safe structs (no boxing)

**Drawbacks:**

- More complex setup
- Alignment rules (std140/std430)
- Overkill for simple per-draw uniforms

### Method 4: Push Constants (Vulkan-style, not in OpenGL)

OpenGL doesn't have push constants. This is a Vulkan feature.

## Performance Comparison

### Current Approach (Dictionary<string, object>)

```csharp
// Per frame, per element
foreach (var (name, value) in element.Uniforms)
{
    int loc = GL.GetUniformLocation(shader, name);  // String hash + GL call
    GL.Uniform4(loc, ((Vector4)value).X, ...);      // Unbox
}
```

**Cost per element:**

- Dictionary enumeration: ~50 cycles
- String hash per uniform: ~20-50 cycles
- glGetUniformLocation per uniform: ~100-500 cycles (driver cached)
- Boxing/unboxing per uniform: ~10-20 cycles
- GL.Uniform call: ~50 cycles

**Total for 3 uniforms:** ~600-1800 cycles per element
**For 100 elements:** ~60,000-180,000 cycles per frame

### Optimized: Cached Locations

```csharp
// One-time setup
Dictionary<(uint shader, string name), int> locationCache;
int loc = locationCache[(shader, "color")];

// Per frame
GL.Uniform4(loc, color.X, color.Y, color.Z, color.W);
```

**Cost per element:**

- Dictionary lookup: ~30 cycles
- GL.Uniform call: ~50 cycles

**Total for 3 uniforms:** ~240 cycles per element
**For 100 elements:** ~24,000 cycles per frame

**Savings: 60-75% improvement**

### Best: Explicit Locations (No Lookup)

```csharp
// Per frame - locations hardcoded
GL.Uniform4(0, color.X, color.Y, color.Z, color.W);
GL.UniformMatrix4(1, false, modelMatrix);
```

**Cost per element:**

- GL.Uniform call: ~50 cycles per uniform

**Total for 3 uniforms:** ~150 cycles per element
**For 100 elements:** ~15,000 cycles per frame

**Savings: 75-90% improvement**

## Proposed Solutions

### Solution 1: Cached Location Dictionary (Quick Win)

```csharp
public readonly struct UniformBinding
{
    public readonly int Location;
    public readonly object Value;  // Still boxes, but better

    public UniformBinding(int location, object value)
    {
        Location = location;
        Value = value;
    }
}

public class DrawCommand
{
    // Cache locations at ResourceManager level, store only location + value
    public UniformBinding[] Uniforms { get; init; } = [];
}
```

**Pros:**

- Eliminates string lookup
- Eliminates glGetUniformLocation per frame
- Still flexible

**Cons:**

- Still boxes values
- Still mutable array

### Solution 2: Typed Uniform Struct (No Boxing)

```csharp
public readonly struct UniformData
{
    // Common uniforms pre-defined
    public readonly Vector4D<float> Color;
    public readonly Matrix4X4<float> ModelMatrix;
    public readonly Matrix4X4<float> ViewMatrix;
    public readonly Matrix4X4<float> ProjectionMatrix;
    public readonly int TextureSlot;

    // Bit flags for which uniforms are set
    public readonly UniformFlags Flags;
}

[Flags]
public enum UniformFlags
{
    None = 0,
    Color = 1,
    ModelMatrix = 2,
    ViewMatrix = 4,
    ProjectionMatrix = 8,
    TextureSlot = 16
}

public class DrawCommand
{
    public UniformData Uniforms { get; init; }
}
```

**Pros:**

- ✅ No boxing
- ✅ Value type (stack allocated)
- ✅ Fast to copy/compare
- ✅ Type-safe

**Cons:**

- Not flexible - can't add custom uniforms
- Need to define all possible uniforms upfront

### Solution 3: Explicit Locations + Span (Best Performance)

Require shaders to use explicit locations:

```glsl
// StandardShader.vert
layout(location = 0) uniform mat4 modelMatrix;
layout(location = 1) uniform mat4 viewMatrix;
layout(location = 2) uniform mat4 projectionMatrix;
layout(location = 3) uniform vec4 color;
```

```csharp
// Use Span<T> or fixed-size array for values
public readonly struct UniformValue
{
    public readonly UniformType Type;
    public readonly Vector4D<float> Vec4;      // Overlapped union
    public readonly Matrix4X4<float> Mat4;     // Overlapped union
    public readonly int Int;
    public readonly float Float;
}

public class DrawCommand
{
    // Fixed array of uniform values indexed by location
    // Most shaders use < 8 uniforms
    public UniformValue[] UniformsByLocation { get; init; } = new UniformValue[8];
}
```

**Pros:**

- ✅ No boxing
- ✅ No string lookup
- ✅ Direct indexing by location
- ✅ Fast
- ✅ Immutable if we use ImmutableArray

**Cons:**

- Requires shader location coordination
- Less flexible

### Solution 4: ImmutableDictionary + ReadOnlyMemory (Hybrid)

```csharp
public readonly struct UniformValue
{
    public readonly UniformType Type;
    private readonly ReadOnlyMemory<byte> _data;

    public ReadOnlySpan<byte> Data => _data.Span;
}

public class DrawCommand
{
    // Truly immutable dictionary
    public ImmutableDictionary<string, UniformValue> Uniforms { get; init; }
        = ImmutableDictionary<string, UniformValue>.Empty;
}
```

**Pros:**

- ✅ Actually immutable
- ✅ No boxing (data stored as bytes)
- ✅ Flexible (supports any uniform)

**Cons:**

- ImmutableDictionary allocation overhead
- Still requires location lookup (but can cache in renderer)

## Recommendation

### Phase 1: Quick Win (Do Now)

**Cached locations with struct wrapper**

```csharp
public readonly struct UniformValue
{
    public readonly int Location;      // Cached at ResourceManager level
    public readonly object Value;      // TODO: eliminate boxing in Phase 2

    public UniformValue(int location, object value)
    {
        Location = location;
        Value = value;
    }
}

public class DrawCommand
{
    // ImmutableArray is truly immutable and efficient
    public ImmutableArray<UniformValue> Uniforms { get; init; }
        = ImmutableArray<UniformValue>.Empty;
}
```

**Benefits:**

- Eliminates per-frame glGetUniformLocation calls (60-75% speedup)
- Actually immutable (ImmutableArray)
- Still flexible
- Easy migration from current code

### Phase 2: Eliminate Boxing (Later)

Use discriminated union or type-specific arrays:

```csharp
public readonly struct UniformValue
{
    public readonly int Location;
    public readonly UniformType Type;

    // Discriminated union
    private readonly Vector4D<float> _vec4;
    private readonly Matrix4X4<float> _mat4;
    private readonly float _float;
    private readonly int _int;

    public Vector4D<float> AsVec4() => Type == UniformType.Vec4 ? _vec4 : throw ...;
}
```

### Phase 3: Explicit Locations (Optimal)

Standardize shader locations and use direct indexing:

```csharp
// StandardShaderLocations.cs
public static class UniformLocations
{
    public const int ModelMatrix = 0;
    public const int ViewMatrix = 1;
    public const int ProjectionMatrix = 2;
    public const int Color = 3;
    public const int TextureSlot = 4;
}

public class DrawCommand
{
    // Direct array indexed by location - no lookup needed
    public UniformValue[] UniformsByLocation { get; init; } = new UniformValue[8];
}
```

## Immediate Action

Update DrawCommand to use cached locations:

```csharp
using System.Collections.Immutable;

public readonly struct UniformValue
{
    public readonly int Location;
    public readonly object Value;

    public UniformValue(int location, object value)
    {
        Location = location;
        Value = value;
    }
}

public class DrawCommand
{
    // ... other properties ...

    public ImmutableArray<UniformValue> Uniforms { get; init; }
        = ImmutableArray<UniformValue>.Empty;
}
```

This gives us:

- ✅ True immutability
- ✅ 60-75% performance improvement
- ✅ Easy to extend to eliminate boxing later
- ✅ Maintains flexibility

## Questions to Answer

1. **What OpenGL version are we targeting?**

   - If 4.3+: Use explicit locations (optimal)
   - If 3.3+: Use cached locations (good)

2. **What are the most common uniforms?**

   - If standardized: Can create typed structures
   - If varied: Need flexible system

3. **Do we want to support custom uniforms?**
   - Yes: Keep flexible system with cached locations
   - No: Use typed structs with no boxing

Let me know your answers and I'll implement the optimal solution!
