# Feature Specification: Refactor Entity/Component Lifecycle

**Feature Branch**: `008-component-lifecycle-refactor`
**Created**: 2025-11-21
**Status**: Draft
**Input**: User description: "We need to re-examine the overall Entity/Component lifecycle. With the extensive changes to the source generator, we have a problem with OnLoad, Load(template) and Load(...) calling order. The solution needs to call Load from root to leaf. Components need to be fully loaded, including properties from derived classes, before calling OnLoad and raising the Loaded event. Alternatively, we could remove OnLoad and the Loaded event and use OnActivate(), Activating, and Activated events."
**Update**: "I want Load(template) to call Load(...) -> base class triggers Loading, derived classes classes configure after calling base.Load(...). Loading process should be managed by ComponentFactory. Validation and Activation should be managed by ContentManager. We'll need to add an UpdateLayout method to Element which should be called before Validation and Activation by ContentManager. Load() behavior is generated and fixed. Load(template) should call Load(...) and participate in the full lifecycle. This will require completely overhauling the source generator itself."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consistent Template Loading (Priority: P1)

As a developer, when I load a component from a template, I expect the `Load(template)` method to orchestrate the full lifecycle, ensuring properties are applied before hooks run.

**Why this priority**: Unifies the initialization logic, reducing bugs caused by different behaviors between template loading and manual loading.

**Independent Test**: Call `Load(template)` and `Load(...)` with equivalent data. Verify the resulting component state and event sequence are identical.

**Acceptance Scenarios**:

1. **Given** a component, **When** `Load(template)` is called, **Then** `Loading` fires, `Configure` applies properties, `OnLoad` runs, `IsLoaded` becomes true, and `Loaded` fires.
2. **Given** a component, **When** `Load(...)` is called, **Then** it creates a template and calls `Load(template)`, triggering the same sequence.

---

### User Story 2 - Lifecycle Management Separation (Priority: P1)

As a system architect, I want the `ComponentFactory` to handle the Loading process (instantiation and configuration) and the `ContentManager` to handle Validation and Activation, so that responsibilities are clearly separated.

**Why this priority**: Improves architectural clarity and allows for more flexible lifecycle management (e.g., validating a tree before activating it).

**Independent Test**: Mock `ComponentFactory` and `ContentManager`. Verify `ComponentFactory` only calls `Load`, and `ContentManager` calls `Validate` and `Activate`.

**Acceptance Scenarios**:

1. **Given** a component created by `ComponentFactory`, **When** returned, **Then** it is Loaded but NOT Validated or Activated.
2. **Given** a loaded component passed to `ContentManager`, **When** processed, **Then** `UpdateLayout` (if applicable), `Validate`, and `Activate` are called in that order.

---

### User Story 3 - Layout Update Phase (Priority: P2)

As a UI developer, I want `Element` components to have an `UpdateLayout` method called by `ContentManager` before validation, so that layout calculations (which might affect validation state) are performed at the correct time.

**Why this priority**: Ensures UI components are in a valid layout state before their properties are validated.

**Independent Test**: Create an `Element` with a layout dependency. Verify `UpdateLayout` is called before `Validate`.

**Acceptance Scenarios**:

1. **Given** an `Element`, **When** loaded via `ContentManager`, **Then** `UpdateLayout` is called before `Validate` and `Activate`.

---

## Functional Requirements

1.  **Non-Virtual Orchestrator**: `Configurable.Load(Template)` MUST be a **non-virtual** method that orchestrates the loading lifecycle.
2.  **Lifecycle Sequence**: The `Load(Template)` method MUST execute: `Loading` event -> `Configure(Template)` -> `OnLoad(Template)` -> `IsLoaded = true` -> `Loaded` event.
3.  **Configuration Extension**: A `protected virtual void Configure(Template)` method MUST be added to `Configurable`.
4.  **Generated Configuration**: The `TemplateGenerator` MUST generate an `override void Configure(Template)` method that applies properties from the template to the component.
5.  **Base Call in Configure**: The generated `Configure` override MUST call `base.Configure(template)` *before* applying its own properties (Root-to-Leaf order).
6.  **Convenience Wrapper**: The generated `Load(...)` convenience method MUST create a `Template` instance from its arguments and call `this.Load(template)`.
7.  **ComponentFactory Responsibility**: `ComponentFactory` MUST call the non-virtual `Load(Template)` method.
8.  **ContentManager Responsibility**: `ContentManager` MUST be responsible for calling `Validate` and `Activate` (and `UpdateLayout`) on the component tree.
9.  **Layout Update Interface**: A new interface `IUserInterfaceElement` defining `UpdateLayout()` MUST be created. `Element` MUST implement this interface.
10. **ContentManager Location**: `ContentManager` MUST be moved to `Nexus.GameEngine.Runtime` namespace/folder.
11. **Lifecycle Sequence (ContentManager)**: The sequence in `ContentManager` MUST be: `UpdateLayout` (for `IUserInterfaceElement`s) -> `Validate` -> `Activate`.
12. **IsLoaded Semantics**: `IsLoaded` MUST be set to `true` by the `Load` orchestrator after configuration and hooks. It signifies "Configured".

## Success Criteria

1.  **Unified Lifecycle**: Both `Load(Template)` and `Load(...)` trigger the exact same lifecycle events and hooks.
2.  **Correct Property Order**: Properties are applied from Base to Derived via the `Configure` virtual chain.
3.  **Hook Safety**: `OnLoad` hooks run only *after* the entire `Configure` chain is complete, ensuring all properties are set.
4.  **Separation**: `ComponentFactory` only Loads. `ContentManager` Validates and Activates.
5.  **Namespace Organization**: `ContentManager` resides in `Nexus.GameEngine.Runtime`.

## Assumptions

1.  `IUserInterfaceElement` will be defined in a core namespace accessible to `ContentManager`.
2.  Creating a temporary `Template` object in `Load(...)` is acceptable performance-wise (it's a record, so lightweight).

## Key Entities

-   `Configurable` (Base class, Orchestrator)
-   `ComponentFactory`
-   `ContentManager`
-   `Element`
-   `TemplateGenerator`
-   `IUserInterfaceElement`

## Clarifications

*All clarifications resolved.*

