# Feature Specification: Component Base Class Consolidation

**Feature Branch**: `014-component-consolidation`  
**Created**: December 6, 2025  
**Status**: Draft  
**Input**: User description: "Consolidate the component base classes into one Component class. This will involve merging the following into one class using partial class declarations: Entity -> Component.Identity, Configurable -> Component.Configuration, Component -> Component.Graph, RuntimeComponent -> Component.Runtime. I'm not sure if we should merge the interfaces, or keep them separate, or if we can apply separate interfaces to each partial class definition. This needs to be clarified. This is a major refactoring, since we'll have references to these classes and interfaces throughout the codebase."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unified Component Class Structure (Priority: P1)

Developers work with a single `Component` class that organizes functionality into logical partial classes (`Component.Identity.cs`, `Component.Configuration.cs`, `Component.Graph.cs`, `Component.Runtime.cs`) instead of a deep inheritance hierarchy (`Entity` -> `Configurable` -> `Component` -> `RuntimeComponent`).

**Why this priority**: Core architectural change that enables all other benefits. Without this consolidation, the codebase maintains unnecessary complexity and inheritance depth.

**Independent Test**: Can be fully tested by creating a new component that uses identity, configuration, graph, and runtime features, verifying all functionality works identically to the current inheritance-based approach.

**Acceptance Scenarios**:

1. **Given** existing component types that inherit from `RuntimeComponent`, **When** the consolidation is complete, **Then** all component types compile and function identically using the new `Component` base class
2. **Given** a developer creating a new component, **When** they inherit from `Component`, **Then** they have access to all identity, configuration, graph, and runtime functionality without navigating multiple base classes
3. **Given** the consolidated `Component` class, **When** examining the class definition, **Then** the functionality is organized into four partial class files with clear separation of concerns

---

### User Story 2 - Preserved and Improved Interface Contracts (Priority: P2)

Components maintain their granular interface contracts (`IEntity`, `ILoadable`, `IValidatable`, `IComponentHierarchy`, `IActivatable`, `IUpdatable`) with a new unified `IComponent` interface that combines all concerns. The previous `IComponent` is renamed to `IComponentHierarchy` to better reflect parent-child relationships beyond simple trees. The previous `IRuntimeComponent` is split into `IActivatable` (setup/teardown) and `IUpdatable` (frame updates) for better separation of lifecycle concerns.

**Why this priority**: Maintains backward compatibility while improving interface naming clarity and separation of concerns. Enables both granular interface usage (for specific concerns) and unified interface usage (for general component handling). Splitting runtime lifecycle allows systems to depend only on the lifecycle phases they need.

**Independent Test**: Can be fully tested by verifying that components can be cast to any constituent interface and that the unified `IComponent` interface provides access to all component functionality.

**Acceptance Scenarios**:

1. **Given** a `Component` instance, **When** casting to `IEntity`, **Then** identity functionality is accessible
2. **Given** a `Component` instance, **When** casting to `IComponentHierarchy`, **Then** parent-child relationship management functionality is accessible
3. **Given** a `Component` instance, **When** casting to `IActivatable`, **Then** activation/deactivation lifecycle functionality is accessible
4. **Given** a `Component` instance, **When** casting to `IUpdatable`, **Then** frame-by-frame update functionality is accessible
5. **Given** a `Component` instance, **When** casting to the unified `IComponent`, **Then** all component functionality is accessible
6. **Given** code using the old `IComponent` or `IRuntimeComponent` interfaces, **When** updated to use new interface names, **Then** functionality remains identical

---

### User Story 3 - Simplified Codebase Navigation (Priority: P3)

Developers navigate component functionality more easily by examining logical partial classes instead of traversing a four-level inheritance hierarchy.

**Why this priority**: Developer experience improvement that reduces cognitive load and onboarding time. Not critical for functionality but valuable for maintainability.

**Independent Test**: Can be fully tested by measuring time to locate specific functionality (e.g., "Find where Name property is defined") before and after consolidation.

**Acceptance Scenarios**:

