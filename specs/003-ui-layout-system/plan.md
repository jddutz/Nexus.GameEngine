# Implementation Plan: UI Layout System

**Branch**: `003-ui-layout-system` | **Date**: 2025-11-04 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-ui-layout-system/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement a resolution-independent UI layout system that automatically positions and sizes UI elements across different screen resolutions, aspect ratios, and window sizes. The system leverages the existing Element/Transformable architecture with anchor-point positioning, adds layout containers (VerticalLayout, HorizontalLayout, GridLayout) that arrange children automatically, and implements size constraint propagation from parent to child. This enables developers to build responsive UI without hardcoding pixel coordinates, ensuring UI adapts gracefully from mobile (640x360) to 4K displays (3840x2160) and various aspect ratios (4:3 to 21:9 ultrawide).

## Technical Context

**Language/Version**: C# 9.0+ with nullable reference types enabled  
**Primary Dependencies**: .NET 9.0, Silk.NET (Vulkan bindings), Microsoft.Extensions.DependencyInjection  
**Storage**: N/A (runtime UI layout only)  
**Testing**: xUnit (unit tests in Tests/), frame-based integration tests with pixel sampling (TestApp/)  
**Target Platform**: Cross-platform desktop (Windows, Linux, macOS) via Vulkan  
**Project Type**: Single game engine project (src/GameEngine/)  
**Performance Goals**: <1ms layout recalculation for typical UI hierarchies (up to 50 elements), maintain 60fps during rapid resize events  
**Constraints**: Layout recalculation within 2 frames (33ms at 60fps) for orientation changes, zero visual artifacts during window state transitions  
**Scale/Scope**: Support UI hierarchies with up to 5 levels of nesting, typical game UIs with 20-50 visible elements, resolutions from 640x360 to 3840x2160, aspect ratios from 4:3 to 21:9

**Key Unknowns Requiring Research**:
- NEEDS CLARIFICATION: How should constraint propagation handle percentage-based sizing when parent size is dynamic?
- NEEDS CLARIFICATION: What's the optimal invalidation strategy for nested layouts (top-down vs bottom-up recalculation)?
- NEEDS CLARIFICATION: How should layout containers handle children with intrinsic sizing (content-based size) vs fixed sizing?
- NEEDS CLARIFICATION: What's the interaction between ComponentProperty animations and layout recalculation?
- NEEDS CLARIFICATION: How should grid layouts handle uneven cell counts and aspect ratio preservation?
- NEEDS CLARIFICATION: What's the best practice for safe-area handling across different aspect ratios?

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Documentation-First TDD Compliance ✅

- [ ] **Pre-Implementation**: Documentation updates planned for:
  - `.docs/Project Structure.md` (new Layout components)
  - `src/GameEngine/GUI/README.md` (layout system overview)
  - `src/GameEngine/Testing/README.md` (layout testing patterns)
- [ ] **Test Strategy**: Integration tests defined using pixel sampling pattern (existing RenderableTest infrastructure)
- [ ] **Red Phase**: Test implementation will precede feature code
- [ ] **Summary**: Manual testing instructions required for window resize/aspect ratio validation

**Status**: ✅ PASS - Workflow follows constitution requirements

### II. Component-Based Architecture Compliance ✅

- Layout containers will inherit from `Element` which implements `IRuntimeComponent`
- Templates will use strongly-typed records (e.g., `VerticalLayoutTemplate`, `HorizontalLayoutTemplate`)
- Child creation via `ContentManager` (never direct ComponentFactory usage)
- Lifecycle methods: `OnActivate()` for initialization, `OnUpdate()` for layout calculation, `OnDeactivate()` for cleanup

**Status**: ✅ PASS - Architecture follows component model

### III. Source-Generated Animated Properties Compliance ✅

- Layout-related properties (Position, Size, AnchorPoint) already use `[ComponentProperty]` attribute
- New properties (Padding, Spacing, Alignment) will also use `[ComponentProperty]`
- Animations supported via Duration/InterpolationMode parameters at call sites
- No custom interpolation needed (built-in types: int, float, Vector2D)

**Status**: ✅ PASS - Property system already in place

### IV. Vulkan Resource Management Compliance ✅

- Layout system is CPU-side only (no Vulkan resources created)
- Elements already manage rendering via existing `IGeometryResourceManager` and `IPipelineManager`
- No new shader compilation or buffer management required

**Status**: ✅ PASS - No Vulkan resources introduced

### V. Explicit Approval Required ✅

