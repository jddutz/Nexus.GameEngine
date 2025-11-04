# Technical Implementation Plan: Template Generation

## Feature ID
`001-template-generation`

---

## Architecture Overview

### High-Level Flow

```
[ComponentProperty] attributes
          ↓
Roslyn Source Generator
          ↓
    TemplateGenerator
          ↓
    ┌─────────────────────────────┐
    ├─ {Component}.Template.g.cs   │ Template record
    ├─ {Component}.OnLoad.g.cs     │ OnLoad method
    └─ Validation errors/warnings  │ Compiler diagnostics
          ↓
   Generated Code Combined
          ↓
   Component + Template + OnLoad
```

### Technology Stack

**Language**: C# 13
**Framework**: .NET 8+
**Code Generation**: Roslyn (Microsoft.CodeAnalysis)
**Generator Type**: Incremental Source Generator (IIncrementalGenerator)
**Testing**: xUnit with Roslyn TestHelpers
**Analyzers**: DiagnosticAnalyzer for validation

---

## Component Architecture

### 1. Base Template Record
**File**: `src/GameEngine/Components/Template.cs`
**Scope**: Manual (not generated)

```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Base template for all component configuration.
/// Provides common properties for component creation and hierarchy.
/// </summary>
public record Template
{
    /// <summary>
    /// The type of component to create from this template.
    /// Each generated template overrides this property.
    /// </summary>
    public virtual Type? ComponentType { get; set; } = null;
    
    /// <summary>
    /// Optional component name. Defaults to component type name.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Child component templates for hierarchical component creation.
    /// </summary>
    public Template[] Subcomponents { get; set; } = [];
}
```

### 2. TemplateGenerator Source Generator
**File**: `src/GameEngine.SourceGenerators/TemplateGenerator.cs`
**Type**: IIncrementalGenerator

**Responsibilities**:
- Scan for classes with `[ComponentProperty]` fields
- Extract field metadata (type, name, default value)
- Generate template records with properties
- Generate OnLoad methods with field assignments
- Generate ComponentType property overrides

**Key Methods**:
- `Initialize()` - Register incremental pipeline
- `GetClassesToGenerate()` - Filter classes with ComponentProperty fields
- `GenerateTemplate()` - Create template record code
- `GenerateOnLoad()` - Create OnLoad method code

### 3. ComponentProperty Validation Analyzer
**File**: `src/GameEngine.Analyzers/ComponentPropertyValidator.cs`
**Type**: DiagnosticAnalyzer

**Rules**:
- **NX3001 (Error)**: ComponentProperty on public field
- **NX3002 (Error)**: ComponentProperty field has no initializer
- **NX3003 (Warning)**: ComponentProperty field doesn't start with underscore

### 4. Updated ComponentFactory
**File**: `src/GameEngine/Components/ComponentFactory.cs`
**Changes**:
- Update `IComponentFactory.CreateInstance()` to accept `Template` parameter
- Read `ComponentType` from template to determine component class
- Use `GetRequiredService<T>` with ComponentType
- Call `Load(template)` on created component

---

## Implementation Phases

### Phase 1: Foundation (Hours: 4-6)

**Goals**: Set up base infrastructure and create manual Template record

**Tasks**:
1. Create `src/GameEngine/Components/Template.cs`
   - Base record with ComponentType, Name, Subcomponents
   - Properly documented with XML comments

2. Update `IComponent` interface
   - Ensure `Load(Template)` method exists
   - Update signature if needed

3. Create `src/GameEngine.SourceGenerators/TemplateGenerator.cs` (stub)
   - Basic IIncrementalGenerator implementation
   - Detection logic for ComponentProperty fields
   - Output routing (not implemented yet)

**Verification**:
- [ ] Base Template compiles
- [ ] IComponent.Load accepts Template
- [ ] TemplateGenerator registers with incremental pipeline
- [ ] Solution builds without errors

---

### Phase 2: Template Generation (Hours: 6-8)

**Goals**: Implement core template record generation

**Tasks**:
1. Extract ComponentProperty field metadata
   - Field name, type, default value
   - XML documentation
   - Handle complex types (generics, custom types)

2. Generate template record code
   - Create typed properties from fields
   - Preserve default values
   - Generate ComponentType override property
   - Add XML documentation

3. Handle type resolution
   - Use fully qualified names
   - Handle aliased types
   - Resolve nested types correctly

