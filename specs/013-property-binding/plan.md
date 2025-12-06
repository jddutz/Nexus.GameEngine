# Implementation Plan: Property Binding System

**Branch**: `013-property-binding` | **Date**: December 6, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/013-property-binding/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement a property binding system that enables declarative parent-to-child property synchronization in component templates, eliminating manual event subscription boilerplate. The system uses a composition-first approach with source generators to create type-safe `PropertyBindings` classes for each component, supporting multiple lookup strategies (parent, sibling, context, named), value converters, and both one-way and two-way binding modes. Bindings activate during the component lifecycle's `OnActivate()` phase and automatically clean up during `OnDeactivate()`.

## Technical Context

**Language/Version**: C# 9.0+ with nullable reference types enabled, .NET 9.0  
**Primary Dependencies**: 
- Roslyn source generators (ComponentPropertyGenerator, TemplateGenerator, new PropertyBindingsGenerator)
- Microsoft.Extensions.DependencyInjection
- Silk.NET (Vulkan bindings for graphics context)
- System.Reflection (for runtime event subscription)

**Storage**: N/A (in-memory component property state)  
**Testing**: xUnit for unit tests, frame-based integration tests via TestApp  
**Target Platform**: Windows/Linux desktop (Vulkan-capable)  
**Project Type**: Game engine library with component-based architecture  
**Performance Goals**: 
- <5% overhead compared to direct property assignment
- Zero allocations in hot paths (binding activation happens once)
- Support 1000+ active bindings without frame drops at 60 fps

**Constraints**: 
- Must integrate with existing `[ComponentProperty]` attribute system
- Bindings activate during `OnActivate()` lifecycle phase (after Load, before first Update)
- Must use `SetCurrent{PropertyName}()` method to bypass interpolation for immediate updates
- Event subscriptions must be cleaned up during `OnDeactivate()` to prevent memory leaks
- No breaking changes to existing ComponentProperty or Template systems

**Scale/Scope**: 
- ~200 LOC for core PropertyBinding class
- 3 new source generators (PropertyBindingsGenerator, PropertyChangedEventGenerator, modifications to existing generators)
- 5 lookup strategy implementations
- 3-5 built-in value converters
- Support for ~10-50 bindings per component in typical use cases

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Initial Check**: ✅ PASSED (before Phase 0)  
**Post-Design Check**: ✅ PASSED (after Phase 1)

### I. Documentation-First Test-Driven Development ✅

- [ ] **Build Verification**: Solution builds cleanly before starting work
- [ ] **Documentation Updates**: Update `.docs/Deferred Property Generation System.md` with binding system integration
- [ ] **Test Generation**: Unit tests for PropertyBinding, lookup strategies, converters, and lifecycle integration
- [ ] **Red Phase**: Tests fail initially as expected
- [ ] **Implementation**: Implement PropertyBinding class, source generators, and component integration
- [ ] **Green Phase**: All tests pass
- [ ] **Rebuild & Verify**: Final build with no warnings/errors
- [ ] **Summary**: Provide manual testing instructions for TestApp integration

**Status**: ✅ PASS - Standard TDD workflow applies

### II. Component-Based Architecture ✅

- PropertyBinding system integrates with existing `IRuntimeComponent` lifecycle
- Bindings configured via template records (`PropertyBindings` property on each template)
- `IContentManager` creates components; binding activation happens in component's `OnActivate()`
- Follows interface segregation: `IValueConverter` and `IBidirectionalConverter` interfaces
- No changes to component creation pattern or factory responsibilities

**Status**: ✅ PASS - Fully aligned with component architecture

### III. Source-Generated Animated Properties ✅

- Extends existing `[ComponentProperty]` system with `NotifyChange` parameter
- Generated `{PropertyName}Changed` events enable observable properties
- Bindings use existing `SetCurrent{PropertyName}()` method for immediate updates (bypasses interpolation)
- No conflicts with deferred updates or animation system
- New `PropertyBindingsGenerator` creates type-safe binding configuration classes
- Maintains zero runtime overhead principle through compile-time generation

