# Implementation Plan: Directional Layout Components (VerticalLayout & HorizontalLayout)

**Branch**: `007-vertical-layout` | **Date**: November 16, 2025 | **Updated**: November 17, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-vertical-layout/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement VerticalLayout and HorizontalLayout components that arrange child UI elements along their respective axes using a property-based design (no layout mode enums). Both layouts share identical API patterns with axis-specific behavior, using four core properties (ItemHeight/ItemWidth, Spacing, ItemSpacing) plus inherited Alignment for flexible spacing and sizing control. The components extend the existing Container class and integrate with the current layout invalidation system, deferred property updates, and resource management infrastructure.

**Design Evolution**: Original specification proposed a VerticalLayoutMode enum with five modes (StackedTop, StackedMiddle, StackedBottom, SpacedEqually, Justified). Through iterative decomposition into atomic layout properties, the design evolved to eliminate enum-based configuration in favor of composable behavior using four simple properties plus a minimal SpacingMode enum (2 values). This approach provides greater flexibility, simpler implementation, and aligns with industry patterns (CSS Flexbox justify-content/align-items).

## Technical Context

**Language/Version**: C# 9.0+ with .NET 9.0, nullable reference types enabled  
**Primary Dependencies**: Silk.NET (Vulkan, Maths, Windowing), Microsoft.Extensions.DependencyInjection, Roslyn Source Generators  
**Storage**: N/A (in-memory component hierarchy)  
**Testing**: xUnit for unit tests, frame-based integration tests via TestApp  
**Target Platform**: Cross-platform desktop (Windows, Linux, macOS via Vulkan)  
**Project Type**: Single project - game engine library  
**Performance Goals**: Layout recalculation within 1 frame update cycle, 60 fps rendering for layouts with 100+ children  
**Constraints**: Zero allocation in hot paths (rendering loops), component-based architecture, source-generated properties for animation support  
**Scale/Scope**: Support 1-100+ child elements per VerticalLayout, nested layouts supported

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Documentation-First TDD Workflow
- Build verification: Required before implementation
- Documentation updates: Will update relevant .docs/ files and test documentation
- Test generation: Unit tests required before implementation
- Red → Green → Refactor cycle: Will be followed

### ✅ Component-Based Architecture
- VerticalLayout extends Container (existing IRuntimeComponent)
- Uses ContentManager for child creation (never ComponentFactory directly)
- Follows declarative template pattern with [TemplateProperty] attributes
- Lifecycle managed by ContentManager

### ✅ Source-Generated Animated Properties
- VerticalLayoutMode property will use [ComponentProperty] and [TemplateProperty] attributes
- Leverages existing source generators for deferred updates
- No custom interpolation needed (enum property)

### ✅ Vulkan Resource Management
- N/A - Layout component does not directly manage Vulkan resources
- Children manage their own rendering resources

### ✅ Explicit Approval Required
- All changes require explicit approval
- Separate files for each class/interface
- Use dotnet CLI for project file edits

**Result**: ✅ NO VIOLATIONS - All constitution principles are satisfied. VerticalLayout follows existing patterns and architecture.

**Post-Phase-1 Re-evaluation**: ✅ CONFIRMED - Design documents (research.md, data-model.md, contracts/api-contract.md, quickstart.md) confirm adherence to all constitution principles. No new violations introduced.

## Project Structure

### Documentation (this feature)

```text
specs/007-vertical-layout/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command) - NEEDS UPDATE
├── data-model.md        # Phase 1 output (/speckit.plan command) - NEEDS UPDATE
├── quickstart.md        # Phase 1 output (/speckit.plan command) - NEEDS UPDATE
├── contracts/           # Phase 1 output (/speckit.plan command) - NEEDS UPDATE
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/GameEngine/
├── GUI/
│   └── Layout/
│       ├── Container.cs              # Base class (existing)
│       ├── VerticalLayout.cs         # Component implementation (existing - needs enhancement)
│       ├── HorizontalLayout.cs       # New component implementation (mirror of VerticalLayout)
│       ├── SpacingMode.cs            # New enum for spacing distribution (2 values)
│       └── ILayout.cs                # Interface (existing)

Tests/GameEngine/GUI/Layout/
├── Container.Tests.cs                # Existing tests
├── VerticalLayout.Tests.cs           # Enhanced unit tests for property-based design
└── HorizontalLayout.Tests.cs         # New unit tests (mirror of VerticalLayout tests)

TestApp/Tests/
├── VerticalLayoutTests.cs            # Integration tests for property combinations
└── HorizontalLayoutTests.cs          # Integration tests for horizontal layouts
```

