# Implementation Plan: Text Rendering with TextElement

**Branch**: `002-text-rendering` | **Date**: 2025-11-04 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-text-rendering/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement text rendering system with TextElement component that displays text using font atlases generated from embedded TrueType fonts. The system must generate a shared font atlas geometry buffer (380 vertices for 95 ASCII printable characters) that is cached and reused by all TextElements, with each element emitting per-glyph DrawCommands that reference different offsets in the shared buffer. The implementation leverages Element's existing positioning system (Position, AnchorPoint, Size, Scale) and reuses the existing UIElement shader, requiring no new shaders. Primary technical challenges are FreeType library integration for glyph rasterization, efficient font atlas texture/geometry generation with pre-baked UV coordinates, and implementing per-glyph WorldMatrix calculations for positioning.

## Technical Context

**Language/Version**: C# 9.0+ with nullable reference types enabled, .NET 9.0  
**Primary Dependencies**: Silk.NET (Vulkan bindings), FreeType library wrapper (StbTrueTypeSharp or similar), Microsoft.Extensions.DependencyInjection  
**Storage**: GPU memory (Vulkan buffers/textures), embedded assembly resources (TrueType fonts)  
**Testing**: xUnit for unit tests, frame-based integration tests via TestApp with pixel sampling  
**Target Platform**: Windows (primary), cross-platform support deferred (requires VulkanSDK with glslc)  
**Project Type**: Single game engine project with component-based architecture  
**Performance Goals**: Font atlas generation <100ms at startup, batching N text DrawCommands into 1 GPU draw call, zero geometry uploads for dynamic text changes  
**Constraints**: 512x512 atlas texture for ASCII printable at 16pt, 6KB shared geometry per font (not per-element), push constants <128 bytes (96 bytes for model/tint/uvRect)  
**Scale/Scope**: MVP supports single default font (Roboto Regular 16pt), 95 ASCII printable characters, single-line left-to-right text only

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Documentation-First TDD ✅ PASS
- **Requirement**: Update documentation BEFORE code, write tests BEFORE implementation
- **Compliance**: Spec includes comprehensive testing requirements, acceptance scenarios with pixel sampling validation, and mandates documentation updates
- **Action**: Follow prescribed workflow (build verification → docs → tests → red phase → implementation → green phase → rebuild)

### Principle II: Component-Based Architecture ✅ PASS
- **Requirement**: All systems follow IRuntimeComponent pattern, use templates, lifecycle management, ContentManager for children
- **Compliance**: TextElement extends Element (IRuntimeComponent), uses TextElementTemplate with [TemplateProperty], integrates with ContentManager/ComponentFactory
- **Action**: Ensure TextElement implements lifecycle methods (OnActivate, OnDeactivate), uses ContentManager for child creation

### Principle III: Source-Generated Animated Properties ✅ PASS
- **Requirement**: Properties requiring animation/deferred updates use [ComponentProperty] attribute
- **Compliance**: Spec specifies Text property will use [ComponentProperty] for runtime updates
- **Action**: Mark Text field with [ComponentProperty] to enable source-generated property updates

### Principle IV: Vulkan Resource Management ✅ PASS
- **Requirement**: Resources managed through IResourceManager, cached and reused, disposal handled by managers
- **Compliance**: Spec defines IFontResourceManager following IResourceManager pattern, caches font atlas texture + geometry, reference counting for disposal
- **Action**: Implement IFontResourceManager.Fonts property, integrate with existing IResourceManager interface

### Principle V: Explicit Approval Required ✅ PASS
- **Requirement**: Do not change code without explicit instructions, ask if uncertain
- **Compliance**: This is a planning phase command, no code changes yet
- **Action**: Present generated plan for approval before proceeding to implementation

### Architecture Constraints ✅ PASS
- **Technology Stack**: Uses C# 9.0+/.NET 9.0, Vulkan via Silk.NET, dotnet CLI, xUnit + TestApp - all compliant
- **Application Startup**: Spec doesn't modify startup pattern, adds services via .AddSingleton<IFontResourceManager, FontResourceManager>()
- **Performance Standards**: Implements batch rendering (N DrawCommands batch to 1 GPU call), resource caching (shared geometry), zero-allocation for text changes

### Testing Infrastructure ✅ PASS
- **Unit Tests**: Spec requires unit tests for font atlas generation, resource caching, geometry generation
- **Integration Tests**: Spec requires TestApp frame-based tests with pixel sampling for "Hello World" validation
- **Test Coverage**: Acceptance scenarios cover all functional requirements