4. File generation and naming
   - Output: `{ComponentName}.Template.g.cs`
   - Namespace: Same as component
   - Format with proper C# conventions

**Verification**:
- [ ] Unit tests for template generation
- [ ] Simple single-property component generates correct template
- [ ] Multiple properties generate correctly
- [ ] Default values preserved
- [ ] XML docs transferred
- [ ] ComponentType property generated

---

### Phase 3: OnLoad Generation (Hours: 5-7)

**Goals**: Implement OnLoad method generation with partial hooks

**Tasks**:
1. Generate OnLoad method signature
   - Override from base
   - Call `base.OnLoad()`
   - Template property type-checking

2. Direct field assignment
   - Assign template property values to private fields
   - No Set method calls
   - Handle nullable types naturally

3. Partial method hook
   - Generate call to `OnLoad(DerivedTemplate template)`
   - Compiler handles missing method (safe)
   - Type-safe typed parameter

4. Integration with existing methods
   - Works with component inheritance
   - Compatible with existing OnLoad implementations
   - Calls to `ApplyUpdates()` after Load()

**Verification**:
- [ ] Unit tests for OnLoad generation
- [ ] Fields assigned correctly from template
- [ ] Partial method hook compiled correctly
- [ ] Works with component inheritance chains
- [ ] Existing components' Load() phase still works

---

### Phase 4: Validation Analyzer (Hours: 3-4)

**Goals**: Compile-time validation of ComponentProperty usage

**Tasks**:
1. Create ComponentPropertyValidator analyzer
   - NX3001: Public field error
   - NX3002: No initializer error
   - NX3003: Naming convention warning

2. Clear error messages
   - Explain the rule
   - Suggest fix
   - Link to documentation

3. Test scenarios
   - Public field violation
   - Missing initializer
   - Underscore naming
   - Valid cases pass

**Verification**:
- [ ] Errors appear in IDE
- [ ] Correct line numbers
- [ ] Messages are clear
- [ ] False positives eliminated

---

### Phase 5: Unit Tests (Hours: 6-8)

**Goals**: Comprehensive test coverage (TDD approach)

**Test Categories**:

1. **Template Generation Tests** (15-20 tests)
   - Single property
   - Multiple properties
   - Different types (Vector, Quaternion, arrays, etc.)
   - Default values (simple, complex expressions)
   - Nullable properties
   - XML documentation

2. **OnLoad Generation Tests** (10-15 tests)
   - Field assignment correctness
   - Partial method hook calling
   - Inheritance chains
   - Nullable handling
   - Multiple properties

3. **Type Resolution Tests** (8-10 tests)
   - Fully qualified names
   - Generic types
   - Nested types
   - Custom types
   - Array types

4. **Validation Tests** (8-10 tests)
   - Public field errors
   - Missing initializers
   - Naming conventions
   - Valid patterns pass

5. **Integration Tests** (5-8 tests)
   - Full component + template + OnLoad
   - Factory integration
   - Real component scenarios

**Test Infrastructure**:
- Use Roslyn TestHelpers
- Snapshot testing for generated code
- Assertion helpers for code structure

**Verification**:
- [ ] All tests pass
- [ ] >90% code coverage for generator
- [ ] Real components work as expected

---

### Phase 6: ComponentFactory Updates (Hours: 2-3)

**Goals**: Integrate templates with factory

**Tasks**:
1. Update factory signature
   - `CreateInstance(Template template)` method
   - Backwards compatibility if needed (discuss)

2. ComponentType extraction
   - Read from template
   - Validate type exists
   - Clear error messages

3. Service resolution
   - Use `GetRequiredService<T>` with ComponentType
   - Handle missing services
   - DI integration

**Verification**:
- [ ] Factory creates correct component type
- [ ] ComponentType override works
- [ ] Clear errors for invalid types

---

### Phase 7: Migration (Hours: 8-12)

**Goals**: Migrate existing components to generated templates

**Components to Migrate** (in order of dependency):
1. Transformable
2. Drawable
3. Element
4. Border
5. Input components (KeyBinding, MouseBinding, GamepadBinding)
6. Layout components (GridLayout, HorizontalLayout, VerticalLayout)
7. Camera components (OrthoCamera, PerspectiveCamera, StaticCamera)
8. Background layer components

**For Each Component**:
1. Remove manual Template record
2. Verify generated template appears
3. Update OnLoad if needed (add partial hook)
4. Test with existing templates in TestApp
5. Verify no functional changes

