# Coordinate System Analysis: Position and Transform Hierarchy

**Date**: November 16, 2025  
**Purpose**: Document how Position, coordinate systems, and transformations work in the rendering pipeline

---

## Summary: The Three Critical Facts

1. **Position units are PIXELS**, not normalized coordinates (-1 to 1)
   - Position = (100, 200) means 100 pixels right, 200 pixels down from parent origin
   - Position = (-1, -1) means 1 pixel left, 1 pixel up (NOT top-left corner)

2. **Position is in parent's local coordinate space**, not world space
   - Child Position is relative to parent's coordinate system
   - Root element Position is in world space (centered at screen center)

3. **Position range is NOT bounded** to [-1, 1] or any specific values
   - For 1280x720 screen: visible range is approximately -640 to 640 (X), -360 to 360 (Y)
   - Position can exceed visible range (element renders off-screen)
   - Size is also in pixels (width, height)

---

## Key Discovery: Position is in Parent's Local Space

From `Transformable.cs`:

```csharp
protected virtual void UpdateWorldMatrix()
{
    // Hierarchical transform composition: WorldMatrix = ChildLocal * ParentWorld
    // With S*R*T matrices, this order correctly transforms the child's local position
    // by the parent's rotation, then adds the parent's world position
    if (Parent is ITransformable parentTransform)
        _worldMatrix = LocalMatrix * parentTransform.WorldMatrix;
    else
        _worldMatrix = LocalMatrix; // Root object: local space = world space
}
```

**Critical Insight**: Position is defined **in the parent's local coordinate space**, NOT in world/screen space.

- If an element has no parent: `WorldMatrix = LocalMatrix` → Position is in world space
- If an element has a parent: `WorldMatrix = LocalMatrix * ParentWorldMatrix` → Position is in parent's local space

---

## Position Units: PIXELS, Not Normalized Coordinates

### Critical Answer: Position is in PIXELS

**If you set Position = (-1, -1), that means**: Shift by **1 pixel** up and to the left (from parent origin)

**NOT**: Move to normalized corner at (-1, -1)

### Proof from StaticCamera

The projection matrix transforms **pixel coordinates** to normalized device coordinates (NDC):

```csharp
/// Creates a projection matrix that transforms pixel coordinates (centered at origin)
/// to normalized device coordinates. Origin (0,0) is at the center of the viewport,
/// with coordinates ranging from (-width/2, -height/2) to (width/2, height/2).

ProjectionMatrix = Matrix4X4.CreateOrthographicOffCenter(
    -_viewportWidth / 2f,   // left edge in pixels
    _viewportWidth / 2f,    // right edge in pixels
    -_viewportHeight / 2f,  // bottom edge in pixels
    _viewportHeight / 2f,   // top edge in pixels
    _nearPlane,
    _farPlane);
```

**For a 1280x720 window**:
- Projection maps pixel range [-640, 640] (X) and [-360, 360] (Y) to NDC range [-1, 1]
- Position = (0, 0) → screen center (world origin)
- Position = (-640, -360) → top-left corner of screen
- Position = (640, 360) → bottom-right corner of screen
- Position = (-1, -1) → **1 pixel left and 1 pixel up from parent origin**

### Shader Pipeline Confirmation

```glsl
// 1. Shader computes local position from quad vertices
vec2 xy = (inPos - anchor) * size * 0.5;  // Result in PIXELS
vec4 p = vec4(xy, 0, 1);

// 2. Apply world transform (Position is in pixels)
// world matrix contains Position as translation in PIXELS
vec4 worldPos = world * p;  // worldPos is in PIXELS

// 3. Apply view-projection (converts PIXELS to NDC)
gl_Position = camera.viewProjection * worldPos;  // Final NDC coordinates
```

**The complete transform**:
1. Quad vertices (-1 to 1) → scaled by Size/2 and offset by AnchorPoint → **pixel offsets**
2. World matrix translates by Position → **pixel coordinates in parent space**
3. ViewProjection matrix converts **pixels to NDC** for GPU

### Concrete Examples

**Example 1: Center of screen**
```
Position = (0, 0)
Size = (100, 50)
AnchorPoint = (0, 0)  // center

Quad vertex at inPos=(0, 0):
xy = (0 - 0) * (100, 50) * 0.5 = (0, 0) pixels
World position = Position + xy = (0, 0) + (0, 0) = (0, 0) pixels
→ Renders at screen center
```