**Structure Decision**: Single project structure using existing `src/GameEngine/GUI/Layout/` directory. VerticalLayout already exists with basic stacking behavior; this feature enhances it with four property-based controls. HorizontalLayout is new but follows the identical pattern on the horizontal axis. Unit tests follow the existing pattern in `Tests/GameEngine/GUI/Layout/`, and integration tests use the TestApp's frame-based testing infrastructure.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations detected. This section is not applicable.

---

## Planning Phase Completion Summary

**Status**: ✅ COMPLETE  
**Date**: November 16, 2025  
**Branch**: `007-vertical-layout`

### Artifacts Generated

#### Phase 0: Research
- ✅ `research.md` - Comprehensive research covering 10 decision areas:
  - Existing VerticalLayout implementation analysis
  - Layout mode enumeration design
  - Content area calculation patterns
  - Child measurement and positioning strategies
  - Five layout mode algorithms (StackedTop, StackedMiddle, StackedBottom, SpacedEqually, Justified)
  - Invalidation and update lifecycle integration
  - Source generator integration
  - Two-level testing strategy
  - Performance considerations
  - Backward compatibility approach

#### Phase 1: Design & Contracts
- ✅ `data-model.md` - Complete data model specification:
  - VerticalLayout component entity with properties and methods
  - VerticalLayoutMode enumeration with 5 values
  - Component hierarchy and ownership model
  - Layout update flow diagrams
  - Five detailed layout algorithms with pseudocode
  - Edge case handling (empty children, overflow, negative spacing)
  - Template configuration examples
  - Performance characteristics

- ✅ `contracts/api-contract.md` - Public API contract:
  - VerticalLayout class API surface
  - VerticalLayoutMode enum contract
  - Template API (VerticalLayoutTemplate record)
  - Behavioral contracts for all five layout modes
  - Invalidation contract and timing guarantees
  - Event contracts (PropertyChanged, Animation events)
  - Thread safety guarantees
  - Backward compatibility promises
  - Example usage patterns
  - Version history

- ✅ `quickstart.md` - Developer guide:
  - Basic usage examples for all five layout modes
  - Programmatic creation patterns
  - Dynamic update scenarios
  - Advanced configurations (nesting, ItemHeight, SafeArea)
  - Common UI patterns (forms, loading screens, settings menus)
  - Troubleshooting guide
  - Next steps and API reference links

- ✅ Agent context updated:
  - Ran `update-agent-context.ps1 -AgentType copilot`
  - Updated `.github/copilot-instructions.md` with technology stack
  - Preserved manual additions between markers

### Constitution Compliance

**Initial Check**: ✅ PASSED (no violations)  
**Post-Design Check**: ✅ PASSED (no new violations introduced)

All five constitution principles satisfied:
1. ✅ Documentation-First TDD - Plan follows TDD workflow
2. ✅ Component-Based Architecture - Extends Container, uses ContentManager
3. ✅ Source-Generated Animated Properties - Uses [ComponentProperty] attributes
4. ✅ Vulkan Resource Management - N/A (layout component)
5. ✅ Explicit Approval Required - All changes documented for review

### Key Decisions Made

1. **Property-based design** - No layout mode enum, use composable properties instead
2. **Dual-layout implementation** - VerticalLayout and HorizontalLayout with identical API patterns
3. **Four core properties** - ItemHeight/ItemWidth, Spacing, ItemSpacing
4. **Minimal enum** - SpacingMode with only 2 values (Justified, Distributed)
5. **Inherited alignment** - Use Alignment.Y/X for remaining space distribution
6. **Zero-size collapse** - Children with zero measured size excluded from layout
7. **Clear precedence** - ItemHeight > Measure() for sizing
8. **No proportional growth** - Equal stretching only; GridLayout will handle proportional sizing
9. **Reuse Container patterns** - Inherit invalidation and content area calculation
10. **Two-level testing** - Unit tests for property combinations, integration tests for real scenarios

