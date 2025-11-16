# Implementation Plan: Layout Alignment Refactor

**Branch**: `005-layout-alignment-refactor` | **Date**: November 14, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-layout-alignment-refactor/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Refactor HorizontalLayout and VerticalLayout to use a unified `Alignment` property (Vector2D<float>) instead of separate axis-specific alignment properties. For HorizontalLayout, only the Y component (vertical alignment) is used; for VerticalLayout, only the X component (horizontal alignment) is used. The alignment values use the -1 to 1 range where -1 represents start (left/top), 0 represents center, and 1 represents end (right/bottom). This change simplifies the layout API and provides consistency across both layout types.

## Technical Context

**Language/Version**: C# 12 / .NET 9.0  
**Primary Dependencies**: Silk.NET.Maths (Vector2D<T>, Rectangle<T>), Microsoft.Extensions.DependencyInjection  
**Storage**: N/A (UI layout calculations are runtime-only)  
**Testing**: xUnit for unit tests, TestApp for frame-based integration tests  
**Target Platform**: Cross-platform (.NET 9.0 - Windows, Linux, macOS)  
**Project Type**: Single project (GameEngine library with GUI subsystem)  
**Performance Goals**: Layout updates must complete within single frame budget (16.67ms @ 60fps), minimal allocations in UpdateLayout() hot path  
**Constraints**: Must maintain backward compatibility with existing Container base class, preserve source generator compatibility for [ComponentProperty] attributes, align with existing Align static class constants (-1 to 1 range)  
**Scale/Scope**: 2 layout classes (HorizontalLayout, VerticalLayout), affects ~10 unit tests, minimal impact on integration tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Documentation-First TDD
- **Status**: PASS
- **Verification**: Feature spec exists at `specs/005-layout-alignment-refactor/spec.md` with complete user scenarios, functional requirements, and success criteria. Plan workflow will generate research.md, data-model.md, contracts/, and quickstart.md before implementation.

### ✅ Component-Based Architecture
- **Status**: PASS
- **Verification**: Refactor targets existing `HorizontalLayout` and `VerticalLayout` components which already inherit from `Container` (which inherits from `Element` which implements `IRuntimeComponent`). Changes are localized to property definitions and layout algorithm, preserving existing component lifecycle.

### ✅ Source-Generated Animated Properties
- **Status**: PASS
- **Verification**: New `Alignment` property will use `[ComponentProperty]` and `[TemplateProperty]` attributes, consistent with existing `_padding`, `_spacing`, and `_safeArea` properties in Container base class. Source generators will handle property generation automatically.

### ✅ Vulkan Resource Management
- **Status**: PASS (Not Applicable)
- **Verification**: Layout refactor does not involve Vulkan resources. Layout components arrange children via `SetSizeConstraints()` calls; they do not directly manage graphics resources.

### ✅ Explicit Approval Required
- **Status**: PASS
- **Verification**: This plan execution follows the explicit workflow defined in `speckit.plan.prompt.md`. All code changes will be implemented in subsequent tasks after plan approval.

### Summary
All constitution principles are satisfied. No violations require justification. This refactor is a straightforward API modernization within the existing architecture.

## Project Structure

### Documentation (this feature)

```text
specs/005-layout-alignment-refactor/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification (existing)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── IHorizontalLayout.cs
│   └── IVerticalLayout.cs
├── checklists/
│   └── requirements.md  # Existing requirements checklist
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/GameEngine/
├── GUI/
│   ├── Align.cs                    # Existing - static alignment constants (-1 to 1 range)
│   ├── Align.cs      # Existing - static helper class (DEPRECATED after refactor)
│   ├── Align.cs        # Existing - static helper class (DEPRECATED after refactor)
│   └── Layout/
│       ├── Container.cs            # Existing - base class for layouts (provides padding, spacing, content area)
│       ├── HorizontalLayout.cs     # TO MODIFY - change _alignment from float to Vector2D<float>
│       └── VerticalLayout.cs       # TO MODIFY - change _alignment from float to Vector2D<float>

src/Tests/GameEngine/
└── GUI/
    ├── Layout/
    │   ├── HorizontalLayout.Tests.cs  # TO UPDATE - modify tests for new Alignment property
    │   └── VerticalLayout.Tests.cs    # TO UPDATE - modify tests for new Alignment property
    ├── Align.Tests.cs   # Existing (may deprecate)
    └── Align.Tests.cs     # Existing (may deprecate)
```

**Structure Decision**: This is a single-project refactor within the existing GameEngine library. Changes are localized to the GUI/Layout subsystem. The existing Container base class provides the foundation (content area calculation, padding, spacing), while HorizontalLayout and VerticalLayout implement specific arrangement algorithms. The refactor will replace the single-axis `_alignment` field (float) with a two-axis `_alignment` field (Vector2D<float>) in both layout classes, and update the layout calculation logic to use the appropriate axis component (Y for HorizontalLayout, X for VerticalLayout).

## Complexity Tracking

No constitution violations detected. This section is not needed for this feature.

---

## Phase 1 Design Complete ✓

**Date Completed**: November 14, 2025

### Artifacts Generated

#### Phase 0: Research
- ✅ **research.md**: Comprehensive research covering alignment patterns, source generator compatibility, backward compatibility strategy, layout calculation algorithms, StretchChildren interaction, and edge cases

#### Phase 1: Design
- ✅ **data-model.md**: Complete data model with entity definitions, field specifications, behavioral changes, validation rules, state transitions, relationships, and performance considerations
- ✅ **contracts/**: API contract definitions
  - `README.md`: Contract documentation overview
  - `HorizontalLayoutContract.cs`: Public API surface for HorizontalLayout after refactor
  - `VerticalLayoutContract.cs`: Public API surface for VerticalLayout after refactor
- ✅ **quickstart.md**: Quick start guide with usage examples, migration patterns, best practices, and troubleshooting

### Constitution Re-Evaluation

All constitution principles remain satisfied after Phase 1 design:
- ✅ Documentation-First TDD: Complete spec, research, data model, contracts, and quickstart generated before implementation
- ✅ Component-Based Architecture: Design preserves existing component hierarchy and lifecycle
- ✅ Source-Generated Properties: Vector2D<float> type confirmed compatible with existing source generators
- ✅ Vulkan Resource Management: Not applicable (layout components don't manage Vulkan resources)
- ✅ Explicit Approval Required: Design complete, awaiting approval for implementation

### Agent Context Updated

GitHub Copilot context file updated with:
- Language: C# 12 / .NET 9.0
- Framework: Silk.NET.Maths (Vector2D<T>, Rectangle<T>), Microsoft.Extensions.DependencyInjection
- Database: N/A (UI layout calculations are runtime-only)

### Next Steps

1. **Review and Approve**: Review generated artifacts (research.md, data-model.md, contracts/, quickstart.md)
2. **Generate Tasks**: Run `/speckit.tasks` command to generate implementation tasks
3. **Implementation**: Follow TDD workflow as defined in constitution.md:
   - Build solution and verify clean build
   - Update relevant documentation
   - Write failing unit tests (Red phase)
   - Implement changes to make tests pass (Green phase)
   - Rebuild and address warnings/errors
   - Provide summary with manual testing instructions
