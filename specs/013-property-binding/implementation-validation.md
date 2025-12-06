# Property Binding Implementation Validation

## Summary of Your Plan

### 1. Template Configuration
```csharp
new HealthBarTemplate()
{
    Bindings = 
    {
        CurrentHealth = PropertyBinding.From("PlayerCharacter", nameof(CurrentHealth)),
        MaxHealth = PropertyBinding.From("PlayerCharacter", nameof(MaxHealth))
    }
}
```

### 2. Source Generator Infrastructure

**PropertyBindingsGenerator** (new):
- For each `{ComponentType}Template`, generate `{ComponentType}PropertyBindings` class
- Class has one property per `[ComponentProperty]` in the component
- Implements `IEnumerable<(string propertyName, PropertyBinding binding)>`

**TemplateGenerator** (modified):
- Add `{ComponentType}PropertyBindings Bindings { get; init; } = new();` to each template

### 3. Component Lifecycle

**Load** (populate bindings):
```csharp
protected override void OnLoad(Template? template)
{
    base.OnLoad(template);
    _bindings = template.Bindings.ToList();
}
```

**Activate** (activate bindings):
```csharp
protected override void OnActivate()
{
    base.OnActivate();
    foreach (var binding in _bindings)
        binding.Activate();
}
```

### 4. Event System

**ComponentPropertyGenerator** (modified):
- Emit `{PropertyName}Changed` event for each `[ComponentProperty]`
- Fire event from `On{PropertyName}Changed` partial method

### 5. Immediate Updates

**ComponentPropertyUpdater** (modified):
- Add method to bypass deferral and set backing field directly
- Bindings use this to avoid double-deferral

## ✅ Validation

Your plan is **solid**! Here are some considerations and refinements:

### Critical Missing Pieces

#### 1. **Deactivate/Cleanup** ⚠️
You mentioned Activate but not Deactivate. Bindings must unsubscribe:

```csharp
protected override void OnDeactivate()
{
    foreach (var binding in _bindings)
        binding.Deactivate();
    base.OnDeactivate();
}
```

#### 2. **Component Lookup Timing** ⚠️
When does `FromNamedObject("PlayerCharacter")` resolve?

**Options**:
- **Option A**: During `PropertyBinding.Activate()` (lazy)
  - ✅ Allows source component to be created after target
  - ❌ Binding fails silently if source doesn't exist
  
- **Option B**: During `Component.Load()` (eager)
  - ✅ Fast fail if source doesn't exist
  - ❌ Requires source to be loaded before target

**Recommendation**: **Option A (lazy)** - More flexible, matches composition-first. Add optional validation:
```csharp
public override void Validate()
{
    base.Validate();
    
    // Validate that all binding sources can be resolved
    foreach (var (propertyName, binding) in _bindings)
    {
        if (!binding.CanResolveSource())
        {
            AddValidationError($"Cannot resolve source for binding on {propertyName}");
        }
    }
}
```

#### 3. **PropertyBinding Lifecycle** 🔑
`PropertyBinding` needs to be stateful:

```csharp
public class PropertyBinding
{
    // Configuration (from template)
    public required string SourceObjectName { get; init; }
    public required string SourcePropertyName { get; init; }
    public IValueConverter? Converter { get; init; }
    
    // Runtime state
    private IComponent? _sourceComponent;
    private Delegate? _eventHandler;
    private Action<object?>? _targetSetter;
    
    public void Activate(IComponent targetComponent, string targetPropertyName)
    {
        // 1. Find source component
        _sourceComponent = targetComponent.FindByName(SourceObjectName);
        if (_sourceComponent == null) return;
        
        // 2. Get target setter method
        _targetSetter = CreateSetter(targetComponent, targetPropertyName);
        
        // 3. Subscribe to source property change event
        var eventInfo = _sourceComponent.GetType().GetEvent($"{SourcePropertyName}Changed");
        _eventHandler = CreateEventHandler();
        eventInfo.AddEventHandler(_sourceComponent, _eventHandler);
        
        // 4. Initial sync
        SyncValue();
    }
    
    public void Deactivate()
    {
        if (_sourceComponent != null && _eventHandler != null)
        {
            var eventInfo = _sourceComponent.GetType().GetEvent($"{SourcePropertyName}Changed");
            eventInfo.RemoveEventHandler(_sourceComponent, _eventHandler);
            _eventHandler = null;
            _sourceComponent = null;
            _targetSetter = null;
        }
    }
}
```

