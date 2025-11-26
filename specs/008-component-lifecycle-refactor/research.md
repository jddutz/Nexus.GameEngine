# Research: Component Lifecycle Refactor

## Current Implementation Analysis

### Configurable.cs
- `Load(Template)` calls `OnLoad(Template)`.
- `OnLoad` is virtual.
- `IsLoaded` is set at the end of `Load`.
- `Loading` and `Loaded` events wrap the process.

### TemplateGenerator.cs
- Generates `OnLoad(Template)` override.
- Calls `Load(...)` overload (generated).
- `Load(...)` calls `base.Load(...)` (if not root) then applies properties.
- This causes the interleaving: Base.Load -> Base.OnLoad -> Derived.Load -> Derived.Properties.

### ContentManager.cs
- `Load(Template)` calls `CreateInstance` then `ActivateComponentTree`.
- `ActivateComponentTree` calls `Activate` on `IRuntimeComponent`s.
- No explicit validation phase visible in `Load`.

## Proposed Changes

### 1. Configurable.cs Refactor
- Make `Load(Template)` non-virtual.
- Add `protected virtual void Configure(Template)`.
- `Load` implementation:
  ```csharp
  public void Load(Template template) {
      Loading?.Invoke(this, new(template));
      Configure(template); // Virtual chain
      OnLoad(template);    // Virtual hook
      IsLoaded = true;
      Loaded?.Invoke(this, new(template));
  }
  ```

### 2. TemplateGenerator.cs Updates
- **Generate `Configure` override instead of `OnLoad` override.**
  - `override void Configure(Template t)`
  - Call `base.Configure(t)` first.
  - Apply properties from `t`.
- **Update `Load(...)` convenience method.**
  - Create `Template` from args.
  - Call `this.Load(template)`.
  - Do NOT call `base.Load(...)`.

### 3. ContentManager.cs Refactor
- Move to `Nexus.GameEngine.Runtime`.
- Update `Load` flow:
  - `CreateInstance(template)` (calls `ComponentFactory.Create` -> `Load`).
  - `UpdateLayout(root)` (new phase).
  - `Validate(root)` (new phase).
  - `Activate(root)` (existing phase).

### 4. Element.cs / IUserInterfaceElement
- Define `IUserInterfaceElement` with `UpdateLayout()`.
- Implement in `Element`.

## Impact Analysis

- **Breaking Changes**:
  - `Load(Template)` is no longer virtual. Derived classes overriding it will break. (Search required).
  - `OnLoad` behavior changes (properties are now fully set).
  - `ContentManager` namespace change.
- **Migration**:
  - Update manual overrides of `Load`.
  - Update `using` statements for `ContentManager`.

## Verification Plan

- **Unit Tests**:
  - Verify execution order: Base.Configure -> Derived.Configure -> Base.OnLoad -> Derived.OnLoad.
  - Verify `IsLoaded` state during hooks.
  - Verify `ContentManager` calls `UpdateLayout` before `Validate`.