### Next Steps

**Phase 2** (separate command: `/speckit.tasks`):
- Generate tasks.md with implementation checklist
- Break down into atomic tasks for:
  - SpacingMode enum creation
  - VerticalLayout property implementation
  - HorizontalLayout component creation
  - Layout algorithm implementation
  - Unit test implementation (property combinations, edge cases)
  - Integration test implementation (real UI scenarios)
  - Documentation updates (all 5 planning documents)

**Implementation** (after Phase 2):
1. Build solution and verify clean build
2. Update relevant documentation (research.md, data-model.md, quickstart.md, contracts/api-contract.md)
3. Create SpacingMode.cs enum (2 values)
4. Write failing unit tests for property combinations
5. Implement VerticalLayout with 4 properties + UpdateLayout() algorithm
6. Implement HorizontalLayout (mirror of VerticalLayout on horizontal axis)
7. Run tests (Red → Green)
8. Create integration tests in TestApp
9. Rebuild and address warnings/errors
10. Provide summary with manual testing instructions

### Files Generated

```
specs/007-vertical-layout/
├── plan.md                          ✅ This file (updated with final property-based design)
├── research.md                      ⏳ NEEDS UPDATE (remove enum modes, add properties)
├── data-model.md                    ⏳ NEEDS UPDATE (add HorizontalLayout, update properties)
├── quickstart.md                    ⏳ NEEDS UPDATE (property-based examples)
└── contracts/
    └── api-contract.md              ⏳ NEEDS UPDATE (SpacingMode enum, both layouts)
```

**Total Documentation**: ~600 lines across 5 files (pending updates)

**Planning Command Status**: Design evolution complete. Documentation updates needed before `/speckit.tasks` execution.

---

## Design Evolution: From Enum Modes to Composable Properties (Nov 17, 2025)

### Original Design (Enum-Based)

The initial specification proposed five layout modes via `VerticalLayoutMode` enum:
- **StackedTop**: Align children to top with fixed spacing
- **StackedMiddle**: Center stacked children vertically
- **StackedBottom**: Align children to bottom with fixed spacing
- **SpacedEqually**: Distribute spacing evenly (space-between pattern)
- **Justified**: Stretch children to fill container height

**Problem**: Enum-based design violates Open/Closed Principle and limits flexibility. Cannot combine behaviors (e.g., centered children with calculated spacing).

### Iterative Decomposition

Through systematic analysis, layout behavior was decomposed into four atomic decisions:
1. **Child Sizing**: Fixed height override vs intrinsic size vs stretch-to-fill
2. **Spacing Calculation**: Fixed spacing vs space-between vs space-evenly
3. **Vertical Positioning**: Top-aligned vs centered vs bottom-aligned (remaining space distribution)
4. **Layout Direction**: Top-to-bottom (natural order, no reverse flag needed)

### Final Property-Based Design

**VerticalLayout** (4 properties + 1 inherited):
```csharp
[ComponentProperty] public uint? ItemHeight { get; set; }        // Fixed height for all children
[ComponentProperty] public SpacingMode Spacing { get; set; }     // Distribution when ItemSpacing is null
[ComponentProperty] public uint? ItemSpacing { get; set; }       // Fixed spacing between children
// Inherited: Alignment.Y (float -1 to 1) - Remaining space distribution when ItemSpacing is set
```

**HorizontalLayout** (identical pattern on horizontal axis):
```csharp
[ComponentProperty] public uint? ItemWidth { get; set; }         // Fixed width for all children
[ComponentProperty] public SpacingMode Spacing { get; set; }     // Distribution when ItemSpacing is null
[ComponentProperty] public uint? ItemSpacing { get; set; }       // Fixed spacing between children
// Inherited: Alignment.X (float -1 to 1) - Remaining space distribution when ItemSpacing is set
```

**SpacingMode** (minimal enum with 2 values):
```csharp
public enum SpacingMode
{
    Justified,    // Space between items only (first at start, last at end)
    Distributed   // Space before, between, and after items (equal everywhere)
}
```

### Property Interactions

