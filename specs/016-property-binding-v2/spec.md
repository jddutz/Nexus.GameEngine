# Feature Specification: Property Binding Framework Revision

**Feature Branch**: `016-property-binding-v2`  
**Created**: December 7, 2025  
**Status**: Draft  
**Input**: User description: "The previously implemented Property Binding Framework is incomplete and not working as expected. Revise with simplified, high-performance runtime property synchronization using event-driven bindings configured via templates."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Parent Property Binding (Priority: P1)

A developer creates a health bar UI component that automatically updates when a parent component's health property changes, using a simple fluent template syntax without manual event subscriptions.

**Why this priority**: This is the foundational use case that delivers immediate value. The framework must support basic one-way property synchronization from parent to child before any advanced features are useful.

**Independent Test**: Create a parent component with a health property and a child component with a text display. Configure the child's template with `Bindings = [new PropertyBinding<Parent, string>().FindParent().On(p => p.HealthChanged).ConvertToString(formatter).Set(SetText)]`. Verify that changing parent health updates child text.

**Acceptance Scenarios**:

1. **Given** a parent component with a `Health` property that raises `HealthChanged` event, **When** a child component defines a property binding to parent's health, **Then** the child's bound property updates immediately when parent health changes
2. **Given** a component with an active property binding, **When** the component is deactivated, **Then** all event subscriptions are removed and no memory leaks occur
3. **Given** a property binding configured in a template, **When** the component loads, **Then** the binding is added to the component's PropertyBindings collection but not yet activated
4. **Given** bindings in the PropertyBindings collection, **When** the component activates, **Then** each binding subscribes to its source event and begins synchronizing values

---

### User Story 2 - Type Conversion and Formatting (Priority: P1)

A developer binds a numeric health value to a text display with automatic string conversion and formatting (e.g., "Health: 75"), without writing custom conversion code.

**Why this priority**: Real-world property synchronization almost always requires type transformation. String formatting is the most common conversion pattern and must be built into the core framework.

**Independent Test**: Bind a float health property to a string text property with `.ConvertToString(new StringFormatConverter("Health: {0:F0}"))`. Verify the text displays "Health: 75" when health is 75.0.

**Acceptance Scenarios**:

1. **Given** a property binding with `.ConvertToString(formatter)`, **When** the source value changes, **Then** the formatter converts the value and the converted string is set on the target
2. **Given** a property binding with a custom converter, **When** the source event fires, **Then** the converter's Convert method transforms the value before setting the target property
3. **Given** a binding without type conversion methods, **When** the source and target types match, **Then** the value is passed directly without conversion

---

### User Story 3 - Custom Event Subscription (Priority: P2)

A developer subscribes to a specific property change event (e.g., `HealthChanged`) instead of a generic `PropertyChanged` event, for better performance and explicit intent.

**Why this priority**: Fine-grained event subscription reduces unnecessary update checks and makes bindings more explicit. This is important for performance-sensitive scenarios but the framework can default to PropertyChanged if not specified.

**Independent Test**: Create a component with both `HealthChanged` and `ManaChanged` events. Bind only to `HealthChanged` using `.On(s => s.HealthChanged)`. Verify that only health changes trigger the binding, not mana changes.

**Acceptance Scenarios**:

1. **Given** a property binding with `.On(s => s.HealthChanged)`, **When** the HealthChanged event fires, **Then** the binding updates the target property
2. **Given** a property binding without `.On()` specified, **When** any PropertyChanged event fires on the source, **Then** the binding updates using the default event subscription
3. **Given** a binding subscribed to a specific event, **When** other events fire on the source component, **Then** the binding does not update

---

### User Story 4 - Source Object Resolution Strategies (Priority: P2)

A developer uses different source resolution strategies (FindParent, FindSibling, FindChild, FindNamed) to bind properties across various component relationships beyond just parent-child.

**Why this priority**: Component trees have complex relationships. While FindParent is most common, sibling and named lookups enable flexible architectures. This is P2 because basic parent lookup (P1) must work first.

**Independent Test**: Create sibling components and bind between them using `.FindSibling<T>()`. Create a named component and bind to it using `.FindNamed("ComponentName")`. Verify both binding strategies correctly resolve source objects.

