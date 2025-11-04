# Template Generation from ComponentProperty Attributes

## Feature ID
`001-template-generation`

## Status
Specification Complete - Ready for Technical Planning

---

## Problem Statement

Developers currently maintain **three duplicate definitions** for each component property:

1. **ComponentProperty attribute** - Marks field for animation/deferred updates
   ```csharp
   [ComponentProperty]
   private Vector3D<float> _position = Vector3D<float>.Zero;
   ```

2. **Template record property** - Mirrors the field type and default for declarative configuration
   ```csharp
   public new record Template : RuntimeComponent.Template
   {
       public Vector3D<float> Position { get; set; } = Vector3D<float>.Zero;
   }
   ```

3. **OnLoad mapping** - Manual method that transfers template values to properties
   ```csharp
   protected override void OnLoad(Configurable.Template? componentTemplate)
   {
       if (componentTemplate is Template template)
           SetPosition(template.Position);
   }
   ```

**Issues with current approach:**
- ❌ Type information duplicated across three locations
- ❌ Default values must be maintained in multiple places
- ❌ Easy to introduce bugs (type mismatch, forgotten properties)
- ❌ Boilerplate code for every component
- ❌ Property names must be kept in sync manually
- ❌ XML documentation scattered across multiple files

---

## Solution

**Auto-generate templates and OnLoad methods** from `[ComponentProperty]` attributes using Roslyn source generators.

### Single Source of Truth

Developers write:
```csharp
public partial class Transformable : RuntimeComponent
{
    [ComponentProperty]
    private Vector3D<float> _position = Vector3D<float>.Zero;
    
    [ComponentProperty]
    private Quaternion<float> _rotation = Quaternion<float>.Identity;
    
    [ComponentProperty]
    private Vector3D<float> _scale = new(1f, 1f, 1f);
    
    // Custom behavior methods...
    public void Translate(Vector3D<float> delta) { }
}
```

Generator produces:
- **TransformableTemplate.g.cs** - Template record with all properties
- **Transformable.OnLoad.g.cs** - Populated OnLoad method
- **Validation** - Compile-time checks for private fields and initializers

---

## Requirements

### Functional Requirements

1. **Generate Template Records**
   - Create record for each class with `[ComponentProperty]` fields
   - Include all fields as properties with matching types
   - Preserve default values from field initializers
   - Include XML documentation from field comments

2. **Generate OnLoad Method**
   - Auto-generate `protected override void OnLoad(Template? template)`
   - Direct field assignment from template properties
   - Call typed partial hook: `OnLoad(DerivedTemplate template)` if defined
   - Support nullable properties without conditionals

3. **Support Template Hierarchy**
   - Base `Template` record in `Nexus.GameEngine.Components`
   - All generated templates inherit directly from `Template`
   - `ComponentType` as get-only virtual property (override per template)
   - Flat hierarchy (no intermediate generated templates)

4. **ComponentFactory Integration**
   - Factory uses `ComponentType` property to create components
   - Support type override for polymorphic component creation
   - Defaults to matching component type (safe default)

5. **Validation at Compile-Time**
   - Error if `[ComponentProperty]` on public field
   - Warning if field doesn't start with underscore
   - Error if field has no initializer

### Quality Requirements

1. **Performance**
   - Incremental generation (only changed files regenerated)
   - No runtime reflection needed
   - Zero allocation for template property mapping

2. **Developer Experience**
   - Clear error messages for violations
   - IDE integration for Roslyn analyzers
   - Generated files in same namespace for discoverability
   - Optional partial hooks (no boilerplate when not needed)

3. **Compatibility**
   - Works with existing deferred property system
   - Compatible with partial method pattern
   - Supports all animatable types
   - Handles nullable reference types

---

## Design Decisions

### Decision 1: Custom OnLoad Hook Pattern
**Choice**: Optional typed partial method
```csharp
// Only call if developer defines this:
partial void OnLoad(ElementTemplate template)
{
    // Custom initialization with access to typed template
}
```
**Rationale**: Avoids scaffolding for 90% of components that don't need custom initialization

### Decision 2: Property Assignment Method
**Choice**: Direct field assignment (not calling Set methods)
```csharp
_position = template.Position;  // Direct assignment
```
**Rationale**: During template loading, direct assignment is appropriate. Deferred updates applied after Load completes.

### Decision 3: ComponentType Property
**Choice**: Get-only virtual property override
```csharp
public override Type? ComponentType => typeof(Transformable);
```
**Rationale**: Explicit, type-safe, no constructor overhead

