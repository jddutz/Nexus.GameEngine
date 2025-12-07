# Implementation Plan: Component Base Class Consolidation

**Branch**: `014-component-consolidation` | **Date**: December 6, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/014-component-consolidation/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Consolidate four base classes (`Entity`, `Configurable`, `Component`, `RuntimeComponent`) into a single `Component` class using partial class declarations. Rename `IComponent` to `IComponentHierarchy` and split `IRuntimeComponent` into `IActivatable` and `IUpdatable` for better separation of concerns. Create a new unified `IComponent` interface that combines all constituent interfaces. This refactoring eliminates deep inheritance hierarchy while maintaining all existing functionality through well-defined interfaces.

## Technical Context

**Language/Version**: C# 9.0+ with nullable reference types enabled  
**Primary Dependencies**: .NET 9.0, Silk.NET (Vulkan bindings), Microsoft.Extensions.DependencyInjection  
**Storage**: N/A (component architecture refactoring)  
**Testing**: xUnit for unit tests, frame-based integration tests via TestApp  
**Target Platform**: Windows/Linux desktop (Vulkan-capable)  
**Project Type**: Single game engine solution  
**Performance Goals**: Zero runtime overhead from consolidation (compile-time only change), maintain 60 fps rendering  
**Constraints**: Must preserve all existing functionality, backward compatibility not required (breaking change accepted)  
**Scale/Scope**: Core architecture affecting ~100+ component types, ~50+ source files referencing base classes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ I. Documentation-First TDD
- Will follow exact sequence: Build → Documentation → Tests → Implementation
- Documentation updates required:
  - `.docs/Project Structure.md` (component hierarchy changes)
  - `README.md` (if component usage examples affected)
  - `src/GameEngine/Testing/README.md` (if test patterns change)
- Unit tests will be updated for new class/interface names
- Integration tests will verify behavioral equivalence

### ✅ II. Component-Based Architecture
- Consolidation preserves component-based architecture
- `IRuntimeComponent` functionality moves to `IComponent` (unified interface)
- Interface segregation maintained through constituent interfaces
- ContentManager/ComponentFactory separation unchanged
- Template-based configuration unchanged

### ✅ III. Source-Generated Animated Properties
- `[ComponentProperty]` attribute system unchanged
- Source generators will be updated to reference new class names
- Deferred updates and interpolation behavior preserved
- No changes to property generation logic

### ✅ IV. Vulkan Resource Management
- Resource management layer unaffected by component consolidation
- No changes to IResourceManager or related interfaces
- Component disposal patterns unchanged

### ✅ V. Explicit Approval Required
- Awaiting explicit approval to proceed with consolidation
- Will use `.temp/agent/` for planning artifacts
- Separate files for each partial class definition

### Architecture Constraints Check
- ✅ C# partial classes supported (standard language feature)
- ✅ .NET 9.0 compatibility maintained
- ✅ No new dependencies introduced
- ✅ Source generators will be updated in Analyzers/SourceGenerators projects

**GATE STATUS**: ✅ PASS - No constitution violations. This is a structural refactoring that preserves all architectural principles.

## Project Structure

### Documentation (this feature)