**Acceptance Scenarios**:

1. **Given** a component with no `.Find*()` method specified, **When** the binding activates, **Then** it defaults to `.FindParent<TSource>()`
2. **Given** a binding with `.FindSibling<PlayerComponent>()`, **When** the component has a sibling of type PlayerComponent, **Then** the binding resolves that sibling as the source
3. **Given** a binding with `.FindNamed("Player")`, **When** a component named "Player" exists in the tree, **Then** the binding resolves that component as the source
4. **Given** a binding that fails to resolve a source object, **When** activation occurs, **Then** the binding silently skips activation without throwing exceptions

---

### User Story 5 - Binding Without Source Generators (Priority: P1)

The framework operates without relying on source generators, using explicit setter delegates defined in template configurations for type-safe property updates.

**Why this priority**: The user explicitly questioned whether source generators are needed. The revised design should work with explicit setter methods (like `SetText`) provided in the binding definition, avoiding code generation complexity while maintaining type safety.

**Independent Test**: Create a property binding that calls a component's existing setter method (`.Set(SetText)`) without any generated code. Verify the binding successfully updates the property at runtime.

**Acceptance Scenarios**:

1. **Given** a property binding with `.Set(SetText)` in the template, **When** the source event fires, **Then** the binding invokes the SetText method with the converted value
2. **Given** a property binding without `.Set()` specified, **When** the binding is loaded, **Then** it is considered invalid and not added to the PropertyBindings collection
3. **Given** component templates defining property bindings, **When** compiled, **Then** no source generators are required for the bindings to function correctly

---

### User Story 6 - Default Behavior and Minimal Configuration (Priority: P2)

A developer creates a simple binding with minimal configuration, and the framework applies sensible defaults (FindParent for source, PropertyChanged for event, as-is conversion for matching types).

**Why this priority**: Usability feature that reduces boilerplate for common cases. The framework should work with just `new PropertyBinding<TSource, TOut>().Set(setter)` for the simplest scenarios.

**Independent Test**: Create a binding `new PropertyBinding<Parent, string>().Set(SetText)` with no explicit FindParent, On, or conversion methods. Verify it defaults to finding parent, subscribing to PropertyChanged, and converts using `as string`.

**Acceptance Scenarios**:

1. **Given** a binding without `.FindParent()` specified, **When** activated, **Then** it searches for the nearest parent of type TSource
2. **Given** a binding without `.On()` specified, **When** activated, **Then** it subscribes to the source's PropertyChanged event
3. **Given** a binding without conversion methods and matching types, **When** the event fires, **Then** it directly assigns the source value to the target

---

### Edge Cases

- What happens when a binding's source component is not found during activation? (Should skip activation silently, logging a warning)
- What happens when the source event is null or doesn't exist on the source type? (Should validate during activation and skip if invalid)
- How does the system handle recursive updates (e.g., binding A→B and B→A)? (Currently out of scope; two-way bindings are not in this revision)
- What happens when a component is deactivated while an event handler is executing? (Handler should complete, but no new events should be processed)
- How are bindings cleaned up when a component is disposed? (Deactivate should unsubscribe all events; disposal should clear the PropertyBindings collection)
- What happens when `.Set()` is not called on a binding definition? (Binding is invalid and should not be added to PropertyBindings collection)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The framework MUST provide a PropertyBinding class parameterized by source component type and output value type for defining property synchronization rules in templates
- **FR-002**: PropertyBinding MUST support method chaining for configuration including source finding, event selection, value conversion, and target setting
- **FR-003**: Each component MUST maintain a PropertyBindings collection populated during the Load phase from template binding definitions
- **FR-004**: The framework MUST activate bindings during component activation by subscribing to source events
- **FR-005**: The framework MUST deactivate bindings during component deactivation by unsubscribing from all events
- **FR-006**: PropertyBinding MUST support multiple source object resolution strategies: find nearest parent (default), find sibling, find child, find by name
- **FR-007**: PropertyBinding MUST support subscribing to specific property change events or default to a general property change notification
- **FR-008**: PropertyBinding MUST support value conversion through format strings for text output and custom converter objects
- **FR-009**: PropertyBinding MUST require an explicit target setter method to define how values are assigned to the target property
- **FR-010**: PropertyBinding definitions without a target setter MUST be considered invalid and excluded from the PropertyBindings collection
- **FR-011**: The framework MUST operate without automated code generation for property binding functionality
- **FR-012**: PropertyBinding MUST default to finding nearest parent if no source resolution strategy is specified
- **FR-013**: PropertyBinding MUST default to subscribing to general property change notifications if no specific event is specified
- **FR-014**: Failed source resolution during activation MUST skip activation silently without raising errors
- **FR-015**: Event subscriptions MUST be removed during deactivation to prevent memory leaks
- **FR-016**: PropertyBinding MUST be strongly typed with compile-time verification of source and output types
- **FR-017**: The framework MUST support built-in string formatting for common text conversion scenarios
- **FR-018**: Custom value converter implementations MUST be supported for arbitrary type transformations

