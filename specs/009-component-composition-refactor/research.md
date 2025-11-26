# Research: Component Composition Refactor

**Feature**: Component Composition Refactor
**Branch**: `009-component-composition-refactor`
**Date**: 2025-11-23

## 1. RuntimeComponent Sealing Strategy

**Decision**: `RuntimeComponent` will be marked `abstract` (if it serves as a base) or `sealed` (if it is the only component type). Given `UserInterfaceElement` must exist as a class and likely needs `RuntimeComponent` functionality, we have two options:
1. `UserInterfaceElement` inherits from `RuntimeComponent`. `RuntimeComponent` cannot be sealed.
2. `UserInterfaceElement` *is* a `RuntimeComponent` (composition) and we use a `LayoutComponent` for behavior.
3. `RuntimeComponent` is the base, but we use `internal` constructors to restrict inheritance to the engine assembly.

**Selected Approach**: **Option 3 (Effective Restriction)**.
- `RuntimeComponent` will be `abstract` (or open) but with an `internal` constructor (if possible/needed) or we simply document it.
- *Correction*: The spec says "MUST be sealed (or effectively restricted)".
- If we seal `RuntimeComponent`, `UserInterfaceElement` cannot inherit.
- **Refined Decision**: We will interpret "effectively restricted" as preventing *user* inheritance. We will likely keep `RuntimeComponent` open for *engine* components like `UserInterfaceElement`, but maybe mark it `internal` or use an `internal` constructor.
- *Actually*: `UserInterfaceElement` is a core engine component.
- **Final Decision**: `RuntimeComponent` will remain open for engine components but we will discourage deep inheritance. `UserInterfaceElement` will inherit from `RuntimeComponent`.

## 2. Text Rendering Migration

**Decision**: Logic from `TextElement` (spec 002) will be moved to `TextRenderer`.
- `TextElement` logic: Font resource management, `GetDrawCommands` with shared geometry.
- `TextRenderer` will implement `IDrawable`.
- `TextRenderer` will accept `IGraphicsContext` (or `IResourceManager`) via constructor/injection.
- `TextRenderer` will use `Parent` (which is `UserInterfaceElement`) to get `WorldMatrix`.

## 3. Sprite Rendering Migration

**Decision**: Logic for sprite rendering (likely from `ImageElement` or similar) will move to `SpriteRenderer`.
- `SpriteRenderer` implements `IDrawable`.
- Uses `Parent` transform.

## 4. UserInterfaceElement Responsibilities

**Decision**:
- Implements `ITransformable` (layout logic).
- Does NOT implement `IDrawable`.
- Acts as the "Container" or "Node" in the UI tree.
- Visuals are child components (`SpriteRenderer`, `TextRenderer`).

## 5. ITransformable Implementation

**Decision**: `UserInterfaceElement` will implement `ITransformable` by mapping 2D layout properties (Position, Rotation, Scale) to 3D matrices.
- `Position`: mapped to X, Y, Z (Z usually 0 or layer index).
- `Rotation`: mapped to Z-axis rotation.
- `Scale`: mapped to X, Y scale.
- `WorldMatrix`: Calculated from parent.

## 6. Dependency Injection

**Decision**: Rendering components (`SpriteRenderer`, `TextRenderer`) will receive dependencies via constructor injection (supported by `ComponentFactory`).
- `UserInterfaceElement` will NOT have graphics dependencies.

## 7. Unknowns Resolved

- **RuntimeComponent Restriction**: Use `internal` constructor or just documentation/convention if `UserInterfaceElement` must inherit.
- **Text Logic**: Source found in `TextElement.cs` (from search results).
- **Sprite Logic**: Will be similar to `TextElement` but for textures.