**Status**: ✅ PASS - Builds upon and complements existing property system

### IV. Vulkan Resource Management ✅

- No direct interaction with Vulkan resources
- Bindings are in-memory event subscriptions (no GPU resources involved)
- No changes to resource management patterns

**Status**: ✅ PASS - N/A for this feature

### V. Explicit Approval Required ✅

- All implementation follows TDD workflow with test-first approach
- Source generator changes are incremental (extend existing generators)
- Component lifecycle integration is non-breaking (add binding activation/deactivation)
- No assumptions about desired behavior; all patterns documented in spec

**Status**: ✅ PASS - Will follow explicit approval workflow

### Overall Constitution Compliance

**VERDICT**: ✅ **APPROVED TO PROCEED**

No constitution violations. Feature aligns with all core principles:
- Documentation-first TDD workflow
- Component-based architecture integration
- Source generator extension pattern
- No resource management changes needed
- Explicit approval process respected

## Project Structure

### Documentation (this feature)

```text
specs/013-property-binding/
├── plan.md              # This file (filled by /speckit.plan command)
├── research.md          # Already exists - industry analysis and design decisions
├── data-model.md        # Phase 1 output - entity definitions
├── quickstart.md        # Phase 1 output - developer quick start guide
└── contracts/           # Phase 1 output - API interfaces and contracts
```

### Source Code (repository root)

```text
src/
├── GameEngine/
│   ├── Components/
│   │   ├── PropertyBinding.cs           # NEW: Core binding class with fluent API
│   │   └── PropertyBindings.cs          # NEW: Base class for generated bindings
│   ├── Data/
│   │   ├── IValueConverter.cs           # NEW: Value converter interface
│   │   ├── IBidirectionalConverter.cs   # NEW: Two-way converter interface
│   │   ├── StringFormatConverter.cs     # NEW: Built-in format converter
│   │   ├── MultiplyConverter.cs         # NEW: Built-in multiply converter
│   │   └── PercentageConverter.cs       # NEW: Built-in percentage converter
│   ├── Components/Lookups/             # NEW: Lookup strategy implementations
│   │   ├── ILookupStrategy.cs
│   │   ├── ParentLookup.cs
│   │   ├── SiblingLookup.cs
│   │   ├── ChildLookup.cs
│   │   ├── ContextLookup.cs
│   │   └── NamedObjectLookup.cs
│   └── Events/
│       └── PropertyChangedEventArgs.cs   # NEW: Generic property change event args
│
├── SourceGenerators/
│   ├── ComponentPropertyGenerator.cs     # MODIFY: Add PropertyChanged event generation
│   ├── TemplateGenerator.cs              # MODIFY: Add Bindings property to templates
│   └── PropertyBindingsGenerator.cs      # NEW: Generate {Component}PropertyBindings classes
│
└── Tests/
    └── GameEngine/
        ├── Components/
        │   ├── PropertyBinding.Tests.cs  # NEW: Core binding tests
        │   └── PropertyBindings.Tests.cs # NEW: Base class tests
        ├── Data/
        │   └── Converters.Tests.cs       # NEW: Converter tests
        └── Components/Lookups/
            └── LookupStrategies.Tests.cs # NEW: Lookup strategy tests
```

**Structure Decision**: This is a single C# library project (GameEngine) with source generators. New binding functionality extends the existing Components and Data namespaces. Lookup strategies are grouped in a new `Components/Lookups/` subdirectory for organization. Source generators extend existing patterns by adding a new `PropertyBindingsGenerator` and modifying existing generators to add binding-related generated code.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | No violations | All principles satisfied |

---

## Phase Completion Summary

### Phase 0: Research ✅ COMPLETE

