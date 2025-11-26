# Feature Specification: Component Composition Refactor

**Feature Branch**: `009-component-composition-refactor`  
**Created**: 2025-11-23  
**Status**: Draft  
**Input**: User description: "Refactor component system to use composition over inheritance for UI, seal RuntimeComponent, separate ITransformable, and implement Renderer components."

## Clarifications

### Session 2025-11-23
- Q: In the new composition model, what is the hierarchical relationship between the Layout component (`Element`) and the Visual component (`SpriteRenderer`)? → A: **Parent-Child**: `SpriteRenderer` is a child of `UserInterfaceElement`.
- Q: With `ITransformable` becoming read-only (to support `UserInterfaceElement` which is layout-driven), how should 3D `Transformable` components expose mutation (move/rotate/scale)? → A: **Leave As-Is**: `ITransformable` will remain unchanged (including mutation methods). `UserInterfaceElement` will implement the full interface, treating 2D layout as a specialized 3D transform (ViewProjection difference only).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create Standard UI Elements (Priority: P1)

A developer creates standard UI elements (Buttons, Images) using the new composition-based system.

**Why this priority**: This is the core validation that the refactor works for the primary use case (UI construction) and replaces the old inheritance model.

**Independent Test**: Can instantiate a "Button" entity (composed of UserInterfaceElement + SpriteRenderer + TextRenderer) and see it rendered on screen with correct layout and visuals.

**Acceptance Scenarios**:

1. **Given** a new scene, **When** a developer instantiates a "Button" template, **Then** the entity has an `UserInterfaceElement` component (for layout) and a `SpriteRenderer` component (for visuals).
2. **Given** the button is active, **When** the frame renders, **Then** the `SpriteRenderer` draws the button texture at the position defined by the `UserInterfaceElement`.
3. **Given** the button is active, **When** the layout updates, **Then** the `UserInterfaceElement` updates its position, and the `SpriteRenderer` uses this new position for drawing.

---

### User Story 2 - Custom Visual Components (Priority: P2)

A developer creates a custom visual component (e.g., a specialized health bar) and attaches it to a UI element.

**Why this priority**: Validates the flexibility of the composition model—separating logic/layout from rendering.

**Independent Test**: Can create a custom class implementing `IDrawable` and attach it to an `UserInterfaceElement`, resulting in custom rendering behavior without modifying the `UserInterfaceElement` class.

**Acceptance Scenarios**:

1. **Given** a custom `HealthBarRenderer` component, **When** attached to an `UserInterfaceElement`, **Then** it receives the `IGraphicsContext` (or specific managers) via injection.
2. **Given** the renderer is active, **When** the `UserInterfaceElement` resizes, **Then** the renderer adapts its drawing based on the `UserInterfaceElement`'s dimensions.

---

### User Story 3 - Non-Visual UI Elements (Priority: P3)

A developer creates a container element that handles layout but has no visual representation.

**Why this priority**: Ensures that the "tax" of rendering dependencies is removed from non-visual components.

**Independent Test**: Can create a `Container` element that organizes children but has no `IDrawable` component and triggers no rendering logic/overhead.

**Acceptance Scenarios**:

1. **Given** a `Container` element, **When** inspected, **Then** it does NOT have a `DescriptorManager` or `PipelineManager` dependency.
2. **Given** the container is active, **When** the frame renders, **Then** no draw commands are issued for the container itself (only potentially for its children).

### Edge Cases

- **Missing Renderer**: What happens if an `UserInterfaceElement` has no `IDrawable` component? (Should just be invisible/layout-only).
- **Multiple Renderers**: What happens if an entity has multiple `IDrawable` components? (Should render all, potentially z-fighting if not managed).
- **Transform Updates**: How does the `SpriteRenderer` know the `UserInterfaceElement` moved? (Should read `Parent` transform during `GetDrawCommands` or update).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `RuntimeComponent` class MUST be sealed (or effectively restricted) to prevent deep inheritance hierarchies for game logic.
- **FR-002**: The `UserInterfaceElement` class MUST implement `ITransformable` to provide spatial data (Position, Rotation, Scale) to the rendering system, mapping 2D layout concepts to 3D matrices.
- **FR-003**: The `UserInterfaceElement` class MUST NOT inherit from `DrawableElement` or implement `IDrawable`.
- **FR-004**: The `UserInterfaceElement` class MUST NOT have dependencies on `IDescriptorManager`, `IPipelineManager`, or `IResourceManager`.
- **FR-005**: A new `SpriteRenderer` component MUST be implemented that implements `IDrawable` and handles texture rendering for UI elements.
- **FR-006**: A new `TextRenderer` component MUST be implemented that implements `IDrawable` and handles text rendering.
- **FR-007**: The `IDrawable` interface MUST be removed from the base `UserInterfaceElement` hierarchy and used only on leaf rendering components.
- **FR-008**: Rendering components (`SpriteRenderer`, etc.) MUST accept necessary graphics dependencies (e.g., `IGraphicsContext` or individual managers) via constructor injection.
- **FR-009**: Existing UI templates (Button, Label, etc.) MUST be updated to construct entities composed of `UserInterfaceElement` + appropriate Renderer components.
- **FR-010**: The `UserInterfaceElement` class MUST implement the existing `ITransformable` interface fully, supporting standard 3D transformations (Position, Rotation, Scale) to allow unified rendering and animation logic.
- **FR-011**: `IDrawable` implementations MUST query their parent's `ITransformable` interface (e.g. `UserInterfaceElement`) to obtain the `WorldMatrix` for rendering, rather than relying on inheritance.
- **FR-012**: The `IComponent` interface and `Component` base class MUST expose a `Parent` property (returning `IComponent?`) to provide low-overhead access to the immediate parent, bypassing the need for `GetParent<T>()` in performance-critical paths.

### Key Entities

- **UserInterfaceElement**: The core UI node, handling layout, hierarchy, and input hit-testing.
- **SpriteRenderer**: A component responsible for drawing a textured quad using the transform of its parent Entity.
- **TextRenderer**: A component responsible for drawing text using the transform of its parent Entity.
- **ITransformable**: An interface defining the contract for any object that has a position in the world and can be rendered.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `UserInterfaceElement` class constructor has 0 references to Vulkan-specific types (Descriptor/Pipeline managers).
- **SC-002**: A standard "Button" entity can be instantiated and rendered identical to the previous implementation.
- **SC-003**: Unit tests for `UserInterfaceElement` layout logic can run without mocking any graphics interfaces.
- **SC-004**: The inheritance depth for a standard UI element (e.g., Button) is reduced (no longer inherits `DrawableElement` -> `Transformable` -> `RuntimeComponent` in a rigid chain, but uses composition).
- **SC-005**: `ITransformable` is successfully implemented by `UserInterfaceElement` (2D) and `Transformable` (3D) without forcing 3D logic into the 2D implementation beyond the necessary matrix projection.