#### 4. **IEnumerable Implementation** 📋
You said `PropertyBindings` should implement `IEnumerable`. Here's the pattern:

```csharp
// Generated for HealthBarPropertyBindings
public class HealthBarPropertyBindings : IEnumerable<(string propertyName, PropertyBinding binding)>
{
    public PropertyBinding? CurrentHealth { get; set; }
    public PropertyBinding? MaxHealth { get; set; }
    
    public IEnumerator<(string propertyName, PropertyBinding binding)> GetEnumerator()
    {
        if (CurrentHealth != null)
            yield return (nameof(CurrentHealth), CurrentHealth);
        if (MaxHealth != null)
            yield return (nameof(MaxHealth), MaxHealth);
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

#### 5. **Immediate Update Mechanism** 🎯
You correctly identified that bound properties shouldn't be deferred. Here's the API:

**ComponentPropertyUpdater addition**:
```csharp
public struct ComponentPropertyUpdater<T>
{
    // Existing methods...
    
    /// <summary>
    /// Sets the value immediately, bypassing interpolation.
    /// Use this for property bindings where the source value has already changed.
    /// </summary>
    public bool SetImmediate(ref T value, T target)
    {
        if (_comparer.Equals(value, target)) return false;
        
        value = target;
        _target = target;
        _interpolator = null;
        _hasUpdate = false;
        
        return true;
    }
}
```

**Generated setter method**:
```csharp
// NEW: Generated for binding support
public void SetCurrentHealthImmediate(float value)
{
    var oldValue = _currentHealth;
    if (_currentHealthState.SetImmediate(ref _currentHealth, value))
    {
        OnCurrentHealthChanged(oldValue);
    }
}
```

**Binding calls this**:
```csharp
private void OnSourceChanged(object? sender, PropertyChangedEventArgs<float> e)
{
    // Use immediate setter to avoid double-deferral
    _targetComponent.SetCurrentHealthImmediate(e.NewValue);
}
```

### Refinements

#### 1. **Fluent Builder Extensions** 🔧
You mentioned needing methods for mutation, conversion, etc. Here's the pattern:

```csharp
public class PropertyBinding
{
    // Core configuration
    public string? SourceObjectName { get; private set; }
    public string? SourcePropertyName { get; private set; }
    
    // Lookup strategies
    public static PropertyBinding FromNamedObject(string name) 
        => new() { SourceObjectName = name };
    
    public static PropertyBinding FromParent<T>() where T : IComponent
        => new() { LookupStrategy = new ParentLookup<T>() };
    
    public static PropertyBinding FromSibling<T>() where T : IComponent
        => new() { LookupStrategy = new SiblingLookup<T>() };
    
    public static PropertyBinding FromChild<T>() where T : IComponent
        => new() { LookupStrategy = new ChildLookup<T>() };
    
    // Property selection
    public PropertyBinding GetPropertyValue(string propertyName)
    {
        SourcePropertyName = propertyName;
        return this;
    }
    
    // Type conversion
    public PropertyBinding WithConverter(IValueConverter converter)
    {
        Converter = converter;
        return this;
    }
    
    // String formatting
    public PropertyBinding AsFormattedString(string format)
    {
        Converter = new StringFormatConverter(format);
        return this;
    }
    
    // Boolean logic
    public PropertyBinding WhenTrue(object trueValue, object falseValue)
    {
        Converter = new BooleanSwitchConverter(trueValue, falseValue);
        return this;
    }
    
    // Arithmetic mutation
    public PropertyBinding Multiply(float factor)
    {
        Converter = new MultiplyConverter(factor);
        return this;
    }
    
    public PropertyBinding Add(float offset)
    {
        Converter = new AddConverter(offset);
        return this;
    }
}
```

#### 2. **Lookup Strategies** 🔍
Abstract the "find source component" logic:

```csharp
public interface ILookupStrategy
{
    IComponent? ResolveSource(IComponent target);
}

public class NamedObjectLookup : ILookupStrategy
{
    public required string Name { get; init; }
    
    public IComponent? ResolveSource(IComponent target)
        => target.ContentManager?.FindComponentByName(Name);
}