```text
specs/014-component-consolidation/
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
│   ├── Component.Identity.cs       # NEW: Identity functionality (from Entity)
│   ├── Component.Configuration.cs  # NEW: Configuration/validation (from Configurable)
│   ├── Component.Hierarchy.cs      # NEW: Parent-child relationships (from Component)
│   ├── Component.Lifecycle.cs      # NEW: Activation/update lifecycle (from RuntimeComponent)
│   ├── IEntity.cs                  # EXISTING: Preserved
│   ├── ILoadable.cs                # EXISTING: Preserved
│   ├── IValidatable.cs             # EXISTING: Preserved
│   ├── IComponentHierarchy.cs      # RENAMED from IComponent.cs
│   ├── IActivatable.cs             # NEW: Split from IRuntimeComponent
│   ├── IUpdatable.cs               # NEW: Split from IRuntimeComponent
│   ├── IComponent.cs               # REPLACED: New unified interface (old interface renamed to IComponentHierarchy)
│   └── [DELETE]
│       ├── Entity.cs               # TO BE REMOVED
│       ├── Configurable.cs         # TO BE REMOVED
│       ├── Component.cs            # TO BE REMOVED
│       └── RuntimeComponent.cs     # TO BE REMOVED
├── SourceGenerators/
│   ├── TemplateGenerator.cs        # UPDATE: Reference new Component class
│   ├── ComponentPropertyGenerator.cs # UPDATE: Reference new Component class
│   └── AnimatedPropertyGenerator.cs  # UPDATE: Reference new Component class

Tests/GameEngine/Components/
├── Component.Identity.Tests.cs     # NEW: Tests for identity functionality
├── Component.Configuration.Tests.cs # NEW: Tests for configuration functionality
├── Component.Hierarchy.Tests.cs    # NEW: Tests for hierarchy functionality
├── Component.Lifecycle.Tests.cs    # NEW: Tests for lifecycle functionality
└── [UPDATE all existing component tests to use new Component class]

src/TestApp/
└── [UPDATE integration tests to verify behavioral equivalence]
```

**Structure Decision**: Single project structure with component consolidation. All partial class files co-located in `Components/` directory for easy navigation. Source generators updated in `SourceGenerators/` project. Tests mirror source structure in `Tests/GameEngine/Components/`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No complexity violations identified. This refactoring reduces complexity by:
- Eliminating 4-level inheritance hierarchy
- Using C# partial classes (standard language feature, not additional complexity)
- Maintaining clear separation of concerns through interfaces
- Improving code navigability and discoverability

---

## Post-Design Constitution Re-evaluation

*Re-checked after Phase 1 design completion (data-model.md, contracts/, quickstart.md)*

### ✅ I. Documentation-First TDD
- **Status**: PASS - Documentation artifacts created before implementation
- **Evidence**: 
  - `research.md` created (Phase 0)
  - `data-model.md` created (Phase 1)
  - `contracts/` created with 4 interface contracts (Phase 1)
  - `quickstart.md` created (Phase 1)
- **Next**: Implementation will follow TDD sequence (build → update docs → write tests → implement)

### ✅ II. Component-Based Architecture
- **Status**: PASS - Architecture preserved and improved
- **Evidence**:
  - `data-model.md` confirms all component-based patterns preserved
  - Unified `IComponent` interface maintains all lifecycle contracts
  - ContentManager/ComponentFactory separation unchanged
  - Property binding lifecycle integrated in `IActivatable` contract
- **Improvement**: Better interface segregation with `IActivatable`/`IUpdatable` split

### ✅ III. Source-Generated Animated Properties
- **Status**: PASS - No changes to generation system
- **Evidence**:
  - `data-model.md` confirms `[ComponentProperty]` system unchanged
  - Source generators only need class name updates (simple predicate change)
  - `ApplyUpdates` flow documented in `IUpdatable` contract
  - Generated code structure identical (only target class name changes)

### ✅ IV. Vulkan Resource Management
- **Status**: PASS - Completely unaffected
- **Evidence**: No resource management interfaces or patterns changed

### ✅ V. Explicit Approval Required
- **Status**: PASS - Planning complete, awaiting approval for implementation
- **Evidence**: All Phase 0 and Phase 1 artifacts created in proper locations

### Architecture Constraints Re-check
- ✅ Partial class organization documented in `data-model.md` and `research.md`
- ✅ Interface consolidation pattern validated in `contracts/IComponent.md`
- ✅ Migration strategy documented in `research.md` Question 3
- ✅ File organization follows industry standards (`research.md` Question 5)

**FINAL GATE STATUS**: ✅ PASS - Design phase complete. All architectural principles preserved and improved. Ready for implementation phase (Phase 2) after explicit approval.
