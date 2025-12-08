# Implementation Plan: PerformanceMonitor UI Template

**Branch**: `015-performance-monitor` | **Date**: 2025-12-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/015-performance-monitor/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Create a PerformanceMonitor UI template that displays real-time performance metrics (FPS, frame time, subsystem timings) as an on-screen overlay. The implementation uses template composition with UserInterfaceElement, TextRenderer, and layout controllers, connected via property bindings to the existing PerformanceMonitor component. This spec includes implementation of VerticalLayoutController and HorizontalLayoutController (base infrastructure exists, concrete implementations were removed in previous refactor).

## Technical Context

**Language/Version**: C# 9.0+ with .NET 9.0, nullable reference types enabled  
**Primary Dependencies**: Silk.NET (Vulkan bindings), Microsoft.Extensions.DependencyInjection, xUnit  
**Storage**: N/A (in-memory performance data only)  
**Testing**: xUnit for unit tests, frame-based integration tests via TestApp  
**Target Platform**: Windows/Linux desktop with Vulkan support
**Project Type**: Game engine library with test applications  
**Performance Goals**: 60 FPS target (16.67ms per frame), overlay rendering <1ms overhead  
**Constraints**: Zero allocation in hot paths, deferred property updates, resource caching required  
**Scale/Scope**: Single PerformanceMonitor overlay per scene, 6-10 text elements for metrics display

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Documentation-First TDD
- ✅ **PASS**: Spec exists (`spec.md`), plan created before implementation
- ✅ **PASS**: Testing strategy defined (unit tests + frame-based integration tests)
- ⚠️ **PENDING**: Documentation updates required (update README.md, create quickstart.md in Phase 1)
- ⚠️ **PENDING**: Tests must be written before implementation (Red-Green-Refactor)

### II. Component-Based Architecture
- ✅ **PASS**: Uses template composition (UserInterfaceElement + TextRenderer + LayoutControllers)
- ✅ **PASS**: Leverages IRuntimeComponent system via existing components
- ✅ **PASS**: ContentManager pattern for template instantiation
- ✅ **PASS**: Declarative template-based configuration, no custom component needed
- ✅ **PASS**: LayoutController pattern for sibling-based layout (not container-based)

### III. Source-Generated Animated Properties
- ✅ **PASS**: PerformanceMonitor uses [ComponentProperty] for metrics (CurrentFps, AverageFps, etc.)
- ✅ **PASS**: Property bindings will sync to TextRenderer.Text properties
- ✅ **PASS**: Deferred updates via ComponentPropertyUpdater system
- ✅ **PASS**: TextRenderer uses [ComponentProperty] and [TemplateProperty] for Text, Color, Visible
- ℹ️ **NOTE**: No animation/interpolation needed (discrete metric updates)

### IV. Vulkan Resource Management
- ✅ **PASS**: No direct Vulkan resource creation in template
- ✅ **PASS**: TextRenderer rendering delegates to IRenderer and IPipelineManager
- ✅ **PASS**: Font atlas resources managed by IResourceManager
- ℹ️ **NOTE**: Resource management handled by underlying components, not template

### V. Explicit Approval Required
- ✅ **PASS**: TextRenderer exists (`src/GameEngine/GUI/TextRenderer.cs`)
- ✅ **PASS**: LayoutController base class exists (`src/GameEngine/GUI/Layout/LayoutController.cs`)
- ✅ **PASS**: Layout enums exist (SpacingMode, VerticalLayoutMode, HorizontalLayoutMode)
- ⚠️ **TO IMPLEMENT**: VerticalLayoutController (within this spec)
- ⚠️ **TO IMPLEMENT**: HorizontalLayoutController (within this spec)
- ✅ **PASS**: StringFormatConverter exists (`src/GameEngine/Data/StringFormatConverter.cs`)
- ✅ **PASS**: PerformanceMonitor component exists with required properties

**GATE STATUS**: ✅ **PASSING** - Proceed to Phase 1 (design and contracts)

**Scope Additions**:
1. Implement VerticalLayoutController extending LayoutController
2. Implement HorizontalLayoutController extending LayoutController  
3. Create PerformanceMonitor UI template using composition

## Project Structure

### Documentation (this feature)