public class ParentLookup<T> : ILookupStrategy where T : IComponent
{
    public IComponent? ResolveSource(IComponent target)
        => target.FindParent<T>();
}

public class SiblingLookup<T> : ILookupStrategy where T : IComponent
{
    public IComponent? ResolveSource(IComponent target)
        => target.GetSiblings<T>().FirstOrDefault();
}
```

#### 3. **Error Handling** 🚨
Bindings can fail for many reasons:

```csharp
public enum BindingErrorMode
{
    Silent,          // Ignore errors
    LogWarning,      // Log but continue
    ThrowException,  // Fail fast
    FallbackValue    // Use fallback
}

public class PropertyBinding
{
    public BindingErrorMode ErrorMode { get; set; } = BindingErrorMode.LogWarning;
    public object? FallbackValue { get; set; }
    
    private void HandleError(string message)
    {
        switch (ErrorMode)
        {
            case BindingErrorMode.Silent:
                break;
            case BindingErrorMode.LogWarning:
                Log.Warning($"Binding error: {message}");
                break;
            case BindingErrorMode.ThrowException:
                throw new InvalidOperationException(message);
            case BindingErrorMode.FallbackValue:
                if (FallbackValue != null)
                    _targetSetter?.Invoke(FallbackValue);
                break;
        }
    }
}
```

#### 4. **Two-Way Bindings** 🔄
You didn't mention this, but it's common. Should we support it?

```csharp
public enum BindingMode
{
    OneWay,      // Source → Target
    TwoWay,      // Source ↔ Target (with cycle prevention)
    OneWayToSource  // Source ← Target
}

public class PropertyBinding
{
    public BindingMode Mode { get; set; } = BindingMode.OneWay;
    
    private bool _isUpdating; // Prevent cycles
    
    private void OnSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isUpdating) return;
        
        try
        {
            _isUpdating = true;
            // Update target from source
        }
        finally
        {
            _isUpdating = false;
        }
    }
    
    private void OnTargetChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isUpdating || Mode != BindingMode.TwoWay) return;
        
        try
        {
            _isUpdating = true;
            // Update source from target
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
```

#### 5. **Performance Considerations** ⚡
Bindings can be expensive if misused:

```csharp
public class PropertyBinding
{
    // Throttling
    public TimeSpan? UpdateThrottle { get; set; }
    private DateTime _lastUpdate;
    
    // Batching
    public bool BatchUpdates { get; set; } = false;
    
    // Conditional updates
    public Func<object?, object?, bool>? UpdateCondition { get; set; }
    
    private void OnSourceChanged(object? sender, PropertyChangedEventArgs e)
    {
        var newValue = GetSourceValue();
        var oldValue = GetTargetValue();
        
        // Check throttle
        if (UpdateThrottle.HasValue && 
            DateTime.Now - _lastUpdate < UpdateThrottle.Value)
            return;
        
        // Check condition
        if (UpdateCondition != null && 
            !UpdateCondition(oldValue, newValue))
            return;
        
        // Apply update
        ApplyUpdate(newValue);
        _lastUpdate = DateTime.Now;
    }
}
```

### Component Lifecycle Integration

Here's the complete lifecycle with bindings:

```csharp
public partial class Component
{
    private List<(string propertyName, PropertyBinding binding)> _bindings = [];
    
    protected override void OnLoad(Template? template)
    {
        base.OnLoad(template);
        
        // Store bindings (don't activate yet)
        if (template?.Bindings != null)
        {
            _bindings = template.Bindings.ToList();
        }
    }
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Activate all bindings
        foreach (var (propertyName, binding) in _bindings)
        {
            binding.Activate(this, propertyName);
        }
    }
    
    protected override void OnDeactivate()
    {
        // Deactivate bindings BEFORE calling base
        foreach (var (_, binding) in _bindings)
        {
            binding.Deactivate();
        }
        
        base.OnDeactivate();
    }
    
    public override void Validate()
    {
        base.Validate();
        
        // Validate bindings can resolve
        foreach (var (propertyName, binding) in _bindings)
        {
            if (!binding.CanResolveSource(this))
            {
                AddValidationError($"Cannot resolve source for property binding on {propertyName}");
            }
        }
    }
}
```

### Generated Code Example

**HealthBar.g.cs** (ComponentPropertyGenerator):
```csharp
partial class HealthBar
{
    // Existing property generation
    private ComponentPropertyUpdater<float> _currentHealthState;
    
