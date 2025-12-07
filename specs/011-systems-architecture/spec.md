# Feature Specification: Systems Architecture Refactoring

**Feature Branch**: `011-systems-architecture`  
**Created**: November 30, 2025  
**Updated**: December 6, 2025 (revised after component consolidation and property binding implementation)
**Status**: Draft  
**Input**: User description: "Introduce Systems pattern to eliminate dependency injection throughout the framework, reducing constructor bloat and tight coupling between framework classes"

## Context

The codebase has undergone significant architectural improvements:
- **Component Consolidation (Spec 014)**: Unified component hierarchy into single `Component` class with partial files
- **Property Bindings (Spec 013)**: Implemented robust declarative property binding system
- **Component Creation**: Components already use parameterless constructors - they don't inject framework services

**Current Problem**: Framework classes (`Renderer`, `PipelineManager`, `ResourceManager`, `BufferManager`, etc.) heavily use constructor injection, creating:
- Deep constructor parameter lists (5-9 parameters common)
- Tight coupling between framework services
- Inheritance trees where every subclass must pass dependencies up
- Difficult testing requiring extensive mocking

**Solution**: Systems pattern - empty marker interfaces with extension methods providing functionality, eliminating constructor injection.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Framework Classes Without Constructor Injection (Priority: P1)

Framework developers can create new framework services without needing to inject multiple dependencies via constructors. Services access other framework capabilities through strongly-typed system properties initialized by the DI container.

**Why this priority**: This is the core value proposition. Constructor injection bloat makes the framework difficult to maintain and extend.

**Independent Test**: Refactor `Renderer` class (currently has 9 constructor parameters) to use systems. Verify it compiles, runs identically, and has zero constructor parameters for framework services.

**Acceptance Scenarios**:

1. **Given** a framework class like `Renderer` with multiple constructor dependencies, **When** refactored to use systems, **Then** constructor parameters are reduced to zero for framework services (domain dependencies may remain)
2. **Given** a framework class needs graphics, resources, and window services, **When** using systems pattern, **Then** it accesses capabilities via `this.Graphics`, `this.Resources`, and `this.Window` properties
3. **Given** a framework class using systems, **When** instantiated by DI container, **Then** system properties are automatically initialized before any method calls

---

### User Story 2 - Discover Framework Capabilities via IntelliSense (Priority: P2)

Framework developers discover available framework capabilities by typing `this.` and seeing available systems (Graphics, Resources, Window, Input, Content) with full type information and extension methods.

**Why this priority**: Improves developer experience and discoverability. Can be delivered after basic system properties work.

**Independent Test**: In a framework class, type `this.Graphics.` and verify IntelliSense shows available graphics operations with correct documentation.

**Acceptance Scenarios**:

1. **Given** a framework class using systems, **When** developer types `this.` in a method body, **Then** IntelliSense displays available system properties (Graphics, Resources, Window, Input, Content)
2. **Given** developer types `this.Graphics.`, **When** IntelliSense appears, **Then** all graphics-related extension methods display with documentation
3. **Given** developer selects a system extension method, **When** completing the call, **Then** code compiles and method delegates to underlying service

---

### User Story 3 - Test Framework Classes with Mocked Systems (Priority: P3)

Framework developers write unit tests by creating mock system implementations and verifying behavior without full framework infrastructure.

**Why this priority**: Essential for maintainability but can be implemented after core functionality. Existing tests can continue using current patterns during migration.

**Independent Test**: Write unit test for framework class, assign mocked `IGraphicsSystem`, call method using graphics, verify expected operations were invoked.

**Acceptance Scenarios**:

1. **Given** a unit test for a framework class, **When** creating the class for testing, **Then** developer can assign mocked systems to system properties
2. **Given** a framework class uses graphics functionality, **When** test mocks `IGraphicsSystem` and calls the method, **Then** test verifies expected operations were invoked
3. **Given** a framework class created outside normal DI flow, **When** attempting to use uninitialized system, **Then** framework provides clear error message

---

### User Story 4 - Eliminate Service Location Anti-Pattern (Priority: P1)

Framework classes access services through strongly-typed system properties instead of service locator or manual DI resolution, maintaining compile-time type safety.

**Why this priority**: Service locator pattern is considered an anti-pattern. Systems pattern provides better discoverability and type safety.

**Independent Test**: Verify no framework classes use `serviceProvider.GetRequiredService<T>()` except in composition root and system initialization. All service access is through typed system properties.

**Acceptance Scenarios**:

1. **Given** a framework class needs access to graphics context, **When** using systems pattern, **Then** it uses `this.Graphics` instead of `_serviceProvider.GetRequiredService<IGraphicsContext>()`
2. **Given** all framework classes migrated to systems, **When** searching codebase for service location, **Then** only DI composition root and system initialization contain `GetRequiredService` calls
3. **Given** a new framework class, **When** it needs framework capabilities, **Then** developer uses system properties (compile-time safe) rather than service location (runtime lookup)