### Decision 4: Template Hierarchy
**Choice**: All templates inherit directly from base `Template`
```csharp
public record TransformableTemplate : Template { }
public record ElementTemplate : Template { }
```
**Rationale**: Flat hierarchy simpler, no need to discover inheritance, each template self-contained

---

## User Stories

### Story 1: Developer Creates New Component with Properties
**As a** component developer
**I want** to define properties once using `[ComponentProperty]`
**So that** templates are generated automatically without duplication

**Acceptance Criteria:**
- Template record auto-generated with all ComponentProperty fields
- Template file appears in same namespace as component
- All property types and defaults match ComponentProperty declarations
- OnLoad method auto-generated with proper field assignments

### Story 2: Developer Needs Custom OnLoad Logic
**As a** component developer (Element setting up Pipeline)
**I want** to include custom logic in OnLoad without writing the whole method
**So that** I can initialize component-specific resources after template loading

**Acceptance Criteria:**
- Define `partial void OnLoad(ElementTemplate template)` in Element class
- Generator calls this partial method if it exists
- No boilerplate if custom logic isn't needed
- Access to typed template for initialization

### Story 3: Factory Creates Component from Template
**As a** framework user
**I want** the factory to use template's ComponentType to create components
**So that** I can override component type for polymorphic creation

**Acceptance Criteria:**
- ComponentFactory reads ComponentType from template
- Defaults to inferred component type if not overridden
- Can override: `new ElementTemplate { ComponentType = typeof(CustomButton) }`
- Type-safe at compile time

### Story 4: Developer Gets Validation Errors for Invalid Patterns
**As a** component developer
**I want** compile-time errors for invalid ComponentProperty usage
**So that** I catch issues early rather than at runtime

**Acceptance Criteria:**
- Error: ComponentProperty on public field
- Error: ComponentProperty field with no initializer
- Warning: ComponentProperty field not starting with underscore
- Clear error messages with suggestions

---

## Scope

### In Scope
- ✅ Template generation from ComponentProperty attributes
- ✅ OnLoad method generation with field assignment
- ✅ ComponentType property in templates
- ✅ Compiler validation for ComponentProperty usage
- ✅ Partial method hooks for custom initialization
- ✅ XML documentation transfer to templates
- ✅ Support for all animatable types
- ✅ Nullable reference type support
- ✅ Migration of existing components

### Out of Scope
- ❌ Runtime type discovery or reflection
- ❌ Template inheritance from component hierarchy
- ❌ Automatic property change callbacks
- ❌ UI code generation
- ❌ Serialization/deserialization

---

## Success Metrics

1. **Code Reduction**: ~40% less boilerplate per component (no manual templates/OnLoad)
2. **Type Safety**: 100% of property types enforced at compile-time
3. **Zero Defects**: No runtime property mapping errors possible
4. **Developer Experience**: Clear error messages, IDE support, no learning curve
5. **Performance**: No runtime overhead, incremental generation only

---

## Dependencies

### External Dependencies
- Roslyn (C# compiler APIs)
- .NET 8+ with nullable reference types

### Internal Dependencies
- ComponentProperty attribute (existing)
- Deferred property system (existing)
- ComponentFactory pattern (existing)
- Partial method pattern (C# language feature)

### Blocked By
- None (ready to proceed with technical planning)

---

## Open Questions Resolved

1. ✅ **OnLoad Hook Pattern**: Optional `partial void OnLoad(DerivedTemplate template)` 
2. ✅ **Property Assignment**: Direct field assignment, no Set method calls
3. ✅ **ComponentType Default**: Get-only virtual property override
4. ✅ **Template Hierarchy**: Flat structure, all inherit from base Template

---

## Acceptance Criteria

### Feature Complete When:
- [ ] TemplateGenerator source generator implemented and tested
- [ ] All ComponentProperty fields generate template properties
- [ ] OnLoad method generated with correct field assignments
- [ ] ComponentType property functions as designed
- [ ] Compiler validation analyzer working
- [ ] All existing components migrated
- [ ] Zero breaking changes to public API (templates are new, not replacing)
- [ ] TestApp works with new templates
- [ ] Documentation updated

---

## Next Steps

1. **Technical Planning** (`/speckit.plan`)
   - Architecture for TemplateGenerator
   - Implementation phases
   - Technology decisions

2. **Task Breakdown** (`/speckit.tasks`)
   - Create base Template record
   - Implement TemplateGenerator
   - Implement validation analyzer
   - Create unit tests (TDD)
   - Migrate components

3. **Implementation** (`/speckit.implement`)
   - Execute tasks following TDD
   - Build-test-refactor cycle
   - Integration testing

---

**Specification Status**: ✅ COMPLETE
**Ready for**: Technical Planning Phase
**Date**: 2025-11-01