1. **Given** a developer looking for identity-related functionality, **When** they open `Component.Identity.cs`, **Then** they find all identity features in one file
2. **Given** a developer looking for lifecycle functionality, **When** they open `Component.Runtime.cs`, **Then** they find all runtime features in one file
3. **Given** a developer examining component capabilities, **When** they view the `Component` class, **Then** they see a flat structure with clear partial class organization instead of navigating base classes

---

### Edge Cases

- What happens when source generators reference the old class names (`Entity`, `Configurable`, `RuntimeComponent`) or old interface names (`IComponent`, `IRuntimeComponent`)?
- What happens when existing components inherit from intermediate classes (e.g., custom base class inheriting from `Configurable` but not `Component`)?
- What happens when reflection code uses type checks against the old class or interface names?
- How are generic constraints that specify `where T : RuntimeComponent`, `where T : IComponent`, or `where T : IRuntimeComponent` handled?
- What happens when serialization/deserialization relies on old type names?
- What happens when existing code has variables or fields named `IComponent` that would conflict with the new unified interface?
- How are method overloads that differ only by interface parameter type resolved after renaming?
- What happens when code depends on `IRuntimeComponent` but only needs activation (not updates) or vice versa - does splitting into `IActivatable` and `IUpdatable` break those assumptions?
- How are property bindings that reference component lifecycle events affected by interface reorganization?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST consolidate `Entity`, `Configurable`, `Component`, and `RuntimeComponent` classes into a single `Component` class using partial class declarations
- **FR-002**: System MUST organize consolidated functionality into four partial class files: `Component.Identity.cs`, `Component.Configuration.cs`, `Component.Hierarchy.cs`, and `Component.Lifecycle.cs`
- **FR-003**: System MUST preserve all existing functionality from the four base classes without behavior changes
- **FR-004**: System MUST rename existing `IComponent` interface to `IComponentHierarchy` to better reflect parent-child relationships that extend beyond simple tree structures
- **FR-005**: System MUST split existing `IRuntimeComponent` interface into two focused interfaces: `IActivatable` (activation/deactivation lifecycle) and `IUpdatable` (frame-by-frame update cycle)
- **FR-006**: System MUST create a new unified `IComponent` interface that combines all component concerns (`IEntity`, `ILoadable`, `IValidatable`, `IComponentHierarchy`, `IActivatable`, `IUpdatable`)
- **FR-007**: System MUST implement the unified `IComponent` interface on the consolidated `Component` class
- **FR-008**: System MUST allow components to be resolved as any of their constituent interfaces (`IEntity`, `ILoadable`, `IValidatable`, `IComponentHierarchy`, `IActivatable`, `IUpdatable`, or the unified `IComponent`)
- **FR-009**: System MUST update all references to old interface names throughout the codebase to use the new interface names
- **FR-010**: System MUST update all references to old class names throughout the codebase to use the new `Component` class
- **FR-011**: System MUST update source generators that reference old class or interface names to use the new structures
- **FR-012**: System MUST update all unit tests to reference the new `Component` class and interface structure
- **FR-013**: System MUST update all integration tests to verify functionality remains unchanged
- **FR-014**: System MUST maintain all existing events, properties, and methods with identical signatures
- **FR-015**: System MUST preserve all XML documentation comments in their respective partial class files
- **FR-016**: System MUST update type constraints in generic methods that reference old class or interface names

### Key Entities