**Example 2: Top-left corner of screen (1280x720)**
```
Position = (-640, -360)
Size = (100, 50)
AnchorPoint = (-1, -1)  // top-left

Quad vertex at inPos=(-1, -1):
xy = (-1 - (-1)) * (100, 50) * 0.5 = (0, 0) pixels
World position = (-640, -360) + (0, 0) = (-640, -360) pixels
→ Renders at top-left corner of screen
```

**Example 3: Moving 1 pixel**
```
Position = (-1, -1)
Size = (100, 50)  
AnchorPoint = (0, 0)

This places the element's CENTER at 1 pixel left and 1 pixel up from parent origin
NOT at the screen corner!
```

### Range of Position Values

**Position is NOT bounded to [-1, 1]** — it can be any pixel value!

For 1280x720 screen with centered coordinate system:
- **Visible range**: approximately -640 to 640 (X), -360 to 360 (Y)
- **Can exceed**: Position can be outside visible range (off-screen)
- **Units**: Always pixels, relative to parent's coordinate system

**Nested elements**:
- Parent Position = (100, 50) pixels
- Child Position = (20, 10) pixels  
- Child renders at: Parent's world position + (20, 10) pixels in parent's local space

---

## Coordinate System Hierarchy

### World Space (Root Level)

The **StaticCamera** defines the world coordinate system for UI:

```csharp
/// Origin (0,0) is at the center of the viewport,
/// with coordinates ranging from (-width/2, -height/2) to (width/2, height/2).
```

For a 1280x720 window:
- Center: (0, 0)
- Top-left: (-640, -360)
- Bottom-right: (640, 360)

### Root Element Constraints

From `Application.cs`:

```csharp
// Use centered coordinate system to match StaticCamera's viewport
var constraints = new Rectangle<int>(-window.Size.X / 2, -window.Size.Y / 2, window.Size.X, window.Size.Y);
rootElement.SetSizeConstraints(constraints);
```

The root element receives a Rectangle with:
- **Origin**: (-width/2, -height/2) — top-left in world space
- **Size**: (width, height) — dimensions
- **Center**: (0, 0) — calculated from Origin + Size/2

### Current Element.OnSizeConstraintsChanged Behavior

```csharp
var posX = constraints.Center.X + AnchorPoint.X * constraints.HalfSize.X;
var posY = constraints.Center.Y + AnchorPoint.Y * constraints.HalfSize.Y;
SetPosition(new Vector3D<float>(posX, posY, Position.Z));
```

**This positions the element so its AnchorPoint aligns with the constraints.Center**

---

## Rectangle Semantics

A `Rectangle<int>` provides:

```csharp
public Vector2D<T> Origin { get; init; }    // Top-left corner
public Vector2D<T> Size { get; init; }      // Width and height
public Vector2D<T> Center => Origin + Size * 0.5f;
public Vector2D<T> HalfSize => Size * 0.5f;
```

**Rectangle does NOT directly specify Position** — it specifies:
1. **Where the space is** (Origin)
2. **How big the space is** (Size)
3. **Center is calculated** from Origin + Size/2

---

## How Element Interprets SetSizeConstraints

Given `SetSizeConstraints(Rectangle<int> constraints)`:

### Step 1: Determine Element Size
Based on SizeMode (Fixed, Relative, Absolute, FitContent), element calculates its own Size.

### Step 2: Calculate Position

**Current implementation**:
```csharp
var posX = constraints.Center.X + AnchorPoint.X * constraints.HalfSize.X;
var posY = constraints.Center.Y + AnchorPoint.Y * constraints.HalfSize.Y;
```

**This formula means**:
- Position is placed at constraints.Center
- Then offset by AnchorPoint * HalfSize

**Example**: constraints.Center = (0, 0), constraints.HalfSize = (640, 360), AnchorPoint = (-1, -1)
```
posX = 0 + (-1) * 640 = -640
posY = 0 + (-1) * 360 = -360
Position = (-640, -360)  // Top-left of the constraints rectangle
```

### Step 3: Rendering

When rendered, the shader applies:
```glsl
vec2 xy = (inPos - anchor) * size * 0.5;
gl_Position = camera.viewProjection * world * vec4(xy, 0, 1);
```

With:
- `world` = WorldMatrix (contains Position translation)
- `anchor` = AnchorPoint
- `size` = Size
- `inPos` = vertex position from quad geometry (ranges from -1 to 1)