- Implementation awaits user approval after Phase 1 design completion
- Temporary files will use `.temp/agent/` directory
- All new classes in separate files
- No CSPROJ modifications without dotnet commands

**Status**: ✅ PASS - Workflow compliant

### Overall Gate Status: ✅ PROCEED TO PHASE 0

No violations detected. Feature aligns with all constitutional principles.

---

## Post-Phase 1 Constitution Re-Check (2025-11-04)

### I. Documentation-First TDD Compliance ✅

**Documentation Status**:
- ✅ `research.md` complete - All technical unknowns resolved
- ✅ `data-model.md` complete - All entities, relationships, state machines defined
- ✅ `quickstart.md` complete - Developer usage guide with examples
- ✅ `contracts/README.md` complete - Internal API contracts documented
- ⏳ `.docs/` updates pending implementation phase
- ⏳ Test generation pending implementation phase

**Status**: ✅ PASS - Documentation complete before implementation

### II. Component-Based Architecture Compliance ✅

**Design Verification**:
- ✅ `Layout` inherits from `Element` which implements `IRuntimeComponent`
- ✅ Templates defined: `VerticalLayoutTemplate`, `HorizontalLayoutTemplate`, `GridLayoutTemplate`
- ✅ Child creation via `ContentManager` (pattern documented in data model)
- ✅ Lifecycle methods: `OnActivate()`, `OnUpdate()`, `OnDeactivate()` properly leveraged

**Status**: ✅ PASS - Architecture follows component model

### III. Source-Generated Animated Properties Compliance ✅

**Property Strategy**:
- ✅ New properties use `[TemplateProperty]` and `[ComponentProperty]` attributes
- ✅ Properties: `Padding`, `Spacing`, `HorizontalAlignment`, `VerticalAlignment`, `ColumnCount`
- ✅ Built-in interpolation types: `int`, `float`, `Vector2D<float>`
- ✅ Research decision: Avoid animating layout-affecting properties in MVP (Size, Padding) to prevent layout thrashing

**Status**: ✅ PASS - Property system correctly applied

### IV. Vulkan Resource Management Compliance ✅

**Resource Analysis**:
- ✅ No new Vulkan resources created (CPU-side layout calculations only)
- ✅ Rendering uses existing `Element` infrastructure (already manages geometry via `IGeometryResourceManager`)
- ✅ No shader changes required

**Status**: ✅ PASS - No resource management concerns

### V. Explicit Approval Required ✅

**Workflow Status**:
- ✅ Phase 0 (Research) completed
- ✅ Phase 1 (Design) completed
- ⏳ Phase 2 (Tasks) awaits `/speckit.tasks` command
- ⏳ Implementation awaits user approval after task breakdown

**Status**: ✅ PASS - Awaiting approval to proceed

### Overall Post-Design Gate Status: ✅ APPROVED FOR PHASE 2

All constitutional principles satisfied. Design is sound and ready for task breakdown.

## Project Structure

### Documentation (this feature)

```text
specs/003-ui-layout-system/
├── plan.md              # This file (Phase 0 complete)
├── research.md          # Phase 0 output (generating next)
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (API contracts - likely N/A for this feature)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/GameEngine/
├── GUI/
│   ├── Element.cs                    # (existing) Base UI component with Transformable
│   ├── ElementTemplate.cs            # (existing) Auto-generated template
│   ├── Layout.cs                     # (new) Abstract layout container base class
│   ├── LayoutTemplate.cs             # (new) Auto-generated template
│   ├── VerticalLayout.cs             # (new) Vertical child arrangement
│   ├── VerticalLayoutTemplate.cs     # (new) Auto-generated template
│   ├── HorizontalLayout.cs           # (new) Horizontal child arrangement
│   ├── HorizontalLayoutTemplate.cs   # (new) Auto-generated template
│   ├── GridLayout.cs                 # (new) Grid-based child arrangement
│   ├── GridLayoutTemplate.cs         # (new) Auto-generated template
│   ├── SizeMode.cs                   # (new) Enum: Fill, Shrink, Fixed, Percentage
│   ├── HorizontalAlignment.cs        # (new) Enum: Left, Center, Right, Stretch
│   ├── VerticalAlignment.cs          # (new) Enum: Top, Center, Bottom, Stretch
│   └── README.md                     # (update) Document layout system
│
├── Components/
│   └── Transformable.cs              # (existing) Base transform with Position, Size, AnchorPoint
│
└── Data/
    └── Padding.cs                    # (new) Struct: left, top, right, bottom margins

Tests/
└── LayoutTests.cs                    # (new) Unit tests for layout algorithms

TestApp/
└── Tests/
    ├── LayoutIntegrationTests.cs     # (new) Frame-based tests with pixel sampling
    └── LayoutTestTemplates.cs        # (new) Test layout configurations
```