```text
specs/015-performance-monitor/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification (already exists)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command - API schemas if needed)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── GameEngine/
│   ├── Performance/
│   │   └── PerformanceMonitor.cs           # EXISTS - data source component
│   ├── GUI/
│   │   ├── UserInterfaceElement.cs         # EXISTS - positioning container
│   │   ├── TextRenderer.cs                 # EXISTS - text rendering component
│   │   └── Layout/
│   │       ├── LayoutController.cs          # EXISTS - base class
│   │       ├── SpacingMode.cs               # EXISTS - enum
│   │       ├── VerticalLayoutMode.cs        # EXISTS - enum
│   │       ├── HorizontalLayoutMode.cs      # EXISTS - enum
│   │       ├── VerticalLayoutController.cs  # TO CREATE - vertical layout
│   │       └── HorizontalLayoutController.cs # TO CREATE - horizontal layout
│   ├── Data/
│   │   └── StringFormatConverter.cs        # EXISTS - value converter
│   ├── Components/
│   │   ├── PropertyBinding.cs              # EXISTS - binding system
│   │   └── Template.cs                     # EXISTS - template system
│   └── Templates/
│       └── PerformanceMonitorTemplate.cs   # TO CREATE - UI template
├── TestApp/
│   └── Templates/
│       └── PerformanceMonitorExample.cs    # TO CREATE - example usage
└── Tests/
    ├── GameEngine/
    │   └── GUI/
    │       └── Layout/
    │           ├── VerticalLayoutController.Tests.cs   # TO CREATE - unit tests
    │           └── HorizontalLayoutController.Tests.cs # TO CREATE - unit tests
    └── IntegrationTests/
        └── PerformanceMonitorTests.cs      # TO CREATE - integration tests
```

**Structure Decision**: Single project structure (game engine library). Layout controllers belong in `src/GameEngine/GUI/Layout/` namespace alongside existing LayoutController base class. PerformanceMonitor template belongs in `src/GameEngine/Templates/` for reusable UI compositions. Test code follows mirror structure in `Tests/GameEngine/` for unit tests and `Tests/IntegrationTests/` for frame-based tests.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations. Scope includes additional components beyond original spec:

| Additional Component | Justification | Complexity |
|---------------------|---------------|------------|
| VerticalLayoutController | Layout controller infrastructure exists (base class, enums), concrete implementation was removed in refactor. Required for template composition. | Low - 4-6 hours |
| HorizontalLayoutController | Implemented alongside vertical for completeness and symmetry. Useful for future UI features. | Low - 4-6 hours |

**Rationale for Inclusion**: 
- Base LayoutController class already exists with abstract `UpdateLayout()` method
- Enums (SpacingMode, VerticalLayoutMode, HorizontalLayoutMode) already defined
- Implementation is straightforward child positioning logic
- Essential for clean template-based UI composition
- Better to implement both layout controllers together than separately

---

## Phase Completion Status

### Phase 0: Research ✅ COMPLETE

**Deliverables**:
- ✅ `research.md` created with all technical unknowns resolved
- ✅ Component verification complete (TextRenderer exists, layout controllers need implementation)
- ✅ Architecture decisions documented
- ✅ Technology choices validated
- ✅ No external blockers identified

**Key Findings**:
- TextRenderer exists (replaces TextElement from older specs)
- LayoutController base infrastructure exists
- VerticalLayoutController and HorizontalLayoutController to be implemented within this spec
- PerformanceMonitor fully implemented with all required properties

---

### Phase 1: Design & Contracts ✅ COMPLETE

**Deliverables**:
- ✅ `data-model.md` created with entity definitions, relationships, state transitions
- ✅ `contracts/component-contracts.md` created with interface contracts and behavior specifications
- ✅ `quickstart.md` created with usage guide and examples
- ✅ Agent context updated (`.github/copilot-instructions.md`)

**Entities Defined**:
- VerticalLayoutController (properties, behavior, validation rules)
- HorizontalLayoutController (properties, behavior, validation rules)
- PerformanceMonitorTemplate (composition structure, bindings, lifecycle)

**Contracts Defined**:
- Component interface contracts (VerticalLayoutController, HorizontalLayoutController)
- Template composition contract (required structure)
- Property binding contract (sources, targets, lifecycle)
- Performance contract (timing guarantees, allocation limits)
- Error handling contract (validation, runtime errors)

---

### Phase 2: Tasks ⏸️ PENDING

**Next Command**: `/speckit.tasks` - Generate implementation tasks breakdown

This will create `tasks.md` with:
- Implementation checklist for VerticalLayoutController
- Implementation checklist for HorizontalLayoutController
- Implementation checklist for PerformanceMonitor template
- Unit test requirements
- Integration test requirements
- Documentation updates

**Note**: Phase 2 is NOT executed by `/speckit.plan` command. Stop here and report completion.

---

## Summary

**Planning Complete**: ✅  
**Branch**: `015-performance-monitor`  
**Status**: Ready for implementation (after `/speckit.tasks` generates task breakdown)

**Generated Artifacts**:
1. `plan.md` - This file (complete)
2. `research.md` - Technical research and decisions (complete)
3. `data-model.md` - Entity definitions and data flow (complete)
4. `contracts/component-contracts.md` - Interface contracts (complete)
5. `quickstart.md` - Usage guide (complete)
6. Agent context updated

**Next Steps**:
1. Run `/speckit.tasks` to generate implementation task breakdown
2. Follow TDD workflow: Documentation → Tests → Implementation
3. Implement VerticalLayoutController and HorizontalLayoutController
4. Create PerformanceMonitor template composition
5. Add tests and examples