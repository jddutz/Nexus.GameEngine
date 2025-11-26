# Feature Specification: Fix UserInterfaceElement Implementation

**Feature Branch**: `010-fix-ui-element`
**Created**: 2025-11-23
**Status**: Draft
**Input**: User description: "Now that we've fixed a number of issues with composition and lifecycle, we need to fix the implementation of UserInterfaceElement. This class is the base class for all UI components that occupy screen space. It implements ITransformable and defines GetBounds() which returns a Rectangle<int> defining the limits of the object in screen coordinates. For clarification, we need to discuss the best way to optimize screen-space coordinates."

## User Scenarios & Testing

### User Story 1 - Correct Bounds Calculation (Priority: P1)

As a developer, I need `UserInterfaceElement` to accurately calculate its screen-space bounds so that input events and rendering occur in the correct location.

**Why this priority**: Fundamental for any UI interaction and rendering.

**Independent Test**: Create a `UserInterfaceElement` with known position/size, verify `GetBounds()` returns expected values.

**Acceptance Scenarios**:

1. **Given** a UI element at (10, 10) with size 100x50, **When** `GetBounds()` is called, **Then** it returns a rectangle {X=10, Y=10, Width=100, Height=50}.
2. **Given** a UI element nested inside a parent at (20, 20), **When** the child is at local (10, 10), **Then** `GetBounds()` returns {X=30, Y=30, ...}.

---

### User Story 2 - Transform Support (Priority: P2)

As a developer, I need `UserInterfaceElement` to respect `ITransformable` properties so that I can move and position elements dynamically.

**Why this priority**: Required for dynamic UI layouts and animations.

**Independent Test**: Modify transform properties and verify bounds update.

**Acceptance Scenarios**:

1. **Given** a UI element, **When** its position is changed via `ITransformable` interface, **Then** `GetBounds()` reflects the new position.

### Edge Cases

- **Zero Size**: Elements with 0 width/height should have empty bounds but valid coordinates.
- **Negative Coordinates**: Elements positioned off-screen (negative coordinates) should still report correct bounds.
- **Deep Nesting**: Hierarchy depth > 100 should not cause stack overflow or significant performance degradation.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST introduce a new `RectTransform` component class that inherits from `RuntimeComponent`.
- **FR-002**: `RectTransform` MUST implement a new interface `IRectTransform` designed for 2D spatial manipulation.
- **FR-003**: `IRectTransform` MUST define the following properties:
    - `Vector2D<float> Position`: The position in parent space.
    - `Vector2D<float> Size`: The width and height of the rectangle.
    - `float Rotation`: The rotation in radians (clockwise).
    - `Vector2D<float> Scale`: The scale factor (default 1,1).
    - `Vector2D<float> Pivot`: The normalized center of rotation/positioning (0-1).
- **FR-004**: `UserInterfaceElement` MUST inherit from `RectTransform` to gain spatial capabilities.
- **FR-005**: The Screen Space coordinate system MUST use Top-Left as (0,0), with +X right and +Y down.
- **FR-006**: `RectTransform` MUST support a `Pivot` property (Vector2 0-1) to allow elements to be positioned/rotated relative to any point.
    - **UI Usage**: Defaults to (0,0) [Top-Left] for standard UI layout.
    - **Sprite Usage**: Can be set to (0.5,0.5) [Center] for game objects.
    - **Implementation**: Replaces the legacy `AnchorPoint` (-1 to 1) property.
- **FR-007**: `GetBounds()` MUST calculate the screen-space bounding box by applying the full transform hierarchy (Position, Rotation, Scale, Pivot) recursively.
- **FR-008**: The rendering system MUST be updated to map `Pivot` (0-1) to the vertex shader's coordinate system.
- **FR-009**: The `TexturedQuad` geometry definition MUST be updated to use 0..1 coordinates (Top-Left origin) to match the new coordinate system, breaking backward compatibility with legacy -1..1 logic.
- **FR-010**: The `ui.vert` shader MUST be updated to use the new 0..1 geometry and `Pivot` directly, removing legacy `Anchor` conversion logic.

### Key Entities

- **RectTransform**: The concrete implementation of 2D spatial logic.
- **IRectTransform**: The interface defining 2D spatial capabilities.
- **UserInterfaceElement**: The base UI class, now inheriting `RectTransform`.
- **TexturedQuad**: Shared geometry resource (updated to 0..1).

## Success Criteria

### Measurable Outcomes

- **SC-001**: `GetBounds()` returns correct screen coordinates for a 3-level deep nested hierarchy.
- **SC-002**: `GetBounds()` calculation overhead is minimized (e.g., cached until invalidation).
- **SC-003**: 100% of `UserInterfaceElement` implementations pass bounds verification tests.
