# Research: Fix UserInterfaceElement Implementation

## Unknowns & Clarifications

### 1. Coordinate System Optimization
**Question**: What is the best way to optimize screen-space coordinates?
**Finding**: The spec mandates Top-Left (0,0) with +X right and +Y down. This is standard for UI systems.
**Decision**: Update `TexturedQuad` geometry to use 0..1 coordinates (Top-Left origin) instead of the legacy -1..1 (Center origin). This simplifies layout calculations and aligns with the `RectTransform` pivot system.
**Rationale**: 0..1 geometry allows `size` scaling to directly map to screen pixels without offset calculations. It also makes `Pivot` (0..1) logic intuitive.

### 2. Bounds Calculation Overhead
**Question**: How to minimize `GetBounds()` calculation overhead?
**Finding**: The `[ComponentProperty]` system generates partial methods like `OnPositionChanged`.
**Decision**: Implement a dirty flag system in `RectTransform`.
- Add `private bool _isBoundsDirty = true;` 
- Add `private Rectangle<int> _cachedBounds;` 
- Implement partial methods `OnPositionChanged`, `OnSizeChanged`, `OnRotationChanged`, `OnScaleChanged`, `OnPivotChanged` to set `_isBoundsDirty = true`.
- `GetBounds()` checks `_isBoundsDirty`. If true, recalculates and caches.
**Rationale**: Prevents re-calculation on every frame/access if properties haven't changed.

### 3. Shader Compatibility
**Question**: Does `ui.vert` need major changes for 0..1 geometry?
**Finding**: The current shader logic is `vec4 p = vec4((inPos - pivot) * size, 0, 1);`.
- If `inPos` is 0..1 (from new `TexturedQuad`) and `pivot` is 0..1:
    - `inPos - pivot` range is [-1, 1].
    - Multiplied by `size`, it correctly scales around the pivot.
**Decision**: The shader logic is mathematically correct for the new system. Only the `TexturedQuad` definition needs changing.
**Rationale**: Verified by mental model of vertex transformation.

## Technology Choices

### RectTransform Component
**Decision**: Create `RectTransform` inheriting from `RuntimeComponent` and implementing `IRectTransform`.
**Rationale**: Separates spatial logic from generic component logic. `UserInterfaceElement` will inherit from `RectTransform`.

### Source Generation
**Decision**: Use `[ComponentProperty]` for all transform properties (`Position`, `Size`, `Rotation`, `Scale`, `Pivot`).
**Rationale**: Leverages existing engine capabilities for animation and deferred updates.

## Alternatives Considered

### Alternative 1: Keep -1..1 Geometry
- **Pros**: No change to `TexturedQuad`.
- **Cons**: Requires offset math in every UI calculation to convert Top-Left (0,0) to Center (0,0).
- **Verdict**: Rejected. 0..1 is more natural for UI.

### Alternative 2: Calculate Bounds in Shader
- **Pros**: CPU offload.
- **Cons**: Input system needs bounds on CPU for hit testing.
- **Verdict**: Rejected. CPU needs bounds for interaction.