**Status**: Previously completed  
**Output**: `research.md`

Key research findings:
- Analyzed 8 industry solutions (WPF, Unity, Unreal, React, Vue, Godot, Angular, SwiftUI)
- Identified composition-first approach as best fit for Nexus.GameEngine
- Decided on template-based binding configuration (not attribute-based)
- Selected event-based notification over reactive properties for performance
- Documented decision to use source generators for type safety with zero runtime overhead

### Phase 1: Design & Contracts ✅ COMPLETE

**Status**: Completed December 6, 2025  
**Outputs**:
- `data-model.md` - Complete entity definitions with validation rules and relationships
- `contracts/` - 7 API contract files with interfaces, classes, and enums
- `quickstart.md` - Developer quick start guide with common scenarios
- `.github/copilot-instructions.md` - Updated with new technology stack

**Key Design Decisions**:

1. **PropertyBinding Class**: Fluent API for configuration, internal lifecycle management
2. **Lookup Strategies**: 5 implementations (Parent, Sibling, Child, Context, Named)
3. **Value Converters**: Two-tier interface (IValueConverter, IBidirectionalConverter)
4. **Source Generators**: 
   - PropertyBindingsGenerator (new) - generates {Component}PropertyBindings classes
   - ComponentPropertyGenerator (modified) - adds PropertyChanged events
   - TemplateGenerator (modified) - adds Bindings property
5. **Lifecycle Integration**: Bindings stored in OnLoad, activated in OnActivate, deactivated in OnDeactivate
6. **Performance**: Reflection cached during Activate, direct event subscription, <5% overhead target

**Constitution Re-Check**: ✅ PASSED - All principles maintained through detailed design

### Phase 2: Task Breakdown

**Status**: NOT STARTED (use `/speckit.tasks` command)

The planning phase ends here as per the prompt instructions. The next step is to run `/speckit.tasks` to generate the detailed task breakdown for implementation.

---

## Next Steps for Implementation

**Command**: Run `/speckit.tasks` to generate `tasks.md` with detailed implementation checklist

**Required before starting implementation**:
1. ✅ Solution builds cleanly
2. ✅ Constitution check passed
3. ✅ Research completed
4. ✅ Design documented
5. ⏸️ Awaiting explicit approval to proceed with implementation

**Implementation Order** (once approved):
1. Create base interfaces and classes (ILookupStrategy, IValueConverter, PropertyBinding, etc.)
2. Implement lookup strategies
3. Implement built-in converters
4. Modify existing source generators (ComponentPropertyGenerator, TemplateGenerator)
5. Create new PropertyBindingsGenerator
6. Write unit tests for all components
7. Update component lifecycle integration
8. Create integration tests in TestApp
9. Update documentation (.docs/)
10. Manual testing and validation

---

## Artifacts Generated

**Planning Documentation**:
- ✅ `specs/013-property-binding/plan.md` (this file)
- ✅ `specs/013-property-binding/research.md` (pre-existing)
- ✅ `specs/013-property-binding/data-model.md`
- ✅ `specs/013-property-binding/quickstart.md`

**API Contracts**:
- ✅ `specs/013-property-binding/contracts/ILookupStrategy.cs`
- ✅ `specs/013-property-binding/contracts/IValueConverter.cs`
- ✅ `specs/013-property-binding/contracts/IBidirectionalConverter.cs`
- ✅ `specs/013-property-binding/contracts/PropertyBinding.cs`
- ✅ `specs/013-property-binding/contracts/PropertyBindings.cs`
- ✅ `specs/013-property-binding/contracts/PropertyChangedEventArgs.cs`
- ✅ `specs/013-property-binding/contracts/BindingMode.cs`
- ✅ `specs/013-property-binding/contracts/README.md`

**Context Updates**:
- ✅ `.github/copilot-instructions.md` - Updated with property binding technology

**Branch**: `013-property-binding`
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