**Verification**:
- [ ] All components compile
- [ ] Generated templates in correct namespace
- [ ] TestApp templates still work
- [ ] No runtime errors

---

### Phase 8: Integration Testing (Hours: 4-6)

**Goals**: Verify end-to-end functionality

**Test Scenarios**:
1. TestApp loads all components with generated templates
2. Template property values flow through to components
3. ComponentFactory creates components from templates
4. Inheritance chains work correctly
5. Custom OnLoad hooks execute
6. Validation errors appear in IDE

**Verification**:
- [ ] TestApp builds and runs
- [ ] All component tests pass
- [ ] No regressions in existing functionality

---

### Phase 9: Documentation & Cleanup (Hours: 3-4)

**Goals**: Update project documentation and clean up

**Tasks**:
1. Update `.docs/Deferred Property Generation System.md`
   - Add template generation section
   - Show before/after examples
   - Document new workflow

2. Update `.github/copilot-instructions.md`
   - Document ComponentProperty usage
   - Template discovery and naming
   - Partial hook pattern

3. Update `README.md`
   - Mention template generation
   - Link to relevant docs

4. Code cleanup
   - Review generated code formatting
   - Ensure no leftover debug code
   - Check all files for completeness

**Verification**:
- [ ] Documentation is clear
- [ ] Examples are accurate
- [ ] No typos or broken links

---

## Risk Mitigation

### Risk 1: Complex Type Resolution
**Risk**: Roslyn might struggle with complex generic/nested types
**Mitigation**: 
- Extensive testing with real types from codebase
- Use `ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)`
- Document limitations

### Risk 2: Partial Method Calling
**Risk**: Partial method semantics might cause issues
**Mitigation**:
- Test with and without hook defined
- Ensure compiler optimization works
- Document expected behavior

### Risk 3: Breaking Changes
**Risk**: Templates move from nested to namespace level
**Mitigation**:
- Use case search to find all template usages
- Plan migration carefully
- Update TestApp templates as first test

### Risk 4: Performance
**Risk**: Incremental generation might be slower with many components
**Mitigation**:
- Use incremental source generators (IIncrementalGenerator)
- Benchmark with full project
- Optimize if needed

---

## Build Configuration

### Compiler Settings
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis" Version="4.x" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.x" />
</ItemGroup>

<PropertyGroup>
    <LangVersion>13</LangVersion>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

### Project Structure
```
src/GameEngine.SourceGenerators/
├── TemplateGenerator.cs          (new)
├── TemplateAnalyzer.cs          (new)
└── SourceGeneratorHelpers.cs    (helper utilities)

src/GameEngine/
├── Components/
│   ├── Template.cs              (new manual base)
│   ├── ComponentFactory.cs       (updated)
│   └── ... other components

Tests/
├── SourceGenerators/            (new folder)
│   ├── TemplateGeneratorTests.cs
│   └── ComponentPropertyAnalyzerTests.cs
```

---

## Testing Strategy

### Unit Tests (TDD)
1. Write tests first (Red Phase)
2. Implement generator (Green Phase)
3. Refactor for clarity (Refactor Phase)

### Integration Tests
- Full component generation from real ComponentProperty fields
- End-to-end TestApp scenario
- Inheritance chain handling

### Manual Testing
- IDE error validation
- Template discovery
- Runtime component creation

---

## Timeline Estimate

| Phase | Hours | Estimated Dates |
|-------|-------|-----------------|
| 1. Foundation | 5 | Day 1 |
| 2. Template Gen | 7 | Day 2 |
| 3. OnLoad Gen | 6 | Day 2-3 |
| 4. Validation | 3.5 | Day 3 |
| 5. Unit Tests | 7 | Day 3-4 |
| 6. Factory | 2.5 | Day 4 |
| 7. Migration | 10 | Day 4-5 |
| 8. Integration | 5 | Day 5 |
| 9. Docs | 3.5 | Day 5-6 |
| **TOTAL** | **49.5 hours** | **~6 days** |

---

## Success Criteria

✅ All tests pass
✅ All components successfully migrated
✅ TestApp builds and runs
✅ No compile-time errors
✅ Generated templates in correct namespaces
✅ ComponentFactory uses ComponentType correctly
✅ Documentation complete and clear
✅ Code review approved

---

**Plan Status**: ✅ COMPLETE
**Ready for**: Task Breakdown Phase
**Date**: 2025-11-01