- **Component.Identity**: Contains identity-related functionality (Id, Name, ApplyUpdates) formerly in `Entity` class, implements `IEntity` interface
- **Component.Configuration**: Contains configuration and validation functionality (Load, Validate, IsValid, events) formerly in `Configurable` class, contributes to `ILoadable` and `IValidatable` interfaces
- **Component.Hierarchy**: Contains parent-child relationship management (Parent, Children, AddChild, RemoveChild, navigation) formerly in `Component` class, implements `IComponentHierarchy` interface (renamed from `IComponent`)
- **Component.Lifecycle**: Contains runtime lifecycle functionality (Activate, Update, Deactivate, lifecycle events) formerly in `RuntimeComponent` class, implements `IActivatable` and `IUpdatable` interfaces (split from `IRuntimeComponent`)
- **IComponentHierarchy**: Renamed from `IComponent`, represents parent-child relationship capabilities beyond simple tree structures (accounts for property bindings creating non-tree relationships)
- **IActivatable**: Split from `IRuntimeComponent`, represents activation/deactivation lifecycle phase (setup/teardown, property binding activation, event subscriptions)
- **IUpdatable**: Split from `IRuntimeComponent`, represents frame-by-frame update lifecycle phase (temporal updates, child propagation)
- **IComponent (unified)**: New composite interface that inherits from `IEntity`, `ILoadable`, `IValidatable`, `IComponentHierarchy`, `IActivatable`, and `IUpdatable`, providing a single interface for all component functionality
- **IComponent (unified)**: New composite interface that inherits from `IEntity`, `ILoadable`, `IValidatable`, `IComponentHierarchy`, `IActivatable`, and `IUpdatable`, providing a single interface for all component functionality

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All existing unit tests pass without modification to test logic (only class name updates)
- **SC-002**: All existing integration tests pass without behavior changes
- **SC-003**: Codebase compiles without errors after all references are updated
- **SC-004**: Code review confirms functionality is organized logically across four partial class files
- **SC-005**: Zero runtime behavior changes - all components function identically to before consolidation
- **SC-006**: Developer documentation accurately reflects new class structure and migration guide
- **SC-007**: Source generators produce identical output for components using new base class
- **SC-008**: Systems depending only on activation lifecycle can use `IActivatable` interface independently
- **SC-009**: Systems depending only on update lifecycle can use `IUpdatable` interface independently

## Assumptions

- **Assumption 1**: C# partial classes support applying multiple interfaces across different partial class files (interface implementations are combined)
- **Assumption 2**: Existing code using concrete class types (`Entity`, `Configurable`, `Component`, `RuntimeComponent`) is limited and can be identified through compilation errors
- **Assumption 3**: Renaming `IComponent` to `IComponentHierarchy` and splitting `IRuntimeComponent` into `IActivatable` and `IUpdatable` will not cause naming conflicts with existing code
- **Assumption 4**: Source generators can be updated to reference new class and interface names without architectural changes
- **Assumption 5**: No external libraries or plugins depend on the specific class or interface names
- **Assumption 6**: The consolidation maintains the same namespace (`Nexus.GameEngine.Components`)
- **Assumption 7**: Interface inheritance hierarchy (`IComponent` inheriting from all constituent interfaces) is acceptable for the unified interface pattern
- **Assumption 8**: Splitting `IRuntimeComponent` into `IActivatable` and `IUpdatable` will not break existing code that depends on the combined interface (code can be updated to use unified `IComponent` or both interfaces)

## Dependencies

- **Dependency 1**: Source generator updates must be completed before codebase compilation can succeed
- **Dependency 2**: Unit test updates depend on completing the class consolidation
- **Dependency 3**: Integration test validation depends on successful compilation and unit test passage

## Scope

### In Scope

- Consolidating four base classes (`Entity`, `Configurable`, `Component`, `RuntimeComponent`) into one `Component` class
- Creating four partial class files with clear separation of concerns (Identity, Configuration, Hierarchy, Lifecycle)
- Renaming `IComponent` to `IComponentHierarchy` to better reflect parent-child relationships
- Splitting `IRuntimeComponent` into `IActivatable` (activation/deactivation) and `IUpdatable` (frame updates)
- Creating new unified `IComponent` interface that combines all constituent interfaces (`IEntity`, `ILoadable`, `IValidatable`, `IComponentHierarchy`, `IActivatable`, `IUpdatable`)
- Updating all codebase references to use new class and interface structures
- Updating source generators to reference new class and interface structures
- Updating all tests to use new class and interface structures
- Maintaining all existing functionality through constituent interfaces

### Out of Scope

- Changing component behavior or functionality
- Adding new component features
- Refactoring component lifecycle beyond class consolidation
- Removing any constituent interfaces (`IEntity`, `ILoadable`, `IValidatable`, etc.)
- Performance optimizations beyond structural consolidation
- Changes to component creation patterns or dependency injection
- Backward compatibility shims for old interface names (breaking change accepted)