---

### Edge Cases

- **What happens when a framework class is instantiated outside DI and attempts to access system properties?** The systems will be null and accessing them will throw `NullReferenceException`. This is intentional - framework classes must be created via DI.

- **How does the system handle framework classes that need domain-specific dependencies (not framework services)?** Domain dependencies continue to be injected via constructor. The systems pattern only replaces framework service injection, not all DI.

- **What happens if a system extension method is called on a null system reference?** Runtime `NullReferenceException` occurs, pointing to improper initialization. Framework classes must be created via DI container.

- **How are systems handled during framework class disposal?** System properties are references to singleton services managed by DI container. Framework classes don't own systems and shouldn't dispose them. DI container handles system lifecycle.

- **What about circular dependencies between systems?** Systems are marker interfaces wrapping existing services. Circular dependencies that exist today remain; systems pattern doesn't introduce new ones. If circular dependencies exist, they must be resolved through refactoring (e.g., extract shared functionality to new service).

- **How do we handle framework classes that need conditional behavior based on whether a service is available?** Systems are always initialized for framework classes created by DI. For optional services, use traditional nullable DI: `IServiceProvider.GetService<T>()` returns null if not registered.

- **What about performance of extension method calls vs direct method calls?** Extension methods have identical performance to direct instance method calls - they're just syntactic sugar. The IL is identical after compilation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Framework MUST provide five core system interfaces: `IResourceSystem`, `IGraphicsSystem`, `IContentSystem`, `IWindowSystem`, and `IInputSystem`

- **FR-002**: Each system interface MUST be an empty marker interface with all functionality provided through extension methods to prevent coupling to implementation details

- **FR-003**: System implementation classes MUST be internal sealed classes that wrap existing framework services (IGraphicsContext, IResourceManager, etc.)

- **FR-004**: Extension methods MUST cast marker interface to internal implementation to access wrapped services

- **FR-005**: Framework base class (if created) or initialization infrastructure MUST provide system properties that DI container initializes

- **FR-006**: System implementations MUST be registered as singletons in DI container

- **FR-007**: Framework classes SHOULD have parameterless constructors or minimal domain-only constructor parameters (zero framework service parameters)

- **FR-008**: System extension method namespaces MUST be included in `GlobalUsings.cs` for automatic availability

- **FR-009**: Framework classes created via DI MUST have system properties initialized before any method invocations

- **FR-010**: Migration MUST maintain backward compatibility - existing functionality must work identically after refactoring

- **FR-011**: Framework classes MUST NOT use service locator pattern (`serviceProvider.GetRequiredService<T>()`) except in composition root and system initialization

- **FR-012**: System extension methods MUST delegate to underlying framework services accessed via internal implementation classes

- **FR-013**: Testing infrastructure MUST support mocking systems for unit tests without requiring full DI container

### Key Entities

- **System**: Logical grouping of related framework services accessed via marker interface and extension methods. Represents cohesive area of framework functionality (graphics, resources, input, window, content).

- **System Interface**: Empty marker interface (e.g., `IGraphicsSystem`) with no members. Serves as extension point for functionality.

- **System Implementation**: Internal sealed class wrapping underlying framework services. Provides extension methods access to services. Example: `GraphicsSystem` wraps `IGraphicsContext`, `IPipelineManager`, `ISwapChain`.

- **Extension Method**: Static method providing system functionality. Casts system interface to internal implementation to access wrapped services. Example: `DrawQuad(this IGraphicsSystem graphics, ...)`.

- **Framework Class**: Any class in the framework infrastructure (Renderer, PipelineManager, BufferManager, etc.) that needs access to framework services.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Constructor parameter count for framework classes reduced by 100% for framework services (domain dependencies may remain)

- **SC-002**: Zero uses of service locator pattern (`GetRequiredService<T>()`) in framework classes (except composition root and system initialization)

- **SC-003**: Framework compilation succeeds with zero errors after migration

- **SC-004**: All existing integration tests pass without modification after migration

- **SC-005**: Developers can discover framework capabilities via IntelliSense by typing `this.` in framework classes

- **SC-006**: Performance remains neutral - benchmarks show no degradation from extension method overhead

- **SC-007**: Code reviews confirm framework classes have simplified constructors (average reduction from 5-9 parameters to 0-2)

- **SC-008**: Unit tests can mock systems without requiring full DI infrastructure

## Assumptions

- **Assumption 1**: Extension methods on marker interfaces provide adequate IntelliSense discoverability (verified by LINQ precedent)