| Property | Behavior | Overrides | When Applied |
|----------|----------|-----------|--------------|
| ItemHeight (set) | Force all children to fixed height | Measure() | Always when set |
| ItemSpacing (set) | Fixed spacing between children | Spacing enum | Always when set |
| Spacing=Justified | Calculate space-between | None | When ItemSpacing=null |
| Spacing=Distributed | Calculate space-evenly | None | When ItemSpacing=null |
| Alignment.Y | Distribute remaining space | None | When ItemSpacing is set |

**Zero-height children**: Excluded from layout entirely (collapsed).

### Mapping Original Modes to Properties

| Original Mode | ItemHeight | ItemSpacing | Spacing | Alignment.Y |
|---------------|------------|----------------|-------------|---------|-------------|
| StackedTop | null | 10 | (ignored) | -1 |
| StackedMiddle | null | 10 | (ignored) | 0 |
| StackedBottom | null | 10 | (ignored) | 1 |
| SpacedEqually | null | null | Justified | (ignored) |
| Justified | (ignored) | null or value | (ignored) | (ignored) |

**Plus many new combinations** not possible with original enum design!

### Benefits of Property-Based Design

1. **Composability**: Mix and match behaviors (e.g., fixed height + centered + calculated spacing)
2. **Simplicity**: Each property has single clear responsibility
3. **Flexibility**: 4 properties create dozens of valid combinations vs 5 rigid modes
4. **Predictability**: Clear precedence rules (ItemHeight > Measure)
5. **Industry Alignment**: Matches CSS Flexbox patterns (justify-content, align-items)
6. **Maintainability**: No switch statements or mode-specific code branches

### Proportional Growth & Future GridLayout

**Decision**: Directional layouts do NOT support proportional child growth (FlexGrow-like ratios).

**Rationale**: 
- Individual Elements can be positioned using Container, directional layouts are only used to display collections
- Uniform and Equal spacing (SpacingMode.Justify, SpacingMode.Distribute) covers >90% of use cases
- Other layout types will be used for more complex scenarios

---

## Planning Phase Completion Summary

**Status**: ✅ DESIGN EVOLVED - Property-based approach finalized  
**Date**: November 16-17, 2025  
**Branch**: `007-vertical-layout`

### Artifacts Generated

#### Phase 0: Research
- ✅ `research.md` - ⏳ NEEDS UPDATE to reflect property-based design (remove enum modes)

#### Phase 1: Design & Contracts
- ✅ `spec.md` - ✅ UPDATED with dual-layout scope and property-based user stories
- ✅ `plan.md` - ✅ UPDATED with final design evolution and property details
- ⏳ `data-model.md` - NEEDS UPDATE (add HorizontalLayout, SpacingMode, remove VerticalLayoutMode)
- ⏳ `quickstart.md` - NEEDS UPDATE (property-based examples, both layouts)
- ⏳ `contracts/api-contract.md` - NEEDS UPDATE (SpacingMode enum, dual layout API)

### Constitution Compliance

**Initial Check**: ✅ PASSED (no violations)  
**Post-Design Evolution**: ✅ PASSED (property-based design maintains all principles)

All five constitution principles satisfied:
1. ✅ Documentation-First TDD - Plan updated with new design before implementation
2. ✅ Component-Based Architecture - Both layouts extend Container, use ContentManager
3. ✅ Source-Generated Animated Properties - Uses [ComponentProperty] attributes
4. ✅ Vulkan Resource Management - N/A (layout components)
5. ✅ Explicit Approval Required - All design changes documented for review

### Key Decisions Made

1. **Property-based design** - No layout mode enum, use composable properties instead
2. **Dual-layout implementation** - VerticalLayout and HorizontalLayout with identical API patterns
3. **Four core properties** - ItemHeight/ItemWidth, Spacing, ItemSpacing
4. **Minimal enum** - SpacingMode with only 2 values (Justified, Distributed)
5. **Inherited alignment** - Use Alignment.Y/X for remaining space distribution
6. **Zero-size collapse** - Children with zero measured size excluded from layout
7. **Clear precedence** - ItemHeight > Measure() for sizing
8. **No proportional growth** - Equal stretching only; GridLayout will handle proportional sizing
9. **Reuse Container patterns** - Inherit invalidation and content area calculation
10. **Two-level testing** - Unit tests for property combinations, integration tests for real scenarios

### Next Steps
