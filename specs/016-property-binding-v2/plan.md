# Implementation Plan: Property Binding Framework Revision

**Branch**: `016-property-binding-v2` | **Date**: December 7, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/016-property-binding-v2/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Revise the incomplete property binding framework to provide simplified, high-performance runtime property synchronization using event-driven bindings configured via fluent template syntax. The framework enables developers to declaratively bind component properties with automatic type conversion, multiple source resolution strategies, and proper lifecycle management without requiring source generators. Key improvements include explicit setter delegates, minimal configuration defaults, and proper event subscription cleanup to prevent memory leaks.

## Technical Context

**Language/Version**: C# 9.0+ with nullable reference types enabled, .NET 9.0  
**Primary Dependencies**: 
- Silk.NET 2.22.0 (Vulkan, Input, Windowing, Maths)
- Microsoft.Extensions.DependencyInjection 9.0.9
- Microsoft.Extensions.Configuration 9.0.9
- StbImageSharp 2.27.14
- StbTrueTypeSharp 1.26.12

**Storage**: N/A (in-memory component tree structure)  
**Testing**: xUnit for unit tests, frame-based integration tests via TestApp  
**Target Platform**: Windows (primary), cross-platform via Silk.NET  
**Project Type**: Game Engine Library (component-based architecture)  
**Performance Goals**: 
- Binding updates complete within one frame (16ms at 60fps)
- Zero allocations in hot path (event subscription/unsubscription)
- 80% minimum code coverage

**Constraints**: 
- No source generators for property binding functionality
- Must integrate with existing Component lifecycle (OnLoad, OnActivate, OnDeactivate)
- Must work with ComponentProperty system without conflicts
- Event subscriptions must be properly cleaned up to prevent memory leaks

**Scale/Scope**: 
- Support for ~10-20 bindings per component typical
- Component trees up to 1000+ components
- Binding resolution in <1ms per component activation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Documentation-First Test-Driven Development
- ✅ **PASS**: Feature spec includes mandatory user scenarios with independent tests
- ✅ **PASS**: Plan includes explicit documentation requirements (data-model.md, contracts/, quickstart.md)
- ✅ **PASS**: Testing strategy includes both unit and integration tests with 80% coverage target
- ✅ **COMPLETE**: Phase 0 research.md generated with all architectural decisions documented
- ✅ **COMPLETE**: Phase 1 data-model.md generated with entities, relationships, and state machines
- ✅ **COMPLETE**: Phase 1 contracts/ generated with 4 API contract specifications
- ✅ **COMPLETE**: Phase 1 quickstart.md generated with usage patterns and examples
- 🔄 **PENDING**: Unit tests to be written before implementation (Red → Green → Refactor)

### II. Component-Based Architecture
- ✅ **PASS**: PropertyBinding integrates with `IComponent` lifecycle (OnLoad, OnActivate, OnDeactivate)
- ✅ **PASS**: Bindings configured via template records (`IPropertyBindingDefinition`)
- ✅ **PASS**: Component.PropertyBindings collection managed by lifecycle methods
- ✅ **PASS**: No dependency injection violations (bindings resolve components via tree navigation)
- ✅ **VERIFIED**: Data model confirms clean integration with existing component system

### III. Source-Generated Animated Properties
- ✅ **PASS**: PropertyBinding is independent of `[ComponentProperty]` system
- ✅ **PASS**: FR-011 explicitly states framework operates without automated code generation
- ✅ **VERIFIED**: Research confirms no source generators required, uses explicit setter delegates
- ℹ️ **NOTE**: Bindings may target properties with `[ComponentProperty]` but don't require it

### IV. Vulkan Resource Management
- ✅ **PASS**: Not applicable - PropertyBinding is a pure component framework feature
- ℹ️ **NOTE**: No Vulkan resource dependencies

### V. Explicit Approval Required
- ✅ **PASS**: Using `.temp/agent/` for temporary work files
- ✅ **PASS**: Plan requires explicit approval before implementation begins
- ✅ **PASS**: Each class/interface will be in separate files
- ✅ **VERIFIED**: Project structure section defines clear file organization

### Summary (Post-Phase 1 Re-evaluation)
**GATE STATUS: ✅ PASS**

All constitution principles remain satisfied after Phase 1 design. The detailed design confirms:
1. ✅ Documentation-first workflow followed (research, data model, contracts, quickstart all generated)
2. ✅ Component architecture integration verified through data model and lifecycle diagrams
3. ✅ No source generator dependency confirmed in contracts (explicit `.Set(setter)` method)
4. ✅ No Vulkan dependencies (pure component framework)
5. ✅ File organization and approval process established

**Phase 2 Ready**: All design artifacts complete. Proceed to task breakdown and implementation planning.

No complexity violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/016-property-binding-v2/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/GameEngine/
├── Components/
│   ├── Component.cs                      # Base component with lifecycle
│   ├── Component.PropertyBindings.cs     # PropertyBinding lifecycle integration
│   ├── IComponent.cs                     # Component interface
│   ├── IPropertyBinding.cs               # Non-generic marker interface (EXISTING - REVISE)
│   ├── IPropertyBindingDefinition.cs     # Template binding definition (EXISTING - REVISE)
│   ├── PropertyBinding.cs                # Generic binding class (EXISTING - REVISE)
│   ├── PropertyBindings.cs               # Collection base class (EXISTING - EVALUATE)
│   └── Lookups/
│       ├── ILookupStrategy.cs            # Source resolution strategy interface
│       ├── ParentLookup.cs               # Find nearest parent of type
│       ├── SiblingLookup.cs              # Find sibling of type
│       ├── ChildLookup.cs                # Find child of type
│       ├── NamedObjectLookup.cs          # Find by component name
│       └── ContextLookup.cs              # Find ancestor of type
├── Data/
│   ├── IValueConverter.cs                # Value converter interface (EXISTING - VERIFY)
│   ├── IBidirectionalConverter.cs        # Two-way converter interface (EXISTING - VERIFY)
│   └── StringFormatConverter.cs          # String formatting converter (EXISTING - VERIFY)
└── Events/
    └── PropertyChangedEventArgs.cs       # Generic property change event args

Tests/GameEngine/Components/
├── PropertyBindingTests.cs               # NEW - Core binding functionality tests
├── PropertyBindingLifecycleTests.cs      # NEW - Activation/deactivation tests
├── PropertyBindingConverterTests.cs      # NEW - Type conversion tests
└── Lookups/
    ├── ParentLookupTests.cs              # NEW - Parent resolution tests
    ├── SiblingLookupTests.cs             # NEW - Sibling resolution tests
    └── NamedObjectLookupTests.cs         # NEW - Named lookup tests

src/TestApp/Testing/
└── PropertyBindingIntegrationTests.cs    # NEW - Frame-based integration tests
```

**Structure Decision**: 
This is a single-project library enhancement. The property binding framework is purely a component framework feature with no external API surface. Files are organized under `src/GameEngine/Components/` for binding logic, `src/GameEngine/Data/` for converters, and `src/GameEngine/Events/` for event infrastructure. Tests mirror the source structure in `Tests/GameEngine/` with additional frame-based integration tests in `TestApp/Testing/`.

## Complexity Tracking

> **No violations - this section intentionally left blank**

The Constitution Check passed without violations. No complexity justifications are required.
