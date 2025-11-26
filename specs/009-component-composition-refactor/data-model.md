# Data Model: Component Composition Refactor

**Feature**: Component Composition Refactor
**Branch**: `009-component-composition-refactor`

## Entities

### 1. UserInterfaceElement

**Type**: `RuntimeComponent`, `ITransformable`
**Purpose**: Core UI node handling layout and hierarchy. No rendering logic.

**Properties**:
- `Position` (Vector3D): Local position.
- `Rotation` (Quaternion): Local rotation.
- `Scale` (Vector3D): Local scale.
- `Size` (Vector2D): Layout size (width, height).
- `AnchorPoint` (Vector2D): Alignment relative to parent.
- `Pivot` (Vector2D): Center of rotation/scaling.
- `WorldMatrix` (Matrix4x4): Computed world transform.

**Dependencies**: None (no graphics dependencies).

### 2. SpriteRenderer

**Type**: `RuntimeComponent`, `IDrawable`
**Purpose**: Renders a textured quad using the parent's transform.

**Properties**:
- `Texture` (TextureResource): The image to draw.
- `Color` (Color): Tint color.
- `IsVisible` (bool): Rendering toggle.

**Dependencies**:
- `IGraphicsContext` / `IDescriptorManager`: For texture binding.
- `Parent` (`ITransformable`): For `WorldMatrix`.

### 3. TextRenderer

**Type**: `RuntimeComponent`, `IDrawable`
**Purpose**: Renders text using the parent's transform.

**Properties**:
- `Text` (string): Content.
- `Font` (FontDefinition): Font configuration.
- `Color` (Color): Text color.
- `IsVisible` (bool): Rendering toggle.

**Dependencies**:
- `IFontResourceManager`: For font atlas.
- `Parent` (`ITransformable`): For `WorldMatrix`.

## Relationships

- `UserInterfaceElement` (Parent) -> `SpriteRenderer` (Child)
- `UserInterfaceElement` (Parent) -> `TextRenderer` (Child)
- `SpriteRenderer` uses `Parent.WorldMatrix`
- `TextRenderer` uses `Parent.WorldMatrix`

## Interfaces

### ITransformable (Existing)
- Defines `Position`, `Rotation`, `Scale`, `WorldMatrix`.
- Implemented by `UserInterfaceElement`.

### IDrawable (Existing)
- Defines `GetDrawCommands(RenderContext)`.
- Implemented by `SpriteRenderer`, `TextRenderer`.
