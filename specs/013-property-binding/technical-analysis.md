# Property Binding: Technical Analysis

## Problem Statement

You've correctly identified the two fundamental challenges:

### Challenge 1: Component Lookup
**How do we look up the component from which we obtain the value?**

Current navigation methods available in code:
- `FindParent<T>(filter)` - Traverse up tree
- `GetChildren<T>(filter, recursive)` - Traverse down tree
- `GetSiblings<T>(filter)` - Lateral traversal via parent

**Issue**: These are strongly-typed, code-based methods. How do we express these in a declarative Template?

### Challenge 2: Property Update Behavior
**How do we define the update behavior from a property of that component?**

Considerations:
- Type conversions (float → string, int → percentage)
- Format specifiers ("0.00", "F2", custom formats)
- Null value handling (default values, fallback values)
- Event/loop re-entry (A→B→A circular updates)
- Update timing (immediate vs deferred)
- Validation (range checks, constraints)

## Analysis of Challenge 1: Component Lookup

### Option A: Path-Based Strings (Proposed in spec-v2)

```csharp
new HealthBarTemplate()
{
    PropertyBindings = 
    [
        new("../Health", "Health")  // Parent's Health property
    ]
}
```

**Path Syntax Options**:
```
"../PropertyName"              // Parent
"../../PropertyName"           // Grandparent
"ChildName/PropertyName"       // Named child
"../SiblingName/PropertyName"  // Sibling
"$Context<ThemeContext>/Theme" // Context lookup
"/Root/Path/To/Component"      // Absolute path from root
```

**Pros**:
- ✅ Declarative and familiar (like file systems)
- ✅ No type information needed at template construction
- ✅ Can express complex traversals

**Cons**:
- ❌ String-based (no compile-time safety)
- ❌ Fragile (rename breaks bindings)
- ❌ Runtime resolution required
- ❌ How to handle name collisions?
- ❌ What if multiple children have same name?

### Option B: Lookup Functions in Template

```csharp
new HealthBarTemplate()
{
    PropertyBindings = 
    [
        new PropertyBindingTemplate()
        {
            SourceLookup = (self) => self.FindParent<HealthSystem>(),
            SourceProperty = "Health",
            TargetProperty = "Health"
        }
    ]
}
```

**Pros**:
- ✅ Type-safe component lookup
- ✅ Flexible (can use any traversal logic)
- ✅ IntelliSense support