### Key Entities

- **PropertyBinding**: Represents a binding definition configured via method chaining. Contains source resolution strategy, event subscription configuration, value conversion pipeline, and target setter delegate.
- **PropertyBinding Interface**: Non-generic marker for storing bindings in collections. Provides lifecycle methods for activation and deactivation.
- **PropertyBinding Definition**: Represents a binding configuration from a template before it's converted to an active binding instance.
- **Component PropertyBindings Collection**: Collection of binding instances managed by each component. Populated during Load, activated during Activate, deactivated during Deactivate.
- **Value Converter**: Interface for custom value transformation. Provides method to convert input values to output values.
- **String Format Converter**: Built-in converter for string formatting using format patterns.
- **Source Resolution Strategies**: FindParent, FindSibling, FindChild, FindNamed - methods for locating source components in the component tree.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can configure property bindings using template syntax with no more than 5 method calls for common scenarios
- **SC-002**: Property bindings activate and deactivate without memory leaks, verified through integration tests with repeated activation cycles
- **SC-003**: Developers can implement property bindings without relying on automated code generation tools
- **SC-004**: Binding updates complete within one frame (16ms at 60fps) for typical UI synchronization scenarios
- **SC-005**: Property binding code coverage reaches minimum 80% through unit and integration tests
- **SC-006**: Developers can implement basic parent-to-child property synchronization with a single template binding definition
- **SC-007**: Invalid binding configurations (missing target setter) are detected during Load phase before activation

## Dependencies and Assumptions *(optional)*

### Dependencies

- Components must implement property change notification through events (e.g., `HealthChanged` event or generic `PropertyChanged`)
- Component lifecycle infrastructure (OnLoad, OnActivate, OnDeactivate) must call corresponding PropertyBinding lifecycle methods
- Component tree structure must be navigable for source resolution (Parent, Children properties)
- IValueConverter interface and StringFormatConverter must exist for value transformation

### Assumptions

- Property change events follow standard EventHandler signature or can be adapted to Action delegates
- Component properties using bindings have corresponding setter methods that can be passed to `.Set()`
- Source components are typically available when bindings activate (parent components load before children)
- Performance requirements assume modern desktop/gaming hardware (60+ fps rendering)
- Developers prefer explicit, verbose binding definitions over implicit magic behavior
- Two-way bindings are out of scope for this revision (future enhancement)
- The existing ComponentProperty system with deferred updates is separate from this binding framework

## Out of Scope *(optional)*

### Explicitly Excluded

- **Two-way property bindings**: Bidirectional synchronization between source and target is deferred to a future revision
- **Source generators for property bindings**: The framework must work with explicit setter delegates, not generated code
- **Automatic dependency tracking**: The framework will not detect property dependencies; developers must explicitly configure bindings
- **Binding expressions with computed values**: Complex expressions like `Health / MaxHealth` are not supported; use custom converters instead
- **Design-time tooling**: No visual designers or IntelliSense enhancements for binding syntax
- **Performance monitoring for bindings**: No built-in profiling or diagnostics for binding overhead
- **Migration tools from 013-property-binding**: Existing code using the old framework must be manually updated

### Future Enhancements

- Two-way binding support with `.TwoWay()` configuration
- Binding validation at compile-time through analyzers (not source generators)
- Performance profiling attributes to identify expensive binding operations
- Binding debugging visualizer for component trees
- Multi-source bindings (combine values from multiple sources)
