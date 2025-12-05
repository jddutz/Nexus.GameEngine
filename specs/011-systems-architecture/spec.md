# Feature Specification: Systems Architecture Refactoring

**Feature Branch**: `011-systems-architecture`  
**Created**: November 30, 2025  
**Status**: Draft  
**Input**: User description: "Refactor component architecture to introduce Systems pattern for framework service access"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create Components Without Constructor Injection Bloat (Priority: P1)

Developers can create new game components without needing to inject and pass through framework services (graphics, resources, input, window) via constructors. Components access framework services directly through strongly-typed system properties that the framework automatically initializes.

**Why this priority**: This is the core value proposition and addresses the primary pain point. Without this, the entire refactoring provides no benefit.

**Independent Test**: Create a new drawable component class that needs window dimensions and graphics context. Verify the component has a parameterless constructor and accesses window size via `this.Window` property. Component should compile and run without requiring any constructor parameters.

**Acceptance Scenarios**:

1. **Given** a developer needs to create a component requiring window dimensions, **When** they define the component class, **Then** they can access window size via the `Window` property without declaring constructor parameters
2. **Given** a component needs multiple framework services (graphics, resources, window), **When** the component is instantiated by the framework, **Then** all system properties are initialized and accessible in the component's lifecycle methods
3. **Given** an existing component with constructor-injected services, **When** the developer migrates to systems architecture, **Then** they can remove all framework service constructor parameters and access the same functionality via system properties

---

### User Story 2 - Discover Available Framework Capabilities via IntelliSense (Priority: P2)

Developers can discover what framework capabilities are available to their components by typing `this.` and seeing the available systems (Graphics, Resources, Window, Input, Content) appear in IntelliSense with full type information and extension methods.

**Why this priority**: Improves developer experience and discoverability but doesn't block core functionality. Can be delivered after the basic system properties work.

**Independent Test**: Open a component class in the IDE, type `this.Graphics.` and verify IntelliSense shows available graphics operations. Select a method and verify it compiles and provides correct documentation tooltips.

**Acceptance Scenarios**:

1. **Given** a developer is working in a component class, **When** they type `this.` in a method body, **Then** IntelliSense displays all available system properties (Graphics, Resources, Window, Input, Content) with type information
2. **Given** a developer types `this.Graphics.`, **When** IntelliSense appears, **Then** all graphics-related extension methods are displayed with documentation
3. **Given** a developer selects a system extension method, **When** they complete the call, **Then** the code compiles without errors and the method delegates to the appropriate underlying service

---

### User Story 3 - Test Components with Mocked Systems (Priority: P3)

Developers can write unit tests for their components by creating the component with `new`, setting system properties to mocked implementations, and verifying behavior without needing the full framework infrastructure.

**Why this priority**: Essential for maintainability but can be implemented after core functionality works. Existing components can continue using existing test patterns during migration.

**Independent Test**: Write a unit test that creates a component directly, assigns a mocked `IGraphicsSystem` to its `Graphics` property, calls a component method that uses graphics functionality, and verifies the expected graphics operations were invoked on the mock.

**Acceptance Scenarios**:

1. **Given** a developer is writing a unit test for a component, **When** they create the component with `new MyComponent()`, **Then** they can assign mocked system implementations to the component's system properties
2. **Given** a component uses graphics functionality, **When** a test mocks the `IGraphicsSystem` and calls the component method, **Then** the test can verify the expected graphics operations were invoked
3. **Given** a component created outside the framework (e.g., in tests), **When** the component attempts to use an uninitialized system, **Then** the framework provides a clear error message indicating systems must be set before component activation

---

### User Story 4 - Simplified Component Hierarchy with Consolidated Base Class (Priority: P1)

Developers work with a single `Component` base class that provides all component functionality (identity, configuration, hierarchy, lifecycle) instead of navigating through four separate classes (Entity, Configurable, Component, RuntimeComponent).

**Why this priority**: Simplifies the mental model and reduces confusion. This is part of the core architecture improvement and should be delivered with the systems refactoring.

**Independent Test**: Create a new component inheriting from `Component`. Verify it has access to identity (Id, Name), configuration (OnLoad), hierarchy (Parent, Children), and lifecycle (OnActivate, OnUpdate) methods without needing to inherit from multiple classes.

**Acceptance Scenarios**:

1. **Given** a developer creates a new component, **When** they inherit from `Component`, **Then** they have access to all component capabilities (identity, configuration, hierarchy, lifecycle)
2. **Given** an existing component inherits from `RuntimeComponent`, **When** the developer updates the base class to `Component`, **Then** all existing functionality continues to work without changes
3. **Given** a developer is reading component code, **When** they look at the class declaration, **Then** they see a single `Component` base class with functionality organized in partial class files by concern

