# Implementation Plan: Refactor Entity/Component Lifecycle

**Branch**: `008-component-lifecycle-refactor` | **Date**: 2025-11-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-component-lifecycle-refactor/spec.md`

## Summary

Refactor the Entity/Component lifecycle to ensure deterministic initialization order (Root-to-Leaf property application followed by hooks), separate responsibilities between `ComponentFactory` (Loading) and `ContentManager` (Validation/Activation), and introduce a layout update phase.

## Technical Context

**Language/Version**: C# 12 (.NET 8)
**Primary Dependencies**: Microsoft.CodeAnalysis.CSharp (Source Generators)
**Testing**: xUnit
**Target Platform**: Windows (Vulkan)
**Project Type**: Game Engine / Source Generator
**Performance Goals**: Zero-allocation property updates where possible.
**Constraints**: Must maintain backward compatibility with existing component definitions where possible.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **No Breaking Changes without Approval**: Refactor changes lifecycle timing but maintains API surface (mostly).
- [x] **Test Coverage**: New lifecycle must be verified with tests.
- [x] **Documentation**: Spec and Plan updated.

## Project Structure

### Documentation (this feature)

```text
specs/008-component-lifecycle-refactor/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