---

## Concrete Example

**Setup**:
- Window: 1280x720
- Root constraints: Origin=(-640, -360), Size=(1280, 720), Center=(0, 0)
- Element: Size=(200, 100), AnchorPoint=(-1, -1) [top-left]

**OnSizeConstraintsChanged calculates**:
```
posX = 0 + (-1) * 640 = -640
posY = 0 + (-1) * 360 = -360
Position = (-640, -360)
```

**Shader transforms vertices**:
For quad vertex at inPos=(-1, -1) [top-left of quad]:
```
xy = ((-1) - (-1)) * (200, 100) * 0.5 = (0) * (200, 100) * 0.5 = (0, 0)
```

For quad vertex at inPos=(1, 1) [bottom-right of quad]:
```
xy = ((1) - (-1)) * (200, 100) * 0.5 = (2) * (200, 100) * 0.5 = (200, 100)
```

**WorldMatrix includes Position=(-640, -360)**, so:
- Top-left vertex at world position: (-640, -360) + (0, 0) = **(-640, -360)**
- Bottom-right vertex at world position: (-640, -360) + (200, 100) = **(-440, -260)**

**Result**: Element renders from (-640, -360) to (-440, -260) — top-left corner of the screen!

---

## Key Questions Answered

### Q: Is Position in parent's local space or world space?

**Answer**: **Parent's local space**

- If parent exists: Position is relative to parent's coordinate system
- If no parent (root): Position is in world space (centered coordinate system for UI)

### Q: How does Position affect WorldMatrix?

**Answer**: Position is the Translation component of the SRT (Scale-Rotation-Translation) matrix

```csharp
_localMatrix = Matrix4X4.CreateScale(_scale) *
               Matrix4X4.CreateFromQuaternion(_rotation) *
               Matrix4X4.CreateTranslation(_position);  // <-- Position goes here
```

Then:
```csharp
_worldMatrix = LocalMatrix * parentTransform.WorldMatrix;  // Hierarchical composition
```

### Q: What does Rectangle.Origin represent?

**Answer**: **Top-left corner of the available space in the parent's coordinate system**

It does NOT directly represent the element's Position. The element must decide where to position itself within the rectangle.

### Q: What should Position be set to?

**Answer**: **Depends on desired behavior!** Current implementation:

```csharp
Position = constraints.Center + AnchorPoint * constraints.HalfSize
```

This positions the element so its AnchorPoint aligns with the center of the constraints rectangle.

**Alternative approaches**:
1. `Position = constraints.Origin` — element's origin at rectangle's top-left
2. `Position = constraints.Center` — element's center at rectangle's center
3. Custom calculation based on alignment rules

---

## Implications for Container Layout

### Current Container Behavior

Containers (HorizontalLayout, VerticalLayout) calculate a Rectangle for each child:

```csharp
var childConstraints = new Rectangle<int>(x, contentArea.Origin.Y, w, h);
child.SetSizeConstraints(childConstraints);
```

The child then positions itself using its AnchorPoint within those constraints.

### Problem

**Two children with same constraints but different AnchorPoints render at different locations!**

Example:
- Constraints: Origin=(100, 200), Size=(100, 50), Center=(150, 225)
- Child A: AnchorPoint=(-1, -1) → Position=(100, 200)
- Child B: AnchorPoint=(0, 0) → Position=(150, 225)
- Different render locations even though both got same constraints!

### Solutions

**Option 1**: Container sets child AnchorPoint to standard value (e.g., (-1, -1) or (0, 0))
- Ensures consistent positioning
- Overrides child's configured AnchorPoint

**Option 2**: Container calculates constraints.Origin accounting for child's AnchorPoint
- Child keeps its AnchorPoint
- Container reverse-engineers what rectangle produces desired render location
- Complex but respects child configuration

**Option 3**: Container directly sets Position (bypass SetSizeConstraints)
- Container has full control
- Breaks existing protocol
- Requires new API

---

## Recommendation for Spec

The spec needs to clarify:

1. **What does alignment mean**: Position of element's visual bounds or Position property value?
2. **Should containers respect child AnchorPoint** or override it?
3. **What is the relationship** between constraints.Origin and desired Position?
4. **How should nested containers work** given Position is in parent's local space?

These are the core questions that must be answered before we can finalize requirements.