- **Assumption 2**: Performance of extension methods is identical to instance methods (verified - they're syntactic sugar, identical IL)

- **Assumption 3**: Internal sealed classes can be safely cast from marker interfaces within extension methods (C# language guarantee)

- **Assumption 4**: Existing framework service interfaces (IGraphicsContext, IResourceManager, etc.) remain unchanged - systems wrap them

- **Assumption 5**: DI container supports property injection or framework classes can receive systems via initialization method after construction

- **Assumption 6**: Migration can be done incrementally - not all framework classes need systems simultaneously

- **Assumption 7**: No external code depends on framework class constructor signatures (internal framework only)

- **Assumption 8**: Test infrastructure can create mock system implementations for unit testing

## Dependencies

- **Dependency 1**: Component consolidation (Spec 014) COMPLETE - provides foundation for systems pattern

- **Dependency 2**: Property bindings (Spec 013) COMPLETE - demonstrates successful use of declarative patterns

- **Dependency 3**: DI container configuration must be updated to register system implementations as singletons

- **Dependency 4**: Framework classes must be refactored sequentially to avoid breaking dependencies

- **Dependency 5**: Extension method namespaces must be added to `GlobalUsings.cs` before widespread adoption

## Scope

### In Scope

- Creating five system marker interfaces (IGraphicsSystem, IResourceSystem, IWindowSystem, IInputSystem, IContentSystem)
- Implementing internal sealed system implementation classes wrapping existing services
- Creating extension methods for all framework capabilities on system interfaces
- Eliminating constructor injection of framework services from framework classes
- Updating DI registration to include system singletons
- Adding system extension method namespaces to GlobalUsings.cs
- Refactoring Renderer, PipelineManager, and other high-impact framework classes
- Updating unit tests to use mocked systems
- Performance benchmarking to verify no degradation

### Out of Scope

- Changing existing framework service interfaces (IGraphicsContext, etc.) - systems wrap them, don't replace them
- Refactoring component classes - they already don't use constructor injection for framework services
- Creating new framework capabilities - this is purely architectural refactoring
- Changing DI lifetime scopes (singleton, scoped, transient) for existing services
- Modifying game engine public API - this is internal framework refactoring
- Hot-reload or runtime system switching - systems are initialized once at startup
- Custom component base classes - focus is on framework infrastructure

### Migration Strategy

**Phase 1: Infrastructure**
1. Create system interfaces and implementations
2. Register systems in DI container
3. Add extension methods for high-frequency operations
4. Add system namespaces to GlobalUsings.cs

**Phase 2: High-Impact Classes**
1. Refactor `Renderer` (9 parameters → ~0-2)
2. Refactor `PipelineManager` (5 parameters → ~0)
3. Refactor `ResourceManager` (4 parameters → ~0)
4. Update unit tests for refactored classes

**Phase 3: Remaining Framework Classes**
1. Refactor buffer managers, descriptor managers
2. Refactor remaining graphics infrastructure
3. Refactor content management classes
4. Update all remaining tests

**Phase 4: Validation**
1. Run full test suite
2. Performance benchmarks
3. Integration test validation
4. Code review for service locator anti-pattern usage

## Non-Goals

- This spec does NOT add new framework capabilities
- This spec does NOT change component architecture (already consolidated in Spec 014)
- This spec does NOT modify the property binding system (Spec 013)
- This spec does NOT affect game developer-facing APIs
- This spec does NOT introduce new DI patterns - systems are singletons like current services
- This spec does NOT change how components are created - ContentManager/ComponentFactory unchanged
- This spec does NOT replace all constructor injection - domain dependencies still use constructors

## Technical Notes

### Why Marker Interfaces + Extension Methods?

1. **Zero coupling**: Consumers cannot accidentally depend on implementation details
2. **Evolution**: Can add/change extension methods without breaking binary compatibility
3. **IntelliSense**: Extension methods appear as instance methods, natural discoverability
4. **Testing**: Can mock marker interfaces with custom implementations
5. **Precedent**: LINQ uses this pattern extensively (`IEnumerable<T>` + extension methods)

### Alternative Approaches Considered

**Service Locator Pattern**: ❌ Anti-pattern, runtime errors, poor discoverability
**Property Injection**: ❌ Magic, harder to test, implicit dependencies
**Ambient Context**: ❌ Global state, testing nightmares
**Constructor Injection**: ❌ Current problem - creates bloat and tight coupling
**Abstract Factory**: ❌ Adds complexity without solving discoverability

### Performance Considerations

Extension methods compile to identical IL as instance methods:
```csharp
// Extension method call:
graphics.DrawQuad(position, size);

// Compiles to identical IL as:
GraphicsSystemExtensions.DrawQuad(graphics, position, size);

// JIT inlines both identically - zero overhead
```

Systems are singletons - one instance per application lifetime, no allocation overhead.