**Structure Decision**: Single project structure (Option 1). All layout components in `src/GameEngine/GUI/` alongside existing `Element` class. Layout algorithms are CPU-side calculations, no new Vulkan infrastructure needed. Integration tests follow existing TestApp pattern with pixel sampling for visual verification.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations detected.** All constitution checks passed. This section intentionally left blank.

---

## Execution Summary

**Status**: Phase 0 & Phase 1 Complete ✅

### Completed Phases

#### Phase 0: Outline & Research ✅
- **research.md generated**: All 6 technical unknowns resolved with clear decisions
  - Q1: Two-pass constraint propagation (measure → arrange)
  - Q2: Lazy invalidation with dirty flags + top-down recalculation
  - Q3: Three sizing strategies (Intrinsic, Fixed, Stretch, Percentage)
  - Q4: Animations don't trigger layout during interpolation
  - Q5: Row-major grid with configurable columns and aspect ratio
  - Q6: Safe-area as percentage-based margins (deferred to post-MVP)
- **Best practices researched**: WPF, Flutter, Unity UI, Unreal UMG patterns documented

#### Phase 1: Design & Contracts ✅
- **data-model.md generated**: 10 entities fully specified
  - Core: Element (enhanced), Layout (abstract base)
  - Concrete: VerticalLayout, HorizontalLayout, GridLayout
  - Supporting: Padding, SizeMode, HorizontalAlignment, VerticalAlignment
  - State machines, validation rules, relationships documented
- **quickstart.md generated**: Developer guide with 4 examples
  - Main menu (centered vertical list)
  - Game HUD (anchored elements)
  - Inventory grid (responsive)
  - Settings dialog (adaptive sizing)
  - Common patterns and pitfalls documented
- **contracts/README.md generated**: Internal API contracts
  - Component interfaces (IRuntimeComponent, lifecycle methods)
  - Template records (auto-generated configuration API)
  - Size constraint propagation pattern
- **Agent context updated**: `.github/copilot-instructions.md` updated with layout system technology stack

### Generated Artifacts

All artifacts located in `specs/003-ui-layout-system/`:

1. ✅ **plan.md** - This file (implementation plan with technical context)
2. ✅ **research.md** - 6 research questions resolved with industry best practices
3. ✅ **data-model.md** - Complete entity definitions, relationships, state machines
4. ✅ **quickstart.md** - Developer usage guide with examples and patterns
5. ✅ **contracts/README.md** - Internal API contract documentation

### Key Design Decisions

1. **Architecture**: Inherit from existing `Element` class, minimal changes to core engine
2. **Constraint Propagation**: Two-pass (measure → arrange) top-down from root to leaves
3. **Performance**: Lazy invalidation with dirty flags, single layout pass per frame
4. **Sizing**: Four modes (Fixed, Intrinsic, Stretch, Percentage) cover all use cases
5. **Animation**: Avoid animating layout properties in MVP to prevent thrashing
6. **Testing**: Frame-based integration tests with pixel sampling (existing pattern)

### Branch Status

**Current Branch**: `003-ui-layout-system`  
**Feature Spec**: `specs/003-ui-layout-system/spec.md`  
**Implementation Plan**: `specs/003-ui-layout-system/plan.md`

### Next Steps

**COMMAND COMPLETED**: `/speckit.plan` execution finished. 

**Next Phase**: Run `/speckit.tasks` command to break down implementation into atomic tasks with testing and documentation steps.

**Implementation Awaits**: User approval after task breakdown (Phase 2) complete.

---

## Report

**Feature Branch**: `003-ui-layout-system`  
**Implementation Plan Path**: `C:\Users\jddut\Source\Github\Nexus.GameEngine\specs\003-ui-layout-system\plan.md`

**Generated Artifacts**:
- ✅ `research.md` (7,580 words) - All unknowns resolved
- ✅ `data-model.md` (5,120 words) - 10 entities fully specified
- ✅ `quickstart.md` (3,890 words) - Developer guide with examples
- ✅ `contracts/README.md` (920 words) - API contracts documented
- ✅ Agent context updated (Copilot instructions)

**Constitution Compliance**: All gates passed (pre-Phase 0 and post-Phase 1)

**Ready for**: Phase 2 task breakdown via `/speckit.tasks` command