**GATE RESULT**: ✅ ALL CHECKS PASSED - Proceeding to Phase 0 research

---

### Post-Design Constitution Re-evaluation

**Phase 1 Complete - Re-checking all principles:**

### Principle I: Documentation-First TDD ✅ PASS
- **Status**: Generated comprehensive documentation: research.md (6 research findings), data-model.md (6 entities + relationships), quickstart.md (usage guide), API contracts (interface + schema)
- **Next**: Follow TDD workflow during implementation (docs updated, tests written, red phase, implementation, green phase)

### Principle II: Component-Based Architecture ✅ PASS
- **Status**: TextElement design follows IRuntimeComponent pattern with proper lifecycle methods (OnActivate, OnDeactivate), template-based configuration, ContentManager integration
- **Validation**: Data model shows TextElement extends Element, uses TextElementTemplate, integrates with IContentManager

### Principle III: Source-Generated Animated Properties ✅ PASS
- **Status**: Text property marked with [TemplateProperty] and [ComponentProperty] for source generation
- **Validation**: Data model specifies both attributes on _text field for template configuration and runtime updates

### Principle IV: Vulkan Resource Management ✅ PASS
- **Status**: IFontResourceManager contract follows IResourceManager pattern with GetOrCreate/Release semantics, reference counting, caching
- **Validation**: FontResource manages GPU resources (texture, geometry), integrates with GeometryResourceManager and IBufferManager

### Principle V: Explicit Approval Required ✅ PASS
- **Status**: Planning phase complete, awaiting implementation approval
- **Action**: Present plan, research, data-model, contracts, and quickstart for review before proceeding to Phase 2 (tasks)

**POST-DESIGN GATE RESULT**: ✅ ALL CHECKS PASSED - Ready for Phase 2 task breakdown

## Project Structure

### Documentation (this feature)

```text
specs/002-text-rendering/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification (input)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── IFontResourceManager.cs     # Font resource manager interface contract
│   └── FontAtlasStructure.json     # Font atlas data structure definition
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/GameEngine/
├── Components/
│   └── GUI/
│       └── TextElement.cs           # [REPLACE] Complete rewrite of text rendering component
├── Resources/
│   ├── IResourceManager.cs          # [UPDATE] Add Fonts property
│   ├── ResourceManager.cs           # [UPDATE] Implement Fonts property
│   ├── Fonts/                       # [NEW] Font resource management
│   │   ├── IFontResourceManager.cs  # Font resource manager interface
│   │   ├── FontResourceManager.cs   # Font resource manager implementation
│   │   ├── FontResource.cs          # GPU font atlas + geometry + metrics
│   │   ├── FontDefinition.cs        # Font configuration record
│   │   ├── GlyphInfo.cs            # Per-character glyph metrics
│   │   ├── FontAtlasBuilder.cs     # Atlas texture generation utility
│   │   └── FontSource/              # Font data source abstractions
│   │       ├── IFontSource.cs       # Font source interface
│   │       └── EmbeddedTrueTypeFontSource.cs  # Embedded resource loader
│   └── Geometry/
│       └── GeometryResourceManager.cs  # [UPDATE] Support shared font atlas geometry caching
├── EmbeddedResources/
│   └── Fonts/
│       └── Roboto-Regular.ttf       # [NEW] Default embedded TrueType font
└── Shaders/
    ├── ui.vert              # [EXISTING] Reused for text rendering
    └── ui.frag              # [EXISTING] Reused for text rendering

TestApp/
├── TestComponents/
│   └── TextElement/                 # [NEW] Text rendering integration tests
│       ├── HelloWorldTest.cs        # Basic "Hello World" rendering test
│       ├── FontAtlasSharingTest.cs  # Resource sharing validation
│       └── TextAnchorPointTest.cs   # Anchor point alignment tests
└── Resources/
    └── Fonts/                       # [OPTIONAL] Test fonts if needed

Tests/
├── FontResourceManagerTests.cs      # [NEW] Unit tests for font resource management
├── FontAtlasBuilderTests.cs        # [NEW] Unit tests for atlas generation
├── TextElementTests.cs             # [NEW] Unit tests for TextElement component
└── GlyphPositioningTests.cs        # [NEW] Unit tests for per-glyph WorldMatrix calculation
```

**Structure Decision**: Single game engine project (Option 1) with component-based architecture. Text rendering feature adds new Components/GUI/TextElement (complete replacement), new Resources/Fonts/ subsystem for font resource management, embedded Roboto-Regular.ttf font resource, and comprehensive test coverage via TestApp integration tests + xUnit unit tests. Existing UIElement shader is reused without modification, requiring no shader changes.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations detected** - All constitution principles and architecture constraints are satisfied.