    public float CurrentHealth => _currentHealth;
    
    public void SetCurrentHealth(float value, InterpolationFunction<float>? interpolator = null)
    {
        var oldValue = _currentHealth;
        if (_currentHealthState.Set(ref _currentHealth, value, interpolator))
        {
            if (interpolator == null)
                OnCurrentHealthChanged(oldValue);
            else
                _isDirty = true;
        }
    }
    
    // NEW: Immediate setter for bindings
    public void SetCurrentHealthImmediate(float value)
    {
        var oldValue = _currentHealth;
        if (_currentHealthState.SetImmediate(ref _currentHealth, value))
        {
            OnCurrentHealthChanged(oldValue);
        }
    }
    
    // NEW: Change event
    public event EventHandler<PropertyChangedEventArgs<float>>? CurrentHealthChanged;
    
    partial void OnCurrentHealthChanged(float oldValue)
    {
        CurrentHealthChanged?.Invoke(this, new(oldValue, CurrentHealth));
    }
}
```

**HealthBarTemplate.g.cs** (TemplateGenerator):
```csharp
partial record HealthBarTemplate
{
    public HealthBarPropertyBindings Bindings { get; init; } = new();
}
```

**HealthBarPropertyBindings.g.cs** (PropertyBindingsGenerator - new):
```csharp
public class HealthBarPropertyBindings : IEnumerable<(string propertyName, PropertyBinding binding)>
{
    public PropertyBinding? CurrentHealth { get; set; }
    public PropertyBinding? MaxHealth { get; set; }
    
    public IEnumerator<(string propertyName, PropertyBinding binding)> GetEnumerator()
    {
        if (CurrentHealth != null)
            yield return (nameof(CurrentHealth), CurrentHealth);
        if (MaxHealth != null)
            yield return (nameof(MaxHealth), MaxHealth);
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

## Missing Considerations?

### Things to Decide Before Spec:

1. **Should we support TwoWay bindings?** (probably yes, common use case)

2. **Should PropertyBinding be a class or record?** 
   - Class: Mutable, stateful, has Activate/Deactivate methods
   - Record: Immutable config, separate `BindingInstance` class for runtime

3. **How do we handle type mismatches?**
   ```csharp
   // PlayerCharacter.Health is float
   // HealthBar.Text is string
   Bindings.Text = PropertyBinding
       .FromParent<PlayerCharacter>()
       .GetPropertyValue(nameof(PlayerCharacter.Health))
       .AsFormattedString("{0:F1}");  // Explicit conversion required
   ```

4. **Should we generate SetImmediate or reuse SetCurrent?**
   - `SetCurrentHealth()` already exists and bypasses interpolation
   - Could add a parameter: `SetCurrentHealth(value, immediate: true)`
   - Or use `SetCurrentHealth` as-is for bindings

5. **Validation vs Runtime errors?**
   - Validate during `Component.Validate()` (can't start app if bindings broken)
   - Or fail silently at runtime (log warning, use fallback)

6. **Should bindings track dependency changes?**
   - If source component is removed/replaced, should binding auto-rebind?
   - Or is Deactivate/Activate sufficient?

7. **Collection bindings?**
   ```csharp
   // How to bind to array elements?
   Bindings.Items = PropertyBinding
       .FromParent<Inventory>()
       .GetPropertyValue(nameof(Inventory.Items))  // Returns Item[]
       .MapToCollection(item => new ItemDisplayTemplate { ... });
   ```

Your plan is **excellent**! These are refinements and edge cases, not gaps. You've captured the essential architecture.

## Recommendation

Write the spec with:
1. ✅ Your proposed syntax (PropertyBindings.{PropertyName})
2. ✅ Three source generators (PropertyBindingsGenerator, TemplateGenerator update, ComponentPropertyGenerator update)
3. ✅ Component lifecycle integration (Load, Activate, Deactivate)
4. ✅ Immediate update mechanism (SetImmediate or SetCurrent)
5. ✅ Basic fluent API (From*, WithConverter, etc.)
6. ✅ IEnumerable implementation for PropertyBindings classes

**Defer to future iterations**:
- TwoWay bindings
- Throttling/batching
- Collection bindings
- Advanced error handling

Start simple, iterate!