---

### Edge Cases

- What happens when a component is created directly with `new` without using `ComponentFactory` and attempts to access system properties before they're initialized? The framework should detect this during the activation lifecycle and throw `InvalidOperationException` with a clear message: "System properties not initialized. Components must be created via ComponentFactory or ContentManager."

- How does the system handle components that need domain-specific dependencies (not framework services)? Domain dependencies continue to be injected via constructor as before. The systems pattern only replaces framework service injection, not all dependency injection.

- What happens if a system extension method is called on a null system reference? This shouldn't happen in normal use since systems are initialized by the framework. If it does (improper component creation), the `null!` suppression will cause a `NullReferenceException` pointing to the missing system initialization.

- How are systems handled during component disposal? System properties are references to singleton services managed by the DI container. Components don't own systems and shouldn't dispose them. The DI container handles system lifecycle.

- What if a component needs access to a system that hasn't been created yet (Phase 2 systems)? The system property won't exist on the base `Component` class until that phase is implemented. This is a non-breaking addition - new properties can be added to the base class without affecting existing components.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The framework MUST provide five core system interfaces: `IResourceSystem`, `IGraphicsSystem`, `IContentSystem`, `IWindowSystem`, and `IInputSystem`

- **FR-002**: Each system interface MUST be an empty marker interface with all functionality provided through extension methods to prevent coupling to implementation details

- **FR-003**: The `Component` base class MUST provide non-nullable system properties (Graphics, Resources, Content, Window, Input) initialized with the null-forgiving operator (`= null!`)

- **FR-004**: `ComponentFactory` MUST initialize all system properties immediately after component instantiation and before any component lifecycle methods are invoked

- **FR-005**: The framework MUST validate system initialization during the component activation lifecycle and throw `InvalidOperationException` with a descriptive message if systems are not initialized

- **FR-006**: Components MUST support parameterless constructors for framework service access (domain dependencies may still use constructor injection)

- **FR-007**: The component hierarchy MUST be consolidated from four classes (Entity, Configurable, Component, RuntimeComponent) into a single `Component` class organized using partial class files

- **FR-008**: The consolidated `Component` class MUST preserve all existing functionality: identity (Id, Name), configuration (Load, Configure, Validate), hierarchy (Parent, Children), and lifecycle (Activate, Update, Deactivate)

- **FR-009**: System extension methods MUST delegate to underlying framework services accessed via the internal system implementation classes

- **FR-010**: The framework MUST register system implementations as singletons in the DI container and cache them in `ComponentFactory` to avoid repeated service resolution

- **FR-011**: System extension method namespaces MUST be added to `GlobalUsings.cs` to provide automatic availability across the codebase

- **FR-012**: Components created for testing MUST be able to have system properties assigned directly to support mocking without requiring the full framework

- **FR-013**: The migration MUST maintain backward compatibility for components that don't directly inject framework services (no breaking changes to component creation API)

- **FR-014**: The framework MUST support incremental migration where some components use the new systems pattern while others temporarily retain constructor injection until migrated

### Key Entities

- **System**: A logical grouping of related framework services accessed via a marker interface and extension methods. Represents a cohesive area of framework functionality (graphics, resources, input, window management, content management).

- **Component**: The fundamental building block of the game engine. Has identity, configuration, hierarchical relationships, and lifecycle. Accesses framework capabilities through system properties.

- **ComponentFactory**: Responsible for component instantiation and system property initialization. Ensures all components are properly configured before entering their lifecycle.

- **System Implementation**: Internal sealed class that wraps underlying framework services and provides access to extension methods. One implementation per system interface.

- **Extension Method**: Static method that provides system functionality. Casts the system interface to the internal implementation to access underlying services.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of new components created after the refactoring can be implemented with parameterless constructors (zero framework service constructor parameters)

- **SC-002**: Component creation performance improves or remains neutral compared to the current constructor injection approach (measured via benchmark showing system property assignment vs ActivatorUtilities overhead)

- **SC-003**: Existing component tests pass after migration with updated mocking strategy (mock systems instead of individual services)

- **SC-004**: Developers can discover available framework capabilities by typing `this.` in any component method and seeing all five system properties in IntelliSense

- **SC-005**: The consolidated `Component` class hierarchy reduces cognitive load by eliminating three intermediate base classes while maintaining 100% of existing functionality

- **SC-006**: Components created incorrectly (without ComponentFactory) produce clear error messages that guide developers to correct usage within the first activation attempt

- **SC-007**: The migration maintains zero breaking changes to the component creation API (`ContentManager.CreateInstance()` continues to work identically)

- **SC-008**: Code reviews show reduced constructor parameter counts for drawable components (target: average reduction from 3-5 parameters to 0 framework service parameters)