**Cons**:
- ❌ Not serializable (can't save to JSON/XML)
- ❌ More complex template syntax
- ❌ Requires lambda/delegate knowledge

### Option C: Typed Reference (Unity-style)

This doesn't work well without a visual editor to set references.

```csharp
// Would need editor support
new HealthBarTemplate()
{
    SourceComponent = /* reference to instance? Can't do at template time */
}
```

**Cons**:
- ❌ References don't exist at template construction time
- ❌ Would need two-phase initialization
- ❌ Requires editor/inspector tooling

### Option D: ContentManager Lookup Service

Extend ContentManager with a declarative lookup API:

```csharp
public interface IContentManager
{
    // New lookup methods
    IComponent? Lookup(IComponent from, string path);
    IComponent? LookupByName(string name, IComponent? scope = null);
    IComponent? LookupById(ComponentId id);
    T? LookupContext<T>() where T : IComponent, IContextProvider;
}

// Usage in binding
new PropertyBindingTemplate()
{
    LookupPath = "../",  // Use ContentManager.Lookup(this, "../")
    SourceProperty = "Health",
    TargetProperty = "Health"
}
```

**Pros**:
- ✅ Centralized lookup logic
- ✅ Can implement caching/optimization
- ✅ Declarative paths still string-based
- ✅ Could support multiple lookup strategies

**Cons**:
- ❌ Still string-based for paths
- ❌ Adds responsibility to ContentManager

### Recommendation for Challenge 1

**Hybrid Approach**: Path-based with multiple lookup strategies

```csharp
public record PropertyBindingTemplate
{
    // Primary: Path-based lookup (covers 80% of cases)
    public string? LookupPath { get; init; }  // "../", "ChildName/", etc.
    
    // Alternative: Type-based parent lookup (compile-time safe)
    public Type? LookupParentType { get; init; }  // typeof(HealthSystem)
    
    // Alternative: Type-based context lookup
    public Type? LookupContextType { get; init; }  // typeof(ThemeContext)
    
    // Alternative: Direct name lookup (scoped to parent)
    public string? LookupName { get; init; }  // "PlayerHealth"
    
    // Property name on source component
    public string SourceProperty { get; init; }
    
    // Property name on this component
    public string TargetProperty { get; init; }
}

// Examples:
// Parent lookup by type
new PropertyBindingTemplate 
{ 
    LookupParentType = typeof(HealthSystem),
    SourceProperty = "Health",
    TargetProperty = "Health"
}

// Parent lookup by path (when type not known)
new PropertyBindingTemplate 
{ 
    LookupPath = "../",
    SourceProperty = "Health",
    TargetProperty = "Health"  
}

// Sibling by name
new PropertyBindingTemplate
{
    LookupName = "VolumeSlider",
    SourceProperty = "Value",
    TargetProperty = "Text"
}

// Context by type
new PropertyBindingTemplate
{
    LookupContextType = typeof(ThemeContext),
    SourceProperty = "PrimaryColor",
    TargetProperty = "BackgroundColor"
}
```

**Resolution Priority**:
1. If `LookupParentType` set → `FindParent<T>()`
2. Else if `LookupContextType` set → Search up for `IContextProvider` of type
3. Else if `LookupName` set → Find sibling by name
4. Else if `LookupPath` set → Parse path syntax
5. Else → Error (no lookup specified)

**Benefits**:
- Type-safe options when possible (`LookupParentType`, `LookupContextType`)
- Fallback to path-based for complex cases
- Clear and explicit in templates
- Multiple strategies for different use cases

## Analysis of Challenge 2: Property Update Behavior

### Type Conversion

**Need**: Convert between incompatible types (float → string, enum → int, etc.)

```csharp
public interface IValueConverter
{
    object? Convert(object? value, Type targetType);
    object? ConvertBack(object? value, Type sourceType);
}

// Example: Float to formatted string
public class FloatToStringConverter : IValueConverter
{
    public string? FormatString { get; set; } = "F2";
    
    public object? Convert(object? value, Type targetType)
    {
        if (value is float f)
            return f.ToString(FormatString);
        return value?.ToString();
    }
    
    public object? ConvertBack(object? value, Type sourceType)
    {
        if (value is string s && float.TryParse(s, out var f))
            return f;
        return null;
    }
}

// Usage
new PropertyBindingTemplate()
{
    LookupPath = "../Volume",
    SourceProperty = "Value",
    TargetProperty = "Text",
    Converter = new FloatToStringConverter { FormatString = "0.0%" }
}
```

### Null Handling

**Options**:
1. **Propagate null** - Pass null through to target
2. **Use fallback** - Replace null with default value
3. **Skip update** - Don't update target if source is null

```csharp
public record PropertyBindingTemplate
{
    // ...
    public NullBehavior NullHandling { get; init; } = NullBehavior.Propagate;
    public object? FallbackValue { get; init; }
}

public enum NullBehavior
{
    Propagate,     // Pass null to target
    UseFallback,   // Use FallbackValue instead
    SkipUpdate     // Don't update target property
}
```

### Re-entry Prevention

**Problem**: A→B binding triggers B→A binding (in TwoWay mode), creating infinite loop

**Solution**: Track update in-progress flag

```csharp
internal class ActivePropertyBinding
{
    private bool _isUpdating = false;
    
    private void SyncSourceToTarget()
    {
        if (_isUpdating) return;  // ❌ Re-entry detected, abort
        
        try
        {
            _isUpdating = true;
            
            var value = _sourceProperty.GetValue(_sourceComponent);
            // ... conversion, validation
            _targetProperty.SetValue(_targetComponent, value);
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
```

**Alternative**: Deferred updates (batch all binding updates together)

```csharp
public class BindingUpdateCoordinator
{
    private readonly Queue<Action> _pendingUpdates = new();
    private bool _isProcessing = false;
    
    public void QueueUpdate(Action update)
    {
        _pendingUpdates.Enqueue(update);
        
        if (!_isProcessing)
            ProcessQueue();
    }
    
    private void ProcessQueue()
    {
        _isProcessing = true;
        try
        {
            while (_pendingUpdates.Count > 0)
            {
                var update = _pendingUpdates.Dequeue();
                update();
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }
}
```

### Validation

**Need**: Validate values before applying (range checks, business rules)

```csharp
public interface IValueValidator
{
    ValidationResult Validate(object? value, Type targetType);
}

public record ValidationResult(bool IsValid, string? ErrorMessage = null);

public class RangeValidator : IValueValidator
{
    public double Min { get; set; }
    public double Max { get; set; }
    
    public ValidationResult Validate(object? value, Type targetType)
    {
        if (value is IComparable comparable)
        {
            var val = Convert.ToDouble(value);
            if (val < Min || val > Max)
                return new ValidationResult(false, $"Value must be between {Min} and {Max}");
        }
        return new ValidationResult(true);
    }
}

// Usage
new PropertyBindingTemplate()
{
    LookupParentType = typeof(InputField),
    SourceProperty = "Text",
    TargetProperty = "Volume",
    Converter = new StringToFloatConverter(),
    Validator = new RangeValidator { Min = 0, Max = 100 }
}
```

### Update Timing

**Options**:
1. **Immediate** - Update target as soon as source changes
2. **Deferred** - Update target during next frame (matches ComponentProperty behavior)
3. **Throttled** - Limit update frequency (useful for expensive operations)

```csharp
public record PropertyBindingTemplate
{
    // ...
    public UpdateTiming Timing { get; init; } = UpdateTiming.Immediate;
    public double ThrottleMs { get; init; } = 0;  // Only used if Timing = Throttled
}

public enum UpdateTiming
{
    Immediate,   // Update immediately when PropertyChanged fires
    Deferred,    // Queue update, apply during next frame
    Throttled    // Limit to once per ThrottleMs milliseconds
}
```

## Complete PropertyBindingTemplate Design

Combining both challenges:

```csharp
public record PropertyBindingTemplate
{
    // === COMPONENT LOOKUP (Challenge 1) ===
    
    /// <summary>Type-based parent lookup (preferred when type is known)</summary>
    public Type? LookupParentType { get; init; }
    
    /// <summary>Type-based context lookup</summary>
    public Type? LookupContextType { get; init; }
    
    /// <summary>Name-based sibling lookup</summary>
    public string? LookupName { get; init; }
    
    /// <summary>Path-based lookup (fallback for complex cases)</summary>
    public string? LookupPath { get; init; }
    
    // === PROPERTY NAMES ===
    
    /// <summary>Property name on source component</summary>
    public string SourceProperty { get; init; } = string.Empty;
    
    /// <summary>Property name on target component (this)</summary>
    public string TargetProperty { get; init; } = string.Empty;
    
    // === UPDATE BEHAVIOR (Challenge 2) ===
    
    /// <summary>Binding mode</summary>
    public BindingMode Mode { get; init; } = BindingMode.OneWay;
    
    /// <summary>Optional value converter</summary>
    public IValueConverter? Converter { get; init; }
    
    /// <summary>Optional value validator</summary>
    public IValueValidator? Validator { get; init; }
    
    /// <summary>How to handle null source values</summary>
    public NullBehavior NullHandling { get; init; } = NullBehavior.Propagate;
    
    /// <summary>Fallback value when source is null (if NullHandling = UseFallback)</summary>
    public object? FallbackValue { get; init; }
    
    /// <summary>Update timing strategy</summary>
    public UpdateTiming Timing { get; init; } = UpdateTiming.Immediate;
    
    /// <summary>Throttle interval in milliseconds (if Timing = Throttled)</summary>
    public double ThrottleMs { get; init; } = 0;
    
    /// <summary>Priority for binding updates (lower = earlier)</summary>
    public int Priority { get; init; } = 0;
}
```

## Usage Examples

### Example 1: Simple Parent Binding
```csharp
new HealthBarTemplate()
{
    PropertyBindings = 
    [
        new PropertyBindingTemplate
        {
            LookupParentType = typeof(HealthSystem),  // Type-safe
            SourceProperty = "Health",
            TargetProperty = "Health"
        }
    ]
}
```

### Example 2: Formatted Display with Conversion
```csharp
new TextElementTemplate()
{
    PropertyBindings = 
    [
        new PropertyBindingTemplate
        {
            LookupParentType = typeof(HealthSystem),
            SourceProperty = "Health",
            TargetProperty = "Text",
            Converter = new FloatToStringConverter { FormatString = "0.0" },
            NullHandling = NullBehavior.UseFallback,
            FallbackValue = "N/A"
        }
    ]
}
```

### Example 3: Validated Two-Way Binding
```csharp
new InputFieldTemplate()
{
    PropertyBindings = 
    [
        new PropertyBindingTemplate
        {
            LookupName = "VolumeSlider",  // Sibling lookup
            SourceProperty = "Value",
            TargetProperty = "Text",
            Mode = BindingMode.TwoWay,
            Converter = new FloatToStringConverter(),
            Validator = new RangeValidator { Min = 0, Max = 100 },
            Timing = UpdateTiming.Throttled,
            ThrottleMs = 100  // Max 10 updates/sec
        }
    ]
}
```

### Example 4: Context Binding
```csharp
new ButtonTemplate()
{
    PropertyBindings = 
    [
        new PropertyBindingTemplate
        {
            LookupContextType = typeof(ThemeContext),  // Search up tree
            SourceProperty = "PrimaryColor",
            TargetProperty = "BackgroundColor"
        }
    ]
}
```

## Implementation Considerations

### ContentManager Responsibilities

Should ContentManager handle lookup resolution?

**Proposal**: Add lookup helpers to ContentManager:

```csharp
public interface IContentManager
{
    // Existing methods...
    
    // New lookup methods for binding resolution
    IComponent? ResolveBinding(IComponent from, PropertyBindingTemplate binding);
}

// Implementation
public IComponent? ResolveBinding(IComponent from, PropertyBindingTemplate binding)
{
    // Priority: Type > Name > Path
    if (binding.LookupParentType != null)
        return from.FindParent(c => binding.LookupParentType.IsAssignableFrom(c.GetType()));
    
    if (binding.LookupContextType != null)
        return FindContext(from, binding.LookupContextType);
    
    if (binding.LookupName != null)
        return from.GetSiblings<IComponent>().FirstOrDefault(s => s.Name == binding.LookupName);
    
    if (binding.LookupPath != null)
        return ResolvePath(from, binding.LookupPath);
    
    return null;
}
```

### Performance Optimization

**Lazy Resolution**: Don't resolve bindings until OnActivate()
**Caching**: Cache resolved component references (invalidate on tree changes)
**Batching**: Process all binding updates together to avoid cascading updates

## Open Questions

1. **Should binding resolution be cached?**
   - Pro: Faster repeated lookups
   - Con: Must invalidate when tree structure changes
   - **Recommendation**: Cache during stable tree, re-resolve on ChildCollectionChanged

2. **What happens if lookup fails?**
   - Log warning and disable binding?
   - Retry on next Activate()?
   - **Recommendation**: Log warning once, mark binding as failed, don't retry

3. **Should we support computed bindings (multi-source)?**
   ```csharp
   // Bind to multiple sources with custom logic
   new PropertyBindingTemplate
   {
       Sources = ["../Health", "../MaxHealth"],
       Computer = (health, maxHealth) => health / maxHealth,
       TargetProperty = "HealthPercentage"
   }
   ```
   - **Recommendation**: Phase 2 feature

4. **Binding priority/ordering for complex updates?**
   - Some bindings may need to fire before others
   - **Recommendation**: Add Priority field, process in order

## Recommendation Summary

### Challenge 1: Component Lookup
Use **hybrid lookup strategy** with priority:
1. `LookupParentType` (type-safe, preferred)
2. `LookupContextType` (type-safe for contexts)
3. `LookupName` (string-based sibling lookup)
4. `LookupPath` (string-based fallback for complex cases)

### Challenge 2: Update Behavior
Support full feature set:
- **Type conversion** via `IValueConverter`
- **Validation** via `IValueValidator`
- **Null handling** via `NullBehavior` enum + `FallbackValue`
- **Re-entry prevention** via `_isUpdating` flag
- **Update timing** via `UpdateTiming` enum + optional throttling

This provides flexibility for simple cases while supporting complex scenarios when needed.