---

## Phase 0 & Phase 1 Completion Summary

### Deliverables Generated ✅

1. **plan.md** (this file): Complete implementation plan with technical context, constitution checks, and project structure
2. **research.md**: 6 research findings resolving all technical unknowns:
   - Font library selection (StbTrueTypeSharp chosen)
   - Atlas packing strategy (row-based packing)
   - Texture format (R8_UNORM)
   - Shared geometry best practices
   - Per-glyph WorldMatrix calculation
   - UIElement shader compatibility verification
3. **data-model.md**: Complete entity definitions with 6 core entities:
   - TextElement (component)
   - FontResource (GPU resources)
   - FontDefinition (configuration)
   - GlyphInfo (per-character metrics)
   - FontMetrics (font-level measurements)
   - IFontSource (abstraction + implementation)
4. **contracts/**: API contracts defining implementation interfaces:
   - `IFontResourceManager.cs`: Font resource manager interface with detailed documentation
   - `FontAtlasStructure.json`: JSON schema defining font atlas data structure
5. **quickstart.md**: Developer usage guide with examples:
   - Hello World example
   - Positioning scenarios (top-left, top-right, centered, etc.)
   - Scaling, performance characteristics, troubleshooting
6. **Agent Context Update**: Added StbTrueTypeSharp and font rendering technology to `.github/copilot-instructions.md`

### Key Design Decisions

1. **Shared Geometry Optimization**: One 6KB vertex buffer per font (not per-element) → 12× memory savings
2. **StbTrueTypeSharp Library**: Pure C# port avoids native library complexities
3. **UIElement Shader Reuse**: Zero new shader work, proven system
4. **Row-Based Atlas Packing**: Simple, sufficient for ASCII printable at 16pt
5. **R8_UNORM Texture Format**: 75% smaller than RGBA8, widely supported
6. **Per-Glyph WorldMatrix**: Leverages Element positioning system, efficient push constants

### Performance Projections

- **Font Atlas Generation**: <100ms (one-time per font at startup)
- **Memory per Font**: ~272KB (shared across all TextElements)
- **Memory per TextElement**: ~124 bytes + text string
- **100 TextElements**: ~285KB total (vs 27MB without optimization!)
- **GPU Draw Calls**: N DrawCommands batch to 1 GPU call

### Risks Mitigated

- **Library Integration**: Pure C# StbTrueTypeSharp avoids native dependencies
- **Atlas Size**: 512x512 sufficient for ASCII printable, documented limits
- **Text Clarity**: Documented bitmap scaling artifacts, SDF for future
- **Memory Leaks**: Rigorous testing plan for descriptor set lifecycle
- **Shader Compatibility**: Verified UIElement shader supports all requirements

### Next Steps

**Phase 2: Task Breakdown** (NOT part of `/speckit.plan` command):
- Run `/speckit.tasks` command to generate detailed implementation tasks
- Break down implementation into atomic units of work
- Assign priorities and dependencies
- Create development checklist

**Implementation workflow**:
1. Verify build succeeds
2. Update project documentation (following TDD workflow)
3. Add StbTrueTypeSharp NuGet package
4. Embed Roboto-Regular.ttf font resource
5. Implement font resource management subsystem
6. Generate unit tests (red phase)
7. Implement TextElement (green phase)
8. Create integration tests in TestApp
9. Verify all tests pass
10. Manual testing and validation

---

## Command Completion Report

**Feature**: Text Rendering with TextElement  
**Branch**: `002-text-rendering`  
**Command**: `/speckit.plan`  
**Status**: ✅ COMPLETE

**Generated Artifacts**:
- ✅ `specs/002-text-rendering/plan.md` (this file)
- ✅ `specs/002-text-rendering/research.md` (6 research findings)
- ✅ `specs/002-text-rendering/data-model.md` (6 entities + relationships)
- ✅ `specs/002-text-rendering/quickstart.md` (developer usage guide)
- ✅ `specs/002-text-rendering/contracts/IFontResourceManager.cs` (API contract)
- ✅ `specs/002-text-rendering/contracts/FontAtlasStructure.json` (data schema)
- ✅ `.github/copilot-instructions.md` (updated with StbTrueTypeSharp)

**Constitution Compliance**: ✅ ALL CHECKS PASSED (pre-design and post-design)

**Ready for**: Phase 2 task breakdown via `/speckit.tasks` command

---

**END OF PLAN DOCUMENT**
